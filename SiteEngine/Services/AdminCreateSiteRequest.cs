using System.ComponentModel.DataAnnotations;

namespace SiteEngine.Services;

public sealed class AdminCreateSiteRequest
{
	[Required]
	[StringLength(255)]
	public string Hostname { get; set; } = string.Empty;

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
