using SiteEngine.Entities;

namespace SiteEngine.Data;

public static class SeedData
{
	public static readonly Guid DefaultAdminSiteId = Guid.Parse("0F89AC2B-A0AC-40B8-B886-FD117E35903C");
	public const string DefaultAdminHostname = "admin.localhost";
	public static readonly Guid DefaultCitySiteId = Guid.Parse("2B30D683-EA4B-4E9E-B616-17A2198E3B79");
	public const string DefaultCityHostname = "localhost";

	public static readonly Site DefaultAdminSite = new()
	{
		Id = DefaultAdminSiteId,
		Hostname = DefaultAdminHostname,
		IsAdminPortal = true,
		SiteName = "VBPTA Admin",
		LogoUrl = "/images/vbpta-logo.png",
		BannerUrl = "/images/TopBanner.png",
		PrimaryColor = "#003366",
		AccentColor = "#FFCC00",
		WelcomeText = "Monitor and manage all VBPTA sites from the admin portal.",
		CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
	};

	public static readonly Site DefaultCitySite = new()
	{
		Id = DefaultCitySiteId,
		Hostname = DefaultCityHostname,
		IsAdminPortal = false,
		SiteName = "Virginia Beach Council of PTAs",
		LogoUrl = "/images/vbpta-logo.png",
		BannerUrl = "/images/TopBanner.png",
		PrimaryColor = "#003366",
		AccentColor = "#FFCC00",
		WelcomeText = "Welcome to our community! We are dedicated to supporting students, families, and educators.",
		CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		UpdatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
	};
}
