namespace WebApp.Services;

// Prevents an attacker-controlled returnUrl query parameter from being used
// for an open redirect — only same-origin, root-relative paths are allowed.
// Shared by every place that redirects to a caller-supplied returnUrl
// (passwordless sign-in completion, profile completion).
public static class LocalUrlGuard
{
	public static bool IsLocalUrl(string? url) =>
		!string.IsNullOrWhiteSpace(url) &&
		url.StartsWith('/') &&
		!url.StartsWith("//") &&
		!url.StartsWith("/\\");
}
