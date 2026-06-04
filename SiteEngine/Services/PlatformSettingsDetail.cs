namespace SiteEngine.Services;

public sealed class PlatformSettingsDetail
{
	public required string RootDomain { get; init; }
	public required string PlatformDomain { get; init; }
	public required string SmtpHost { get; init; }
	public required int SmtpPort { get; init; }
	public required string SmtpFromAddress { get; init; }
	public required string SmtpUsername { get; init; }
	public required string SmtpPassword { get; init; }
	public required bool UseSsl { get; init; }
	public required Guid CityWideSiteId { get; init; }
	public required string CityWidePtaId { get; init; }
	public required string CityWideSiteName { get; init; }
}
