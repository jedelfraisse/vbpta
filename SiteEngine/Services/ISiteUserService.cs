using SiteEngine.Entities;
using SiteEngine.Identity;

namespace SiteEngine.Services;

/// <summary>
/// Manages user roles scoped to sites.
/// Enables query and assignment of site-specific roles (Admin, BoardMember, Volunteer).
/// </summary>
public interface ISiteUserService
{
	/// <summary>
	/// Gets all roles a user has at a specific site.
	/// </summary>
	Task<IEnumerable<SiteRole>> GetUserRolesAtSiteAsync(string userId, Guid siteId);

	/// <summary>
	/// Checks if a user has a specific role at a specific site.
	/// </summary>
	Task<bool> UserHasRoleAsync(string userId, Guid siteId, SiteRole role);

	/// <summary>
	/// Assigns a role to a user at a specific site.
	/// If the user already has this role at this site, returns the existing assignment.
	/// </summary>
	Task<SiteUserRole> AssignRoleAsync(string userId, Guid siteId, SiteRole role);

	/// <summary>
	/// Removes a role from a user at a specific site.
	/// Returns true if removed, false if the role assignment did not exist.
	/// </summary>
	Task<bool> RemoveRoleAsync(string userId, Guid siteId, SiteRole role);

	/// <summary>
	/// Gets all users with a specific role at a specific site.
	/// </summary>
	Task<IEnumerable<SiteUser>> GetUsersWithRoleAsync(Guid siteId, SiteRole role);

	/// <summary>
	/// Gets the admin contact email for a site (first admin found).
	/// Returns null if no admins found.
	/// </summary>
	Task<string?> GetSiteAdminEmailAsync(Guid siteId);
}
