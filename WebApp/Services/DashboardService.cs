using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public record MembershipSummary(Guid SiteId, string SiteName, SiteType SiteType, SiteRole? Role, string? CustomRoleName, string SchoolYear);
public record SystemStats(int DivisionCount, int UnitCount, int UserCount, int MembershipCount);
public record UserSummary(
	string IdentityUserId,
	string Email,
	string DisplayName,
	bool EmailConfirmed,
	DateTimeOffset? FirstLoginUtc,
	DateTimeOffset? LastLoginUtc,
	Guid? LastLoginSiteId,
	string? LastLoginSiteName,
	int LoginCount);
public record RoleAssignmentSummary(string UserEmail, string SiteName, SiteRole? Role, string? CustomRoleName, string SchoolYear);

// Read (and, for site status, write) access behind the Dashboard's real —
// as opposed to "coming soon" — sections. Every query here maps to data that
// already exists in the schema; nothing here introduces new tables.
public class DashboardService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<List<MembershipSummary>> GetMembershipsAsync(string identityUserId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.SiteUserRoles
			.Where(r => r.SiteUser.IdentityUserId == identityUserId)
			.Select(r => new MembershipSummary(
				r.SiteId,
				r.Site.SiteName,
				r.Site.SiteType,
				r.Role,
				r.CustomRole != null ? r.CustomRole.Name : null,
				r.SchoolYear))
			.ToListAsync(cancellationToken);
	}

	public async Task<List<PortalTools>> GetEnabledToolsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.PortalTools
			.Where(t => t.IsEnabled)
			.OrderBy(t => t.SortOrder)
			.ToListAsync(cancellationToken);
	}

	public async Task<SystemStats> GetSystemStatsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return new SystemStats(
			await db.Sites.CountAsync(s => s.SiteType == SiteType.Division, cancellationToken),
			await db.Sites.CountAsync(s => s.SiteType == SiteType.LocalUnit, cancellationToken),
			await db.Users.CountAsync(cancellationToken),
			await db.SiteUserRoles.CountAsync(cancellationToken));
	}

	public async Task<List<Site>> GetSitesByTypeAsync(SiteType type, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.Sites
			.Where(s => s.SiteType == type)
			.OrderBy(s => s.SiteName)
			.ToListAsync(cancellationToken);
	}

	public async Task<List<UserSummary>> GetAllUsersAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.Users
			.Select(u => new UserSummary(
				u.Id,
				u.Email ?? "",
				db.SiteUsers.Where(su => su.IdentityUserId == u.Id).Select(su => su.DisplayName).FirstOrDefault() ?? "",
				u.EmailConfirmed,
				u.FirstLoginUtc,
				u.LastLoginUtc,
				u.LastLoginSiteId,
				db.Sites.Where(s => s.Id == u.LastLoginSiteId).Select(s => s.SiteName).FirstOrDefault(),
				u.LoginCount))
			.ToListAsync(cancellationToken);
	}

	public async Task<List<RoleAssignmentSummary>> GetAllRoleAssignmentsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.SiteUserRoles
			.Select(r => new RoleAssignmentSummary(
				r.SiteUser.PreferredEmail ?? "",
				r.Site.SiteName,
				r.Role,
				r.CustomRole != null ? r.CustomRole.Name : null,
				r.SchoolYear))
			.ToListAsync(cancellationToken);
	}

	public async Task<SiteStatus> GetSiteStatusAsync(Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.Sites
			.Where(s => s.Id == siteId)
			.Select(s => s.SiteStatus)
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task SetSiteStatusAsync(Guid siteId, SiteStatus status, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FindAsync([siteId], cancellationToken)
			?? throw new InvalidOperationException($"Site '{siteId}' not found.");

		site.SiteStatus = status;
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);
	}

	public async Task<PortalConfig?> GetPortalConfigAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.PortalConfigs.FindAsync([SeedData.DefaultGlobalConfigId], cancellationToken);
	}
}
