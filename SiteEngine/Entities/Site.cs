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

	// This site's generated PTA-style masthead logo (site name + tagline
	// baked into a PNG by PtaLogoGenerationService) — never hand-uploaded.
	// Null until first generated; SiteLayoutBase/*Layout.razor generate and
	// persist one lazily on first render if still unset. Regenerated only
	// when an admin clicks "Generate PTA Logo" (SiteAdminService.GeneratePtaLogoAsync).
	public string? LogoUrl { get; set; }

	public string BannerUrl { get; set; } = string.Empty;

	// Optional admin-uploaded alternate/official PTA logo, shown alongside
	// the generated LogoUrl rather than replacing it.
	public string? PTALogoVariantUrl { get; set; }

	// City/school-district partner logo shown alongside this site's own
	// logo (e.g. a city or school system seal). Optional — sites without one
	// configured simply don't render a partner logo slot. Local Units
	// inherit this from their parent Division when unset (see
	// SiteThemeExtensions.ResolvedPartnerLogoUrl).
	public string? PartnerLogoUrl { get; set; }

	// School district/city logo. Settable directly on a Division or a Local
	// Unit; a Local Unit with no value of its own falls back to its parent
	// Division's value (see SiteThemeExtensions.ResolvedDistrictLogoUrl).
	public string? DistrictLogoUrl { get; set; }

	// Local Unit-only: the individual school's crest/seal shown in the Unit
	// masthead. Falls back to the parent Division's DistrictLogoUrl when unset.
	public string? SchoolCrestUrl { get; set; }

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

	// Masthead logo sizing (Division only for now — Local Unit masthead logos
	// stay a fixed size). Each logo slot in the row (the generated PTA logo,
	// the uploaded variant, the district logo, the partner logo) gets its own
	// explicit width/height box in pixels; unset falls back to
	// MastheadLogoDefaultWidth/Height (88x220 if that's unset too — see
	// SiteLayoutBase.LogoBoxStyle). PreserveAspectRatio picks how the image
	// fills that box: true = scaled down to fit without distortion (CSS
	// object-fit:contain — some empty space in the box is possible), false =
	// stretched to exactly fill it (object-fit:fill — may distort).
	public int? MastheadLogoDefaultWidth { get; set; }
	public int? MastheadLogoDefaultHeight { get; set; }

	public int? GeneratedLogoWidth { get; set; }
	public int? GeneratedLogoHeight { get; set; }
	public bool GeneratedLogoPreserveAspectRatio { get; set; } = true;

	public int? PtaVariantLogoWidth { get; set; }
	public int? PtaVariantLogoHeight { get; set; }
	public bool PtaVariantLogoPreserveAspectRatio { get; set; } = true;

	public int? DistrictLogoWidth { get; set; }
	public int? DistrictLogoHeight { get; set; }
	public bool DistrictLogoPreserveAspectRatio { get; set; } = true;

	public int? PartnerLogoWidth { get; set; }
	public int? PartnerLogoHeight { get; set; }
	public bool PartnerLogoPreserveAspectRatio { get; set; } = true;

	// Background images. Nullable, same inheritance chain as the theme
	// colors above but with no global default — unset means no image.
	public string? MenuBackgroundImageUrl { get; set; }
	public string? PageBackgroundImageUrl { get; set; }

	public string GiveBacksURL { get; set; } = string.Empty;
	public string FaceBookURL { get; set; } = string.Empty;
	public string TwitterURL { get; set; } = string.Empty;
	public string InstagramURL { get; set; } = string.Empty;
	public string SignUpGeniusURL { get; set; } = string.Empty;

	// Optional link to a site hosted outside this portal (e.g. a school's own
	// existing website). Relevant mainly for ActiveListed/MembersOnly/Pending
	// directory entries; Active sites use the hosted site instead.
	public string? ExternalUrl { get; set; }

	public SiteStatus SiteStatus { get; set; } = SiteStatus.Inactive;
	public string? LastActiveYear { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
