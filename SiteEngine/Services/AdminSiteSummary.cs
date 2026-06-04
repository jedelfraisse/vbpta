namespace SiteEngine.Services;

public sealed class AdminSiteSummary
{
	public required Guid SiteId { get; init; }
	public required string Hostname { get; init; }
	public required string SiteName { get; init; }
	public required bool IsAdminPortal { get; init; }
	public required int AnnouncementCount { get; init; }
	public required int EventCount { get; init; }
	public required string HealthStatus { get; init; }
}
