namespace SiteEngine.Services;

public interface IPlatformConfigurationService
{
	Task<bool> IsInitialSetupRequiredAsync(CancellationToken cancellationToken = default);
	Task TestSmtpConnectionAsync(InitialSetupRequest request, CancellationToken cancellationToken = default);
	Task<string> SendSetupTestEmailAsync(InitialSetupRequest request, CancellationToken cancellationToken = default);
	Task CompleteInitialSetupAsync(InitialSetupRequest request, CancellationToken cancellationToken = default);
	Task<PlatformSettingsDetail> GetPlatformSettingsAsync(string? currentUserId, CancellationToken cancellationToken = default);
	Task UpdatePlatformSettingsAsync(string? currentUserId, PlatformSettingsUpdateRequest request, CancellationToken cancellationToken = default);
}
