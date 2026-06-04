using SiteEngine.Services;

namespace WebApp.Infrastructure;

public class SitePublicAssetService(
	IWebHostEnvironment webHostEnvironment,
	ILogger<SitePublicAssetService> logger) : ISitePublicAssetService
{
	private const string SiteDataFolder = "site-data";
	private const string ImagesFolder = "images";
	private const string DefaultLogoFileName = "logo.png";
	private const string DefaultBannerFileName = "banner.png";

	private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
	private readonly ILogger<SitePublicAssetService> _logger = logger;

	public string BuildDefaultLogoUrl(string hostname)
	{
		var normalized = NormalizeHostname(hostname);
		return $"/{SiteDataFolder}/{normalized}/{ImagesFolder}/{DefaultLogoFileName}";
	}

	public string BuildDefaultBannerUrl(string hostname)
	{
		var normalized = NormalizeHostname(hostname);
		return $"/{SiteDataFolder}/{normalized}/{ImagesFolder}/{DefaultBannerFileName}";
	}

	public Task EnsureSiteFoldersAsync(string hostname, bool seedDefaults, CancellationToken cancellationToken = default)
	{
		var normalized = NormalizeHostname(hostname);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return Task.CompletedTask;
		}

		var webRootPath = _webHostEnvironment.WebRootPath;
		if (string.IsNullOrWhiteSpace(webRootPath))
		{
			_logger.LogWarning("Unable to ensure site folders for {Hostname}: WebRootPath is not available.", normalized);
			return Task.CompletedTask;
		}

		var siteImagePath = Path.Combine(webRootPath, SiteDataFolder, normalized, ImagesFolder);
		Directory.CreateDirectory(siteImagePath);

		if (seedDefaults)
		{
			CopyIfMissing(
				Path.Combine(webRootPath, "images", "logo.png"),
				Path.Combine(siteImagePath, DefaultLogoFileName));
			CopyIfMissing(
				Path.Combine(webRootPath, "images", "TopBanner.png"),
				Path.Combine(siteImagePath, DefaultBannerFileName));
		}

		return Task.CompletedTask;
	}

	private static string NormalizeHostname(string hostname)
	{
		return hostname?.Trim().ToLowerInvariant() ?? string.Empty;
	}

	private void CopyIfMissing(string source, string destination)
	{
		if (!File.Exists(source) || File.Exists(destination))
		{
			return;
		}

		File.Copy(source, destination);
	}
}
