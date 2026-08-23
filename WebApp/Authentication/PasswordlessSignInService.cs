using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine;
using SiteEngine.Data;
using SiteEngine.Enums;
using SiteEngine.Identity;
using WebApp.Services;

namespace WebApp.Authentication;

public class PasswordlessSignInService(
	UserManager<ApplicationUser> userManager,
	SignInManager<ApplicationUser> signInManager,
	IEmailLoginSender emailLoginSender,
	SiteContext siteContext,
	PasswordlessCodeStore codeStore,
	IDbContextFactory<AppDbContext> dbFactory,
	ProfileService profileService,
	MembershipLookupService membershipLookup,
	LoginTrackingService loginTracking,
	UserModerationService userModeration
)
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
	private readonly IEmailLoginSender _emailLoginSender = emailLoginSender;
	private readonly SiteContext _siteContext = siteContext;
	private readonly PasswordlessCodeStore _codeStore = codeStore;
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
	private readonly ProfileService _profileService = profileService;
	private readonly MembershipLookupService _membershipLookup = membershipLookup;
	private readonly LoginTrackingService _loginTracking = loginTracking;
	private readonly UserModerationService _userModeration = userModeration;

	// -----------------------------
	// SEND CODE
	// -----------------------------
	// Any email may request a code — existence is decided here, not at
	// verification. An unknown email gets an account right now (no elevated
	// permissions, EmailConfirmed = false) so "does this user exist" always
	// has a single, consistent answer from this point forward. Either way a
	// code is generated and sent, and the caller-facing behavior is identical
	// for new vs. existing emails, so the response never reveals which one it was.
	public async Task RequestCodeAsync(string email)
	{
		var normalizedEmail = NormalizeEmail(email);

		if (await _userModeration.IsBannedAsync(normalizedEmail))
			throw new UserBannedException();

		var user = await _userManager.FindByEmailAsync(normalizedEmail);
		var isFirstLogin = user is null || user.IsFirstLogin;

		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = normalizedEmail,
				Email = normalizedEmail,
				EmailConfirmed = false,
				IsFirstLogin = true
			};

			var createResult = await _userManager.CreateAsync(user);
			if (!createResult.Succeeded)
				return;

			// Separate DbContext instance from the one behind UserManager's
			// store (see SetupService for the same pattern) — set only the FK
			// scalar, never the IdentityUser navigation, or the change tracker
			// will try to re-insert the ApplicationUser row that CreateAsync
			// just committed.
			await using var db = await _dbFactory.CreateDbContextAsync();
			db.SiteUsers.Add(new SiteUser
			{
				IdentityUserId = user.Id,
				PreferredEmail = normalizedEmail
			});
			await db.SaveChangesAsync();
		}

		var code = _codeStore.GenerateCode(normalizedEmail);
		var template = isFirstLogin ? LoginEmailTemplate.Welcome : LoginEmailTemplate.WelcomeBack;
		await _emailLoginSender.SendLoginCodeAsync(normalizedEmail, code, template);
	}

	// -----------------------------
	// VERIFY CODE
	// -----------------------------
	public async Task<PasswordlessSignInResult> SignInWithCodeAsync(string email, string code, string host, string? ipAddress = null)
	{
		var normalizedEmail = NormalizeEmail(email);
		var normalizedCode = code?.Trim() ?? string.Empty;

		// Backstop for a code that was issued before the ban landed — a
		// still-valid code shouldn't let a since-banned email complete sign-in.
		if (await _userModeration.IsBannedAsync(normalizedEmail))
			return PasswordlessSignInResult.BannedResult;

		if (string.IsNullOrWhiteSpace(normalizedCode))
			return PasswordlessSignInResult.Failed;

		if (!_codeStore.Validate(normalizedEmail, normalizedCode))
			return PasswordlessSignInResult.Failed;

		// Accounts are created in RequestCodeAsync now, so a validated code
		// with no matching user shouldn't happen — treat it as failure rather
		// than creating an account here too.
		var user = await _userManager.FindByEmailAsync(normalizedEmail);
		if (user is null)
			return PasswordlessSignInResult.Failed;

		// Single-use: burn the code only once sign-in is actually going to happen,
		// so a transient failure above still leaves it retryable.
		_codeStore.Consume(normalizedEmail);

		user.EmailConfirmed = true;
		user.IsFirstLogin = false;
		await _userManager.UpdateAsync(user);

		await _signInManager.SignInAsync(user, isPersistent: false);

		// This runs from the /auth/passwordless/complete minimal API endpoint,
		// which gets its own DI scope per request — SiteContext here has never
		// had InitializeAsync(host) called against it the way a Blazor circuit
		// does in SiteLayoutSelector/Home.razor, so resolve it now.
		if (_siteContext.CurrentSite is null)
			await _siteContext.InitializeAsync(host);

		await _loginTracking.RecordLoginAsync(user.Id, _siteContext.CurrentSite?.Id, ipAddress);

		var siteUser = await _profileService.GetByIdentityUserIdAsync(user.Id);
		var requiresProfileCompletion = !ProfileService.IsComplete(siteUser);

		SiteRole? membershipRole = null;
		if (siteUser is not null && _siteContext.CurrentSite is not null &&
			(_siteContext.IsDivisionContext || _siteContext.IsUnitContext))
		{
			var schoolYear = SchoolYear.Current();
			var membership = await _membershipLookup.FindCurrentMembershipAsync(
				_siteContext.CurrentSite.Id, siteUser.Id, schoolYear);

			if (membership is not null)
				membershipRole = membership.Role ?? SiteRole.Member;
		}

		return new PasswordlessSignInResult(
			Succeeded: true,
			RequiresProfileCompletion: requiresProfileCompletion,
			MembershipRole: membershipRole);
	}

	public async Task SignOutAsync()
	{
		await _signInManager.SignOutAsync();
	}

	private static string NormalizeEmail(string email)
	{
		var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
			throw new InvalidOperationException("Email is required.");

		return normalized;
	}
}
