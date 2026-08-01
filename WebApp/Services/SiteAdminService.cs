using System.Text;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public record SiteAdminResult(bool Success, string? Error, Site? Site = null);
public record SiteSummary(int UserCount, int ChildSiteCount);

// Backs the Global Admin "Sites" tab: hierarchical Division/Local Unit listing,
// creation, and Hostname/Domain edits. Read-only site queries that predate this
// (GetSitesByTypeAsync, GetSiteStatusAsync/SetSiteStatusAsync) stay on
// DashboardService; this service owns everything new for site administration.
public class SiteAdminService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	public async Task<List<Site>> GetDivisionsWithUnitsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var divisions = await db.Sites
			.Where(s => s.SiteType == SiteType.Division)
			.OrderBy(s => s.SiteName)
			.ToListAsync(cancellationToken);

		var units = await db.Sites
			.Where(s => s.SiteType == SiteType.LocalUnit && s.ParentSiteId != null)
			.ToListAsync(cancellationToken);

		// Site.ChildSites is mapped to its own disconnected shadow FK (see
		// AppDbContext's `WithMany("ChildSites")` self-reference), not
		// ParentSiteId, so it never actually holds anything read from the DB.
		// The real parent/child link is ParentSiteId; populate ChildSites from
		// that manually instead of relying on Include/navigation.
		var unitsByParent = units.ToLookup(u => u.ParentSiteId!.Value);
		foreach (var division in divisions)
			division.ChildSites = unitsByParent[division.Id].ToList();

		return divisions;
	}

	public async Task<List<Site>> GetIndependentLocalUnitsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.Sites
			.Where(s => s.SiteType == SiteType.LocalUnit && s.ParentSiteId == null)
			.OrderBy(s => s.SiteName)
			.ToListAsync(cancellationToken);
	}

	public async Task<List<Site>> GetAllDivisionsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
		return await db.Sites
			.Where(s => s.SiteType == SiteType.Division)
			.OrderBy(s => s.SiteName)
			.ToListAsync(cancellationToken);
	}

	public async Task<Site?> GetSiteDetailAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites
			.Include(s => s.ParentSite)
			.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

		if (site is null)
			return null;

		if (site.SiteType == SiteType.Division)
		{
			// See GetDivisionsWithUnitsAsync — ChildSites has to be built from
			// ParentSiteId by hand, it can't be Include()d.
			site.ChildSites = await db.Sites
				.Where(s => s.ParentSiteId == site.Id)
				.OrderBy(s => s.SiteName)
				.ToListAsync(cancellationToken);
		}

		return site;
	}

	public Task<SiteAdminResult> CreateDivisionAsync(
		string siteName, string hostname, string domain, SiteStatus status, CancellationToken cancellationToken = default)
		=> CreateSiteAsync(siteName, hostname, domain, status, SiteType.Division, parentSiteId: null, cancellationToken);

	public Task<SiteAdminResult> CreateLocalUnitAsync(
		string siteName, Guid? parentSiteId, string hostname, string domain, SiteStatus status, CancellationToken cancellationToken = default)
		=> CreateSiteAsync(siteName, hostname, domain, status, SiteType.LocalUnit, parentSiteId, cancellationToken);

	private async Task<SiteAdminResult> CreateSiteAsync(
		string siteName, string hostname, string domain, SiteStatus status,
		SiteType siteType, Guid? parentSiteId, CancellationToken cancellationToken)
	{
		siteName = siteName.Trim();
		hostname = hostname.Trim().ToLowerInvariant();
		domain = domain.Trim().ToLowerInvariant();

		if (string.IsNullOrWhiteSpace(siteName))
			return new SiteAdminResult(false, "Site name is required.");
		if (string.IsNullOrWhiteSpace(hostname))
			return new SiteAdminResult(false, "Hostname is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (parentSiteId is not null)
		{
			var parentIsDivision = await db.Sites.AnyAsync(
				s => s.Id == parentSiteId && s.SiteType == SiteType.Division, cancellationToken);
			if (!parentIsDivision)
				return new SiteAdminResult(false, "Selected parent Division was not found.");
		}

		if (await db.Sites.AnyAsync(s => s.Hostname == hostname, cancellationToken))
			return new SiteAdminResult(false, $"Hostname \"{hostname}\" is already in use.");

		if (!string.IsNullOrEmpty(domain) && await db.Sites.AnyAsync(s => s.Domain == domain, cancellationToken))
			return new SiteAdminResult(false, $"Domain \"{domain}\" is already in use.");

		var site = new Site
		{
			PtaId = await GenerateUniquePtaIdAsync(db, cancellationToken),
			SiteType = siteType,
			ParentSiteId = parentSiteId,
			SiteName = siteName,
			Hostname = hostname,
			Domain = domain,
			SiteStatus = status,
			CreatedAtUtc = DateTimeOffset.UtcNow,
			UpdatedAtUtc = DateTimeOffset.UtcNow,
		};

		db.Sites.Add(site);

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new SiteAdminResult(false, "Could not save the site — its hostname or domain may already be in use.");
		}

		return new SiteAdminResult(true, null, site);
	}

	public async Task<SiteAdminResult> UpdateSiteStatusAsync(
		Guid siteId, SiteStatus status, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
		if (site is null)
			return new SiteAdminResult(false, "Site not found.");

		site.SiteStatus = status;
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);

		return new SiteAdminResult(true, null, site);
	}

	public async Task<SiteSummary> GetSiteSummaryAsync(Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var userCount = await db.SiteUserRoles
			.Where(r => r.SiteId == siteId)
			.Select(r => r.SiteUserId)
			.Distinct()
			.CountAsync(cancellationToken);

		var childSiteCount = await db.Sites.CountAsync(s => s.ParentSiteId == siteId, cancellationToken);

		return new SiteSummary(userCount, childSiteCount);
	}

	public async Task<SiteAdminResult> UpdateDomainSettingsAsync(
		Guid siteId, string hostname, string domain, CancellationToken cancellationToken = default)
	{
		hostname = hostname.Trim().ToLowerInvariant();
		domain = domain.Trim().ToLowerInvariant();

		if (string.IsNullOrWhiteSpace(hostname))
			return new SiteAdminResult(false, "Hostname is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
		if (site is null)
			return new SiteAdminResult(false, "Site not found.");

		if (await db.Sites.AnyAsync(s => s.Id != siteId && s.Hostname == hostname, cancellationToken))
			return new SiteAdminResult(false, $"Hostname \"{hostname}\" is already in use.");

		if (!string.IsNullOrEmpty(domain) && await db.Sites.AnyAsync(s => s.Id != siteId && s.Domain == domain, cancellationToken))
			return new SiteAdminResult(false, $"Domain \"{domain}\" is already in use.");

		site.Hostname = hostname;
		site.Domain = domain;
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new SiteAdminResult(false, "Could not save — the hostname or domain may already be in use.");
		}

		return new SiteAdminResult(true, null, site);
	}

	private static async Task<string> GenerateUniquePtaIdAsync(AppDbContext db, CancellationToken cancellationToken)
	{
		for (var attempt = 0; attempt < 20; attempt++)
		{
			var candidate = Random.Shared.Next(0, 100_000_000).ToString("D8");
			if (!await db.Sites.AnyAsync(s => s.PtaId == candidate, cancellationToken))
				return candidate;
		}

		throw new InvalidOperationException("Unable to generate a unique PtaId.");
	}

	public static string SlugifyHostname(string siteName)
	{
		if (string.IsNullOrWhiteSpace(siteName))
			return string.Empty;

		var sb = new StringBuilder();
		var lastWasHyphen = false;

		foreach (var c in siteName.Trim().ToLowerInvariant())
		{
			if (char.IsLetterOrDigit(c))
			{
				sb.Append(c);
				lastWasHyphen = false;
			}
			else if (!lastWasHyphen && sb.Length > 0)
			{
				sb.Append('-');
				lastWasHyphen = true;
			}
		}

		return sb.ToString().TrimEnd('-');
	}
}
