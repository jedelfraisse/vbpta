namespace SiteEngine.Entities;

/// <summary>
/// Defines available roles that can be assigned to users at a site.
/// Each site manages its own role assignments independent of other sites.
/// </summary>
public enum SiteRole
{
	Admin,
	BoardMember,
	Volunteer
}
