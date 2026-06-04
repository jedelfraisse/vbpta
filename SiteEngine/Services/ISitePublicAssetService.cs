namespace SiteEngine.Services;

public interface ISitePublicAssetService
{
	string BuildDefaultLogoUrl(string assetKey);
	string BuildDefaultBannerUrl(string assetKey);
	Task EnsureSiteFoldersAsync(string assetKey, bool seedDefaults, CancellationToken cancellationToken = default);
	Task RenameSiteFolderAsync(string originalAssetKey, string newAssetKey, CancellationToken cancellationToken = default);
}
