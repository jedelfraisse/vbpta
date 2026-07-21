using SiteEngine.Enums;

namespace SiteEngine.Entities;

public class Site
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string PtaId { get; set; } = "00000000";

	public string Hostname { get; set; } = string.Empty;
	public string Domain { get; set; } = string.Empty;

	public SiteType SiteType { get; set; } = SiteType.LocalUnit;

	public Guid? ParentSiteId { get; set; }
	public Site? ParentSite { get; set; }
	public ICollection<Site> ChildSites { get; set; } = new List<Site>();

	public string SiteName { get; set; } = string.Empty;
	public string LogoUrl { get; set; } = string.Empty;
	public string BannerUrl { get; set; } = string.Empty;
	public string PrimaryColor { get; set; } = string.Empty;
	public string AccentColor { get; set; } = string.Empty;
	public string HeaderText { get; set; } = string.Empty;

	public string GiveBacksURL { get; set; } = string.Empty;
	public string FaceBookURL { get; set; } = string.Empty;
	public string TwitterURL { get; set; } = string.Empty;
	public string InstagramURL { get; set; } = string.Empty;
	public string SignUpGeniusURL { get; set; } = string.Empty;

	public SiteStatus SiteStatus { get; set; } = SiteStatus.Inactive;
	public string? LastActiveYear { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
