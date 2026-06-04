namespace SiteEngine.Entities;

public class Site
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string PtaId { get; set; } = "00000000";
	public string Hostname { get; set; } = string.Empty;
	public string Domain { get; set; } = string.Empty;
	public bool IsAdminPortal { get; set; }
	public bool IsCityWide { get; set; }
	public string SiteName { get; set; } = string.Empty;
	public string LogoUrl { get; set; } = string.Empty;
	public string BannerUrl { get; set; } = string.Empty;
	public string PrimaryColor { get; set; } = string.Empty;
	public string AccentColor { get; set; } = string.Empty;
	public string WelcomeText { get; set; } = string.Empty;
	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
	public ICollection<SiteEvent> Events { get; set; } = new List<SiteEvent>();
	public ICollection<SiteUserRole> UserRoles { get; set; } = new List<SiteUserRole>();
}
