using SiteEngine.Config;
using SiteEngine.Entities;

namespace SiteEngine.Sites;

public static class SiteMapping
{
	public static SiteConfig ToSiteConfig(this Site site)
	{
		var logoUrl = ResolveSiteAssetUrl(site, site.LogoUrl, isLogo: true);
		var bannerUrl = ResolveSiteAssetUrl(site, site.BannerUrl, isLogo: false);

		return new SiteConfig
		{
			SiteName = site.SiteName,
			LogoUrl = logoUrl,
			BannerUrl = bannerUrl,
			PrimaryColor = site.PrimaryColor,
			AccentColor = site.AccentColor,
			WelcomeText = site.WelcomeText
		};
	}

	private static string ResolveSiteAssetUrl(Site site, string? configuredUrl, bool isLogo)
	{
		var normalized = configuredUrl?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			normalized = isLogo ? "images/logo.png" : "images/banner.png";
		}

		if (site.IsAdminPortal || IsAbsoluteUrl(normalized))
		{
			return normalized;
		}

		var assetKey = site.PtaId.Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(assetKey))
		{
			assetKey = site.Hostname.Trim().ToLowerInvariant();
		}

		return $"/site-data/{assetKey}/{normalized.TrimStart('/')}";
	}

	private static bool IsAbsoluteUrl(string value)
	{
		return value.StartsWith("/", StringComparison.Ordinal)
			|| value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
	}
}
