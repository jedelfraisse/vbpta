using SiteEngine.Services;

namespace WebApp.Infrastructure;

public class SitePublicAssetService(
    IWebHostEnvironment webHostEnvironment,
    ILogger<SitePublicAssetService> logger) : ISitePublicAssetService
{
    private const string SitesFolder = "sites";
    private const string ImagesFolder = "images";
    private const string DefaultLogoFileName = "logo.png";
    private const string DefaultBannerFileName = "banner.png";

    private readonly IWebHostEnvironment _env = webHostEnvironment;
    private readonly ILogger<SitePublicAssetService> _logger = logger;

    public string BuildDefaultLogoUrl(string assetKey)
    {
        var normalized = Normalize(assetKey);
        return $"/{SitesFolder}/{normalized}/{ImagesFolder}/{DefaultLogoFileName}";
    }

    public string BuildDefaultBannerUrl(string assetKey)
    {
        var normalized = Normalize(assetKey);
        return $"/{SitesFolder}/{normalized}/{ImagesFolder}/{DefaultBannerFileName}";
    }

    public Task EnsureSiteFoldersAsync(string assetKey, bool seedDefaults, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(assetKey);
        if (string.IsNullOrWhiteSpace(normalized))
            return Task.CompletedTask;

        var root = Path.Combine(_env.WebRootPath, SitesFolder, normalized);
        var images = Path.Combine(root, ImagesFolder);

        Directory.CreateDirectory(images);

        if (seedDefaults)
        {
            var defaults = Path.Combine(_env.WebRootPath, "defaults");

            CopyIfMissing(
                Path.Combine(defaults, "default-logo.png"),
                Path.Combine(images, DefaultLogoFileName));

            CopyIfMissing(
                Path.Combine(defaults, "default-banner.png"),
                Path.Combine(images, DefaultBannerFileName));
        }

        return Task.CompletedTask;
    }

    public Task RenameSiteFolderAsync(string originalKey, string newKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var oldKey = Normalize(originalKey);
        var newKeyNorm = Normalize(newKey);

        if (string.IsNullOrWhiteSpace(oldKey) ||
            string.IsNullOrWhiteSpace(newKeyNorm) ||
            oldKey == newKeyNorm)
            return Task.CompletedTask;

        var root = Path.Combine(_env.WebRootPath, SitesFolder);
        var oldPath = Path.Combine(root, oldKey);
        var newPath = Path.Combine(root, newKeyNorm);

        if (Directory.Exists(oldPath) && !Directory.Exists(newPath))
            Directory.Move(oldPath, newPath);

        return Task.CompletedTask;
    }

    private static string Normalize(string key) =>
        key?.Trim().ToLowerInvariant() ?? string.Empty;

    private void CopyIfMissing(string source, string destination)
    {
        if (File.Exists(source) && !File.Exists(destination))
            File.Copy(source, destination);
    }
}
