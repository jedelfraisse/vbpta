namespace SiteEngine.Services;

public interface IAdminDashboardService
{
	Task<AdminDashboardOverview> GetOverviewAsync(string? currentUserId, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<AdminSiteSummary>> GetSiteSummariesAsync(string? currentUserId, CancellationToken cancellationToken = default);
	Task<AdminSiteDetail?> GetSiteDetailAsync(string? currentUserId, Guid siteId, CancellationToken cancellationToken = default);
	Task<Guid> CreateSiteAsync(string? currentUserId, AdminCreateSiteRequest request, CancellationToken cancellationToken = default);
	Task<bool> UpdateSiteAsync(string? currentUserId, Guid siteId, AdminUpdateSiteRequest request, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<AdminUserSummary>> GetUserSummariesAsync(string? currentUserId, CancellationToken cancellationToken = default);
	Task<AdminUserDetail?> GetUserDetailAsync(string? currentUserId, string userId, CancellationToken cancellationToken = default);
	Task<string> CreateUserAsync(string? currentUserId, AdminCreateUserRequest request, CancellationToken cancellationToken = default);
	Task<bool> UpdateUserAsync(string? currentUserId, string userId, AdminUpdateUserRequest request, CancellationToken cancellationToken = default);
}
