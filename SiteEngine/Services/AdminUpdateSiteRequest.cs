using System.ComponentModel.DataAnnotations;

namespace SiteEngine.Services;

public sealed class AdminUpdateSiteRequest
{
	[Required]
	[RegularExpression("^\\d{8}$")]
	public string PtaId { get; set; } = string.Empty;

	[StringLength(255)]
	public string Hostname { get; set; } = string.Empty;

	[StringLength(255)]
	public string Domain { get; set; } = string.Empty;

	public bool IsCityWide { get; set; }

	[Required]
	[StringLength(256)]
	public string SiteName { get; set; } = string.Empty;

	[StringLength(512)]
	public string LogoUrl { get; set; } = string.Empty;

	[StringLength(512)]
	public string BannerUrl { get; set; } = string.Empty;

	[StringLength(16)]
	public string PrimaryColor { get; set; } = string.Empty;

	[StringLength(16)]
	public string AccentColor { get; set; } = string.Empty;

	[StringLength(1024)]
	public string WelcomeText { get; set; } = string.Empty;
}
