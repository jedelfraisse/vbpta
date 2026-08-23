using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

// One row per site the user holds any role on — RoleLabels carries every
// role/custom-role they hold there (e.g. the wizard-created admin holds both
// SuperAdmin and SiteAdmin on the Portal); Role is the highest of those, used
// for admin-gating and the role filter. Hostname/Domain back the "Visit
// site" link (SiteUrlHelper.BuildPublicUrl).
public record MembershipSummary(Guid SiteId, string SiteName, SiteType SiteType, SiteRole? Role, List<string> RoleLabels, string SchoolYear, string Hostname, string Domain);
// A site the user has actually logged into (LoginHistory) but doesn't hold a
// role on — distinct from MembershipSummary, which comes from SiteUserRoles.
public record VisitedSiteSummary(Guid SiteId, string SiteName, SiteType SiteType, DateTimeOffset LastLoginUtc, string Hostname, string Domain);
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
public record UnitSiteSummary(Guid Id, string SiteName, string PtaId, Guid? DivisionId, string? DivisionName, SiteStatus SiteStatus);
public record UnitSitePage(List<UnitSiteSummary> Items, int TotalCount, int PageNumber, int PageSize);

// A single row in the public /unit-sites directory (either group).
public record DirectorySite(
	Guid Id, string SiteName, SiteType SiteType, string PtaId,
	Guid? ParentSiteId, string? ParentSiteName,
	SiteStatus SiteStatus, string? ExternalUrl);
public record DirectorySites(List<DirectorySite> Divisions, List<DirectorySite> LocalUnits);

// Backs /unit-sites/{SiteId}. Includes Hostname/Domain (not shown on the page
// itself) so the Details component can build the hosted-site redirect URL for
// Status == Active via SiteUrlHelper.BuildPublicUrl without a second query.
public record SiteDetails(
	Guid Id, string SiteName, SiteType SiteType, string PtaId,
	Guid? ParentSiteId, string? ParentSiteName,
	SiteStatus SiteStatus, string? ExternalUrl,
	string Hostname, string Domain);

// Read (and, for site status, write) access behind the Dashboard's real —
// as opposed to "coming soon" — sections. Every query here maps to data that
// already exists in the schema; nothing here introduces new tables.
public class DashboardService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<List<MembershipSummary>> GetMembershipsAsync(string identityUserId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var rows = await db.SiteUserRoles
			.Where(r => r.SiteUser.IdentityUserId == identityUserId)
			.Select(r => new
			{
				r.SiteId,
				r.Site.SiteName,
				r.Site.SiteType,
				r.Site.Hostname,
				r.Site.Domain,
				r.Role,
				RoleLabel = r.CustomRole != null ? r.CustomRole.Name : (r.Role != null ? r.Role.Value.ToString() : "Member"),
				r.SchoolYear
			})
			.ToListAsync(cancellationToken);

		// A user can hold more than one role on the same site for the same
		// school year (e.g. the wizard-created admin gets both SuperAdmin and
		// SiteAdmin on the Portal) — SiteUserRoles has one row per role, so
		// group those into a single row per (site, school year) rather than
		// showing the same site once per role.
		return rows
			.GroupBy(r => (r.SiteId, r.SchoolYear))
			.Select(g =>
			{
				var first = g.First();
				return new MembershipSummary(
					first.SiteId,
					first.SiteName,
					first.SiteType,
					g.Max(r => r.Role),
					g.Select(r => r.RoleLabel).Distinct().ToList(),
					first.SchoolYear,
					first.Hostname,
					first.Domain);
			})
			.ToList();
	}

	// Backs the Dashboard's "Also show sites you've logged into" toggle on
	// Units & Divisions — role-based memberships are the primary, always-shown
	// list; this is the opt-in supplement for sites visited without a role
	// (e.g. before a role was granted, or a role that was later removed).
	// excludeSiteIds should be the caller's current MembershipSummary site IDs
	// so a site never appears in both lists at once.
	public async Task<List<VisitedSiteSummary>> GetVisitedSitesAsync(
		string identityUserId, IEnumerable<Guid> excludeSiteIds, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var exclude = excludeSiteIds.ToList();

		var visited = await db.LoginHistories
			.Where(h => h.UserId == identityUserId && h.SiteId != null && !exclude.Contains(h.SiteId!.Value))
			.GroupBy(h => h.SiteId!.Value)
			.Select(g => new { SiteId = g.Key, LastLoginUtc = g.Max(h => h.LoginUtc) })
			.ToListAsync(cancellationToken);

		if (visited.Count == 0)
			return [];

		var siteIds = visited.Select(v => v.SiteId).ToList();
		var sites = await db.Sites
			.Where(s => siteIds.Contains(s.Id))
			.ToDictionaryAsync(s => s.Id, cancellationToken);

		return visited
			.Where(v => sites.ContainsKey(v.SiteId))
			.Select(v => new VisitedSiteSummary(
				v.SiteId, sites[v.SiteId].SiteName, sites[v.SiteId].SiteType, v.LastLoginUtc,
				sites[v.SiteId].Hostname, sites[v.SiteId].Domain))
			.OrderByDescending(v => v.LastLoginUtc)
			.ToList();
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

	// The only statuses the public directory (list or detail) ever surfaces.
	// Everything else - Inactive (not yet part of this directory model),
	// Archived, Disabled - is treated as "do not list".
	private static readonly SiteStatus[] DirectoryListedStatuses =
		[SiteStatus.Active, SiteStatus.ActiveListed, SiteStatus.MembersOnly, SiteStatus.Pending];

	// Backs the public Unit Sites browse page: Local Units only (Divisions
	// are their own browsable rows elsewhere, not part of this list), with an
	// optional division filter and a name/PTA-ID search. There's no "city" on
	// Site — the schema has never carried one — so search only covers the
	// two fields that actually exist. Pagination and the SiteName sort both
	// happen in SQL; only the current page's rows round-trip.
	public async Task<UnitSitePage> GetUnitSitesAsync(
		Guid? divisionId, string? searchText, int pageNumber, int pageSize = 25, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var query = db.Sites.Where(s => s.SiteType == SiteType.LocalUnit && DirectoryListedStatuses.Contains(s.SiteStatus));

		if (divisionId is not null)
			query = query.Where(s => s.ParentSiteId == divisionId);

		if (!string.IsNullOrWhiteSpace(searchText))
		{
			var pattern = $"%{searchText.Trim()}%";
			query = query.Where(s => EF.Functions.Like(s.SiteName, pattern) || EF.Functions.Like(s.PtaId, pattern));
		}

		var totalCount = await query.CountAsync(cancellationToken);

		pageNumber = Math.Max(pageNumber, 1);

		var items = await query
			.OrderBy(s => s.SiteName)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.Select(s => new UnitSiteSummary(
				s.Id,
				s.SiteName,
				s.PtaId,
				s.ParentSiteId,
				s.ParentSite != null ? s.ParentSite.SiteName : null,
				s.SiteStatus))
			.ToListAsync(cancellationToken);

		return new UnitSitePage(items, totalCount, pageNumber, pageSize);
	}

	// Backs the Divisions section of /unit-sites (no search/pagination there,
	// same as before - just every listed Division, alphabetically). Local
	// Units keep using the paginated/searchable GetUnitSitesAsync above; this
	// exists only for the ungrouped Division list next to it.
	public async Task<List<DirectorySite>> GetDirectoryDivisionsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.Sites
			.Where(s => s.SiteType == SiteType.Division && DirectoryListedStatuses.Contains(s.SiteStatus))
			.OrderBy(s => s.SiteName)
			.Select(s => new DirectorySite(
				s.Id, s.SiteName, s.SiteType, s.PtaId,
				s.ParentSiteId, s.ParentSite != null ? s.ParentSite.SiteName : null,
				s.SiteStatus, s.ExternalUrl))
			.ToListAsync(cancellationToken);
	}

	// Backs the "Local Units in this Division" list on /unit-sites/{PtaId}
	// when the site is a Division - every listed child, alphabetically. No
	// pagination, same reasoning as GetDirectoryDivisionsAsync above: this is
	// a bounded child list, not the full unit directory.
	public async Task<List<DirectorySite>> GetChildUnitsAsync(Guid divisionId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.Sites
			.Where(s => s.ParentSiteId == divisionId && DirectoryListedStatuses.Contains(s.SiteStatus))
			.OrderBy(s => s.SiteName)
			.Select(s => new DirectorySite(
				s.Id, s.SiteName, s.SiteType, s.PtaId,
				s.ParentSiteId, s.ParentSite != null ? s.ParentSite.SiteName : null,
				s.SiteStatus, s.ExternalUrl))
			.ToListAsync(cancellationToken);
	}

	// Backs /unit-sites/{PtaId}. Returns null for sites that aren't part of
	// the public directory at all (not found, Archived, Disabled, or any
	// other status outside DirectoryListedStatuses) - the caller 404s on null.
	// Keyed by PtaId (unique, char(8)) rather than the internal Id so the
	// public URL doesn't expose/depend on the database-generated Guid.
	public async Task<SiteDetails?> GetSiteDetailsAsync(string ptaId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.Sites
			.Where(s => s.PtaId == ptaId && DirectoryListedStatuses.Contains(s.SiteStatus))
			.Select(s => new SiteDetails(
				s.Id, s.SiteName, s.SiteType, s.PtaId,
				s.ParentSiteId, s.ParentSite != null ? s.ParentSite.SiteName : null,
				s.SiteStatus, s.ExternalUrl,
				s.Hostname, s.Domain))
			.FirstOrDefaultAsync(cancellationToken);
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
