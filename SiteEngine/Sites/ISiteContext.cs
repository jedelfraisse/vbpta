using SiteEngine.Config;
using SiteEngine.Entities;

namespace SiteEngine.Sites;

public interface ISiteContext
{
	Site? CurrentSite { get; }
	SiteConfig SiteConfig { get; }
	bool IsAdminContext { get; }
	Task InitializeAsync(string host, CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks if a user has any role (is authorized) at the current site.
	/// Returns false if user is null/empty or has no roles at current site.
	/// </summary>
	Task<bool> IsUserAuthorizedAtCurrentSiteAsync(string? userId);

	/// <summary>
	/// Checks if a user has a specific role at the current site.
	/// Returns false if user is null/empty, site not set, or user doesn't have role.
	/// </summary>
	Task<bool> UserHasRoleAtCurrentSiteAsync(string? userId, SiteRole role);

	/// <summary>
	/// Checks if a user has global admin access (Admin role on the admin site).
	/// </summary>
	Task<bool> UserHasGlobalAdminRoleAsync(string? userId);

	/// <summary>
	/// Checks if user can access the current site's admin page as local admin or global admin.
	/// </summary>
	Task<bool> UserHasSiteAdminAccessAsync(string? userId);

	/// <summary>
	/// Gets the contact email for the current site admin.
	/// Used in "contact admin" messages when user has no access.
	/// </summary>
	Task<string?> GetCurrentSiteAdminEmailAsync();
}
