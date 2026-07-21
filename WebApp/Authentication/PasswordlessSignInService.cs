using Microsoft.AspNetCore.Identity;
using SiteEngine.Entities;
using SiteEngine.Identity;
using WebApp.Services;

namespace WebApp.Authentication;

public class PasswordlessSignInService(
	UserManager<ApplicationUser> userManager,
	SignInManager<ApplicationUser> signInManager,
	IEmailLoginSender emailLoginSender,
	SiteContext siteContext,
	PasswordlessCodeStore codeStore
)
{
	private readonly UserManager<ApplicationUser> _userManager = userManager;
	private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
	private readonly IEmailLoginSender _emailLoginSender = emailLoginSender;
	private readonly SiteContext _siteContext = siteContext;
	private readonly PasswordlessCodeStore _codeStore = codeStore;

	// -----------------------------
	// SEND CODE
	// -----------------------------
	public async Task RequestCodeAsync(string email)
	{
		var normalizedEmail = NormalizeEmail(email);

		// PORTAL: only existing users can log in. Stay silent for unknown
		// addresses (no code generated, no email sent) so the response can't
		// be used to enumerate accounts.
		if (_siteContext.IsAdminContext)
		{
			var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
			if (existingUser is not null)
			{
				var code = _codeStore.GenerateCode(normalizedEmail);
				await _emailLoginSender.SendLoginCodeAsync(normalizedEmail, code);
			}

			return;
		}

		// DIVISION / UNIT: send a code regardless of whether the account exists
		// yet — it's created at verification time, once the code is proven valid.
		var freshCode = _codeStore.GenerateCode(normalizedEmail);
		await _emailLoginSender.SendLoginCodeAsync(normalizedEmail, freshCode);
	}

	// -----------------------------
	// VERIFY CODE
	// -----------------------------
	public async Task<bool> SignInWithCodeAsync(string email, string code, string host)
	{
		var normalizedEmail = NormalizeEmail(email);
		var normalizedCode = code?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(normalizedCode))
			return false;

		if (!_codeStore.Validate(normalizedEmail, normalizedCode))
			return false;

		var user = await _userManager.FindByEmailAsync(normalizedEmail);

		// PORTAL: must already exist
		if (_siteContext.IsAdminContext && user is null)
			return false;

		if (user is null)
		{
			// DIVISION / UNIT: create the account now that the code is verified
			user = new ApplicationUser
			{
				UserName = normalizedEmail,
				Email = normalizedEmail,
				EmailConfirmed = true
			};

			var result = await _userManager.CreateAsync(user);
			if (!result.Succeeded)
				return false;
		}

		// Single-use: burn the code only once sign-in is actually going to happen,
		// so a transient failure above still leaves it retryable.
		_codeStore.Consume(normalizedEmail);

		await _signInManager.SignInAsync(user, isPersistent: false);
		return true;
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
