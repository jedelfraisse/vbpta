using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Services;

namespace WebApp.Infrastructure;

public static class SitePublicAssetFolderExtensions
{
	public static async Task EnsureSitePublicAssetFoldersAsync(this WebApplication app)
	{
		await using var scope = app.Services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
		var sitePublicAssetService = scope.ServiceProvider.GetRequiredService<ISitePublicAssetService>();

		await using var dbContext = await dbContextFactory.CreateDbContextAsync();
		var sites = await dbContext.Sites
			.OrderBy(x => x.Hostname)
			.ToListAsync();

		var hasUpdates = false;
		foreach (var site in sites)
		{
			await sitePublicAssetService.EnsureSiteFoldersAsync(site.Hostname, seedDefaults: true);

			if (site.IsAdminPortal)
			{
				continue;
			}

			var normalizedLogo = NormalizeSiteRelativeAssetPath(site.Hostname, site.LogoUrl, "images/logo.png");
			var normalizedBanner = NormalizeSiteRelativeAssetPath(site.Hostname, site.BannerUrl, "images/banner.png");
			if (!string.Equals(site.LogoUrl, normalizedLogo, StringComparison.Ordinal))
			{
				site.LogoUrl = normalizedLogo;
				hasUpdates = true;
			}

			if (!string.Equals(site.BannerUrl, normalizedBanner, StringComparison.Ordinal))
			{
				site.BannerUrl = normalizedBanner;
				hasUpdates = true;
			}
		}

		if (hasUpdates)
		{
			await dbContext.SaveChangesAsync();
		}
	}

	private static string NormalizeSiteRelativeAssetPath(string hostname, string? currentUrl, string defaultRelativePath)
	{
		var normalized = currentUrl?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return defaultRelativePath;
		}

		if (normalized.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
		{
			return defaultRelativePath;
		}

		var hostSegment = hostname.Trim().ToLowerInvariant();
		var sitePrefix = $"/site-data/{hostSegment}/";
		if (normalized.StartsWith(sitePrefix, StringComparison.OrdinalIgnoreCase))
		{
			return normalized[sitePrefix.Length..].TrimStart('/');
		}

		return normalized;
	}
}
