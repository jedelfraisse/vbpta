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

	// City/school-district partner logo shown alongside the Division's own
	// logo (e.g. a city or school system seal). Optional — Divisions without
	// one configured simply don't render a partner logo slot.
	public string PartnerLogoUrl { get; set; } = string.Empty;
	public string HeaderText { get; set; } = string.Empty;

	// Color theme. Nullable — an unset value falls through to the parent
	// Division (Local Unit sites only), then to a global default. See
	// SiteThemeExtensions for the resolution chain.
	public string? PrimaryColor { get; set; }
	public string? AccentColor { get; set; }
	public string? TopBarColor { get; set; }
	public string? FooterColor1 { get; set; }
	public string? FooterColor2 { get; set; }
	public string? FooterColor3 { get; set; }
	public string? FooterColor4 { get; set; }

	// Background images. Nullable, same inheritance chain as the theme
	// colors above but with no global default — unset means no image.
	public string? MenuBackgroundImageUrl { get; set; }
	public string? PageBackgroundImageUrl { get; set; }

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
