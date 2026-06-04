namespace SiteEngine.Services;

public sealed class AdminUserSummary
{
	public required string UserId { get; init; }
	public required string Email { get; init; }
	public required bool IsGlobalAdmin { get; init; }
	public required int AssignedSiteCount { get; init; }
}
