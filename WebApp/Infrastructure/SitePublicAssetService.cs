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

	public string BuildDefaultLogoUrl(string assetKey)
	{
		var normalized = NormalizeAssetKey(assetKey);
		return $"/{SiteDataFolder}/{normalized}/{ImagesFolder}/{DefaultLogoFileName}";
	}

	public string BuildDefaultBannerUrl(string assetKey)
	{
		var normalized = NormalizeAssetKey(assetKey);
		return $"/{SiteDataFolder}/{normalized}/{ImagesFolder}/{DefaultBannerFileName}";
	}

	public Task EnsureSiteFoldersAsync(string assetKey, bool seedDefaults, CancellationToken cancellationToken = default)
	{
		var normalized = NormalizeAssetKey(assetKey);
		if (string.IsNullOrWhiteSpace(normalized))
		{
			return Task.CompletedTask;
		}

		var webRootPath = _webHostEnvironment.WebRootPath;
		if (string.IsNullOrWhiteSpace(webRootPath))
		{
			_logger.LogWarning("Unable to ensure site folders for {AssetKey}: WebRootPath is not available.", normalized);
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
				Path.Combine(webRootPath, "images", "banner.png"),
				Path.Combine(siteImagePath, DefaultBannerFileName));
		}

		return Task.CompletedTask;
	}

	public Task RenameSiteFolderAsync(string originalAssetKey, string newAssetKey, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var normalizedOriginal = NormalizeAssetKey(originalAssetKey);
		var normalizedNew = NormalizeAssetKey(newAssetKey);
		if (string.IsNullOrWhiteSpace(normalizedOriginal)
			|| string.IsNullOrWhiteSpace(normalizedNew)
			|| string.Equals(normalizedOriginal, normalizedNew, StringComparison.Ordinal))
		{
			return Task.CompletedTask;
		}

		var webRootPath = _webHostEnvironment.WebRootPath;
		if (string.IsNullOrWhiteSpace(webRootPath))
		{
			_logger.LogWarning("Unable to rename site-data folder from {Original} to {New}: WebRootPath is not available.", normalizedOriginal, normalizedNew);
			return Task.CompletedTask;
		}

		var originalPath = Path.Combine(webRootPath, SiteDataFolder, normalizedOriginal);
		var newPath = Path.Combine(webRootPath, SiteDataFolder, normalizedNew);
		if (!Directory.Exists(originalPath) || Directory.Exists(newPath))
		{
			return Task.CompletedTask;
		}

		Directory.Move(originalPath, newPath);
		return Task.CompletedTask;
	}

	private static string NormalizeAssetKey(string assetKey)
	{
		return assetKey?.Trim().ToLowerInvariant() ?? string.Empty;
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
