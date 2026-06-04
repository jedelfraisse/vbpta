using SiteEngine.Entities;

namespace SiteEngine.Data;

public static class SeedData
{
	public static readonly Guid DefaultAdminSiteId = Guid.Parse("0F89AC2B-A0AC-40B8-B886-FD117E35903C");
	public const string DefaultAdminPtaId = "00000000";
	public const string DefaultAdminHostname = "admin";
	public static readonly Guid DefaultCitySiteId = Guid.Parse("2B30D683-EA4B-4E9E-B616-17A2198E3B79");
	public const string DefaultCityPtaId = "10000000";
	public const string DefaultCityHostname = "";
	public const int DefaultGlobalConfigId = 1;

	public static readonly Site DefaultAdminSite = new()
	{
		Id = DefaultAdminSiteId,
		PtaId = DefaultAdminPtaId,
		Hostname = DefaultAdminHostname,
		Domain = string.Empty,
		IsAdminPortal = true,
		IsCityWide = false,
		SiteName = "City Wide PTA Admin",
		LogoUrl = "/images/logo.png",
		BannerUrl = "/images/banner.png",
		PrimaryColor = "#003366",
		AccentColor = "#FFCC00",
		WelcomeText = "Monitor and manage all PTA sites from the this admin portal.",
		CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
	};

	public static readonly Site DefaultCitySite = new()
	{
		Id = DefaultCitySiteId,
		PtaId = DefaultCityPtaId,
		Hostname = DefaultCityHostname,
		Domain = string.Empty,
		IsAdminPortal = false,
		IsCityWide = true,
		SiteName = "Virginia Beach Council of PTAs",
		LogoUrl = "/images/logo.png",
		BannerUrl = "/images/banner.png",
		PrimaryColor = "#003366",
		AccentColor = "#FFCC00",
		WelcomeText = "Welcome to our community! We are dedicated to supporting students, families, and educators.",
		CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
	};

	public static readonly GlobalConfig DefaultGlobalConfig = new()
	{
		Id = DefaultGlobalConfigId,
		RootDomain = "localhost",
		PlatformDomain = "localhost",
		SmtpHost = string.Empty,
		SmtpPort = 587,
		SmtpFromAddress = "help@localhost",
		SmtpUsername = string.Empty,
		SmtpPassword = string.Empty,
		UseSsl = true
	};
}
