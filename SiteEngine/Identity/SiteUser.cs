namespace SiteEngine.Identity;

public class SiteUser
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string IdentityUserId { get; set; } = string.Empty; // FK to IdentityUser
	public ApplicationUser IdentityUser { get; set; } = null!;

	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string DisplayName { get; set; } = string.Empty;

	public string? PreferredEmail { get; set; }
	public string? Phone { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
