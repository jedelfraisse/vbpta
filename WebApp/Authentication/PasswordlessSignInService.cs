using Microsoft.AspNetCore.Identity;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Identity;
using SiteEngine.Services;
using SiteEngine.Sites;

namespace WebApp.Authentication;

public class PasswordlessSignInService(
	UserManager<SiteUser> userManager,
	SignInManager<SiteUser> signInManager,
	IEmailLoginSender emailLoginSender,
	ISiteResolver siteResolver,
	ISiteUserService siteUserService)
{
	public const string BootstrapAdminEmail = "admin@admin.com";
	public const string BootstrapAdminCode = "123456";

	private readonly UserManager<SiteUser> _userManager = userManager;
	private readonly SignInManager<SiteUser> _signInManager = signInManager;
	private readonly IEmailLoginSender _emailLoginSender = emailLoginSender;
	private readonly ISiteResolver _siteResolver = siteResolver;
	private readonly ISiteUserService _siteUserService = siteUserService;

	public async Task RequestCodeAsync(string email, CancellationToken cancellationToken = default)
	{
		var normalizedEmail = NormalizeEmail(email);
		if (!IsBootstrapEmail(normalizedEmail) && await IsBootstrapLockActiveAsync())
		{
			throw new InvalidOperationException("Bootstrap admin email must be changed before other logins are allowed.");
		}

		if (IsBootstrapEmail(normalizedEmail))
		{
			var bootstrapUser = await EnsureUserAsync(normalizedEmail);
			await EnsureBootstrapAdminRoleAsync(bootstrapUser);
			return;
		}

		var user = await EnsureUserAsync(normalizedEmail);
		var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
		await _emailLoginSender.SendCodeAsync(normalizedEmail, code, cancellationToken);
	}

	public async Task<bool> SignInWithCodeAsync(string email, string code, string host)
	{
		var normalizedEmail = NormalizeEmail(email);
		var normalizedCode = code?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalizedCode))
		{
			return false;
		}

		if (!IsBootstrapEmail(normalizedEmail) && await IsBootstrapLockActiveAsync())
		{
			throw new InvalidOperationException("Bootstrap admin email must be changed before other logins are allowed.");
		}

		if (IsBootstrapEmail(normalizedEmail))
		{
			var resolvedSite = await _siteResolver.ResolveAsync(host);
			if (resolvedSite?.IsAdminContext != true)
			{
				throw new InvalidOperationException("Bootstrap login is only available on the admin site.");
			}

			if (!string.Equals(normalizedCode, BootstrapAdminCode, StringComparison.Ordinal))
			{
				return false;
			}

			var bootstrapUser = await EnsureUserAsync(normalizedEmail);
			await EnsureBootstrapAdminRoleAsync(bootstrapUser);
			await _signInManager.SignInAsync(bootstrapUser, isPersistent: false);
			return true;
		}

		var user = await _userManager.FindByEmailAsync(normalizedEmail);
		if (user is null)
		{
			return false;
		}

		var valid = await _userManager.VerifyTwoFactorTokenAsync(
			user,
			TokenOptions.DefaultEmailProvider,
			normalizedCode);
		if (!valid)
		{
			return false;
		}

		await _signInManager.SignInAsync(user, isPersistent: false);
		return true;
	}

	public async Task ChangeBootstrapEmailAsync(string userId, string newEmail)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new InvalidOperationException("You must be signed in.");
		}

		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
		{
			throw new InvalidOperationException("Unable to find your user account.");
		}

		if (!IsBootstrapEmail(user.Email))
		{
			throw new InvalidOperationException("Only the bootstrap account can use this page.");
		}

		var normalizedNewEmail = NormalizeEmail(newEmail);
		if (IsBootstrapEmail(normalizedNewEmail))
		{
			throw new InvalidOperationException("Choose a different email to disable bootstrap mode.");
		}

		var existing = await _userManager.FindByEmailAsync(normalizedNewEmail);
		if (existing is not null)
		{
			throw new InvalidOperationException("That email is already in use.");
		}

		var setUserNameResult = await _userManager.SetUserNameAsync(user, normalizedNewEmail);
		if (!setUserNameResult.Succeeded)
		{
			throw new InvalidOperationException(BuildErrors(setUserNameResult));
		}

		var setEmailResult = await _userManager.SetEmailAsync(user, normalizedNewEmail);
		if (!setEmailResult.Succeeded)
		{
			throw new InvalidOperationException(BuildErrors(setEmailResult));
		}
	}

	public async Task SignOutAsync()
	{
		await _signInManager.SignOutAsync();
	}

	private async Task EnsureBootstrapAdminRoleAsync(SiteUser user)
	{
		await _siteUserService.AssignRoleAsync(user.Id, SeedData.DefaultAdminSiteId, SiteRole.Admin);
	}

	private async Task<bool> IsBootstrapLockActiveAsync()
	{
		var bootstrapUser = await _userManager.FindByEmailAsync(BootstrapAdminEmail);
		return bootstrapUser is not null;
	}

	private async Task<SiteUser> EnsureUserAsync(string email)
	{
		var existingUser = await _userManager.FindByEmailAsync(email);
		if (existingUser is not null)
		{
			return existingUser;
		}

		var user = new SiteUser
		{
			UserName = email,
			Email = email,
			EmailConfirmed = true
		};

		var result = await _userManager.CreateAsync(user);
		if (!result.Succeeded)
		{
			var errors = string.Join("; ", result.Errors.Select(x => x.Description));
			throw new InvalidOperationException($"Unable to create user for passwordless login: {errors}");
		}

		return user;
	}

	private static bool IsBootstrapEmail(string? email)
	{
		return string.Equals(email, BootstrapAdminEmail, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildErrors(IdentityResult result)
	{
		return string.Join("; ", result.Errors.Select(x => x.Description));
	}

	private static string NormalizeEmail(string email)
	{
		var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			throw new InvalidOperationException("Email is required.");
		}

		return normalized;
	}
}
