using SiteEngine.Entities;
using SiteEngine.Enums;

namespace SiteEngine.Identity;

public class SiteUserRole
{
	public Guid Id { get; set; } = Guid.NewGuid();

	// FK: Site
	public Guid SiteId { get; set; }
	public Site Site { get; set; } = null!;

	// FK: SiteUser
	public Guid SiteUserId { get; set; }
	public SiteUser SiteUser { get; set; } = null!;

	// Built-in role OR custom role
	public SiteRole? Role { get; set; } // President, Treasurer, etc.

	public Guid? CustomRoleId { get; set; }
	public CustomRole? CustomRole { get; set; }

	// School year scoping
	public string SchoolYear { get; set; } = string.Empty;

	// Partial-year terms
	public DateTimeOffset? StartUtc { get; set; }
	public DateTimeOffset? EndUtc { get; set; }

	public bool IsPrimaryContact { get; set; }
}
