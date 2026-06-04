namespace SiteEngine.Services;

public interface ISitePublicAssetService
{
	string BuildDefaultLogoUrl(string hostname);
	string BuildDefaultBannerUrl(string hostname);
	Task EnsureSiteFoldersAsync(string hostname, bool seedDefaults, CancellationToken cancellationToken = default);
}
