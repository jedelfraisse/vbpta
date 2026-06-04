using SiteEngine.Identity;

namespace SiteEngine.Entities;

/// <summary>
/// Maps a user to a site and role. Enables site-scoped, role-based access control.
/// A user can have different roles at different sites (e.g., Admin at admin.localhost, Volunteer at luxfordes.localhost).
/// </summary>
public class SiteUserRole
{
	public int Id { get; set; }
	public string UserId { get; set; } = null!;
	public Guid SiteId { get; set; }
	public SiteRole Role { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	// Navigation properties
	public SiteUser User { get; set; } = null!;
	public Site Site { get; set; } = null!;
}
