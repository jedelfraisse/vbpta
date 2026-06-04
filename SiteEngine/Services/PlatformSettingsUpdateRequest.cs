using System.ComponentModel.DataAnnotations;

namespace SiteEngine.Services;

public sealed class PlatformSettingsUpdateRequest
{
	[Required]
	[StringLength(255)]
	public string RootDomain { get; set; } = string.Empty;

	[Required]
	[StringLength(255)]
	public string PlatformDomain { get; set; } = string.Empty;

	[StringLength(255)]
	public string SmtpHost { get; set; } = string.Empty;

	[Range(1, 65535)]
	public int SmtpPort { get; set; } = 587;

	[Required]
	[StringLength(255)]
	[EmailAddress]
	public string SmtpFromAddress { get; set; } = string.Empty;

	[StringLength(255)]
	public string SmtpUsername { get; set; } = string.Empty;

	[StringLength(255)]
	public string SmtpPassword { get; set; } = string.Empty;

	public bool UseSsl { get; set; } = true;

	[Required]
	[RegularExpression("^\\d{8}$")]
	public string CityWidePtaId { get; set; } = string.Empty;

	[Required]
	[StringLength(256)]
	public string CityWideSiteName { get; set; } = string.Empty;
}
