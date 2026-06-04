namespace SiteEngine.Services;

public sealed class AdminUserDetail
{
	public required string UserId { get; init; }
	public required string Email { get; init; }
	public required bool IsGlobalAdmin { get; init; }
	public required IReadOnlyList<AdminUserSiteRole> SiteRoles { get; init; }
}
