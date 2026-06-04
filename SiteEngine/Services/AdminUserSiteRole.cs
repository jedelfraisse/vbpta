using SiteEngine.Entities;

namespace SiteEngine.Services;

public sealed class AdminUserSiteRole
{
	public required Guid SiteId { get; init; }
	public required string SiteName { get; init; }
	public required string Hostname { get; init; }
	public required SiteRole Role { get; init; }
}
