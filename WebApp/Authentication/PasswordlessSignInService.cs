using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<SiteUser> _userManager = userManager;
    private readonly SignInManager<SiteUser> _signInManager = signInManager;
    private readonly IEmailLoginSender _emailLoginSender = emailLoginSender;
    private readonly ISiteResolver _siteResolver = siteResolver;
    private readonly ISiteUserService _siteUserService = siteUserService;

    public async Task RequestCodeAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        // Ensure user exists
        var user = await EnsureUserAsync(normalizedEmail);

        // Generate and send code
        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        await _emailLoginSender.SendCodeAsync(normalizedEmail, code, cancellationToken);
    }

    public async Task<bool> SignInWithCodeAsync(string email, string code, string host)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedCode = code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedCode))
            return false;

        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
            return false;

        var valid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultEmailProvider,
            normalizedCode);

        if (!valid)
            return false;

        await _signInManager.SignInAsync(user, isPersistent: false);
        return true;
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    private async Task<SiteUser> EnsureUserAsync(string email)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return existingUser;

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

    private static string NormalizeEmail(string email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Email is required.");

        return normalized;
    }
}
