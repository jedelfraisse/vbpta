namespace SiteEngine.Services;

public sealed class AdminDashboardOverview
{
	public required int TotalSites { get; init; }
	public required int TotalUsers { get; init; }
	public required int AssignedUsers { get; init; }
	public required int GlobalAdmins { get; init; }
}
