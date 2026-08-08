using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SiteEngine;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;
using SiteEngine.Identity;

namespace WebApp.Services;

public record SiteAdminResult(bool Success, string? Error, Site? Site = null);
public record SiteSummary(int UserCount, int ChildSiteCount);

// Contact/login data for a single site, as shown alongside its computed
// URLs in the Global Admin Sites list and Site Detail's Local Units section.
// ContactAdminName/Email come from whichever current-school-year SiteUserRole
// is flagged IsPrimaryContact — not a separate field on Site, so there's only
// one place a "who do we contact for this site" answer can come from.
public record SiteActivity(string? ContactAdminName, string? ContactAdminEmail, DateTimeOffset? LastLogin, DateTimeOffset? LastAdminLogin);

// Admin-editable logo fields shared by Division and Local Unit sites (every
// value but SchoolCrest applies to both site types — see UpdateSiteLogoAsync).
// Deliberately excludes LogoUrl, which is never hand-uploaded — see
// GeneratePtaLogoAsync/EnsureGeneratedLogoAsync.
public enum SiteLogoField
{
	PTALogoVariant,
	SchoolCrest,
	DistrictLogo,
	PartnerLogo,
}

// Backs the Global Admin "Sites" tab: hierarchical Division/Local Unit listing,
// creation, and Hostname/Domain edits. Read-only site queries that predate this
// (GetSitesByTypeAsync, GetSiteStatusAsync/SetSiteStatusAsync) stay on
// DashboardService; this service owns everything new for site administration.
public class SiteAdminService(
	IDbContextFactory<AppDbContext> dbFactory, PtaLogoGenerationService logoGenerator, UserManager<ApplicationUser> userManager)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
	private readonly PtaLogoGenerationService _logoGenerator = logoGenerator;
	private readonly UserManager<ApplicationUser> _userManager = userManager;

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
		string siteName, string ptaId, string hostname, string domain, SiteStatus status,
		string? firstContactEmail = null, CancellationToken cancellationToken = default)
		=> CreateSiteAsync(siteName, ptaId, hostname, domain, status, SiteType.Division, parentSiteId: null, firstContactEmail, cancellationToken);

	public Task<SiteAdminResult> CreateLocalUnitAsync(
		string siteName, Guid? parentSiteId, string ptaId, string hostname, string domain, SiteStatus status,
		string? firstContactEmail = null, CancellationToken cancellationToken = default)
		=> CreateSiteAsync(siteName, ptaId, hostname, domain, status, SiteType.LocalUnit, parentSiteId, firstContactEmail, cancellationToken);

	private async Task<SiteAdminResult> CreateSiteAsync(
		string siteName, string ptaId, string hostname, string domain, SiteStatus status,
		SiteType siteType, Guid? parentSiteId, string? firstContactEmail, CancellationToken cancellationToken)
	{
		siteName = siteName.Trim();
		ptaId = ptaId.Trim();
		hostname = hostname.Trim().ToLowerInvariant();
		domain = domain.Trim().ToLowerInvariant();
		var normalizedContactEmail = string.IsNullOrWhiteSpace(firstContactEmail)
			? null
			: firstContactEmail.Trim().ToLowerInvariant();

		if (string.IsNullOrWhiteSpace(siteName))
			return new SiteAdminResult(false, "Site name is required.");
		if (string.IsNullOrWhiteSpace(ptaId))
			return new SiteAdminResult(false, "PTA ID # is required.");
		if (ptaId.Length > 8)
			return new SiteAdminResult(false, "PTA ID # must be 8 characters or fewer.");
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

		if (await db.Sites.AnyAsync(s => s.PtaId == ptaId, cancellationToken))
			return new SiteAdminResult(false, $"PTA ID \"{ptaId}\" is already in use.");

		if (await db.Sites.AnyAsync(s => s.Hostname == hostname, cancellationToken))
			return new SiteAdminResult(false, $"Hostname \"{hostname}\" is already in use.");

		if (!string.IsNullOrEmpty(domain) && await db.Sites.AnyAsync(s => s.Domain == domain, cancellationToken))
			return new SiteAdminResult(false, $"Domain \"{domain}\" is already in use.");

		var site = new Site
		{
			PtaId = ptaId,
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
			return new SiteAdminResult(false, "Could not save the site — its PTA ID, hostname, or domain may already be in use.");
		}

		if (normalizedContactEmail is not null)
		{
			var contactResult = await AssignFirstContactAsync(db, site, normalizedContactEmail, cancellationToken);
			if (!contactResult.Success)
				return new SiteAdminResult(true, $"Site created, but the first contact could not be set up: {contactResult.Error}", site);
		}

		return new SiteAdminResult(true, null, site);
	}

	// Ties the "First Contact Email" collected on the Add Division/Local Unit
	// forms to a SiteAdmin SiteUserRole for the new site, reusing the same
	// find-or-create-ApplicationUser flow as passwordless sign-in (see
	// PasswordlessSignInService.RequestCodeAsync) so a contact who has never
	// logged in yet still ends up with a normal, sign-in-able account.
	private async Task<SiteAdminResult> AssignFirstContactAsync(
		AppDbContext db, Site site, string normalizedEmail, CancellationToken cancellationToken)
	{
		var identityUser = await _userManager.FindByEmailAsync(normalizedEmail);
		if (identityUser is null)
		{
			identityUser = new ApplicationUser
			{
				UserName = normalizedEmail,
				Email = normalizedEmail,
				EmailConfirmed = false,
				IsFirstLogin = true,
			};

			var createResult = await _userManager.CreateAsync(identityUser);
			if (!createResult.Succeeded)
				return new SiteAdminResult(false, string.Join(" ", createResult.Errors.Select(e => e.Description)));
		}

		var siteUser = await db.SiteUsers.FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id, cancellationToken);
		if (siteUser is null)
		{
			siteUser = new SiteUser
			{
				IdentityUserId = identityUser.Id,
				PreferredEmail = normalizedEmail,
			};
			db.SiteUsers.Add(siteUser);
			await db.SaveChangesAsync(cancellationToken);
		}

		db.SiteUserRoles.Add(new SiteUserRole
		{
			SiteId = site.Id,
			SiteUserId = siteUser.Id,
			Role = SiteRole.SiteAdmin,
			SchoolYear = SchoolYear.Current(),
			StartUtc = DateTimeOffset.UtcNow,
			IsPrimaryContact = true,
		});
		await db.SaveChangesAsync(cancellationToken);

		return new SiteAdminResult(true, null);
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

	// Batched so a page rendering a whole list of sites (Sites tab, a
	// Division's Local Units section) issues one round of queries instead of
	// N+1 per row. LastAdminLogin is the most recent LoginHistory row whose
	// user held SiteAdmin+ on that site at all (not scoped to the login's
	// school year — role history isn't tracked at that granularity).
	public async Task<Dictionary<Guid, SiteActivity>> GetSiteActivityAsync(
		IReadOnlyCollection<Guid> siteIds, CancellationToken cancellationToken = default)
	{
		if (siteIds.Count == 0)
			return new Dictionary<Guid, SiteActivity>();

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var currentSchoolYear = SchoolYear.Current();

		var contactsBySite = (await db.SiteUserRoles
			.Where(r => siteIds.Contains(r.SiteId) && r.IsPrimaryContact && r.SchoolYear == currentSchoolYear)
			.Select(r => new { r.SiteId, r.SiteUser.DisplayName, r.SiteUser.PreferredEmail })
			.ToListAsync(cancellationToken))
			.GroupBy(x => x.SiteId)
			.ToDictionary(g => g.Key, g => g.First());

		var lastLoginBySite = await db.LoginHistories
			.Where(h => h.SiteId != null && siteIds.Contains(h.SiteId.Value))
			.GroupBy(h => h.SiteId!.Value)
			.Select(g => new { SiteId = g.Key, LastLogin = g.Max(h => h.LoginUtc) })
			.ToDictionaryAsync(x => x.SiteId, x => x.LastLogin, cancellationToken);

		var adminUserIdsBySite = (await db.SiteUserRoles
			.Where(r => siteIds.Contains(r.SiteId) && r.Role != null && r.Role >= SiteRole.SiteAdmin)
			.Select(r => new { r.SiteId, r.SiteUser.IdentityUserId })
			.ToListAsync(cancellationToken))
			.GroupBy(x => x.SiteId)
			.ToDictionary(g => g.Key, g => g.Select(x => x.IdentityUserId).ToHashSet());

		var loginRows = await db.LoginHistories
			.Where(h => h.SiteId != null && siteIds.Contains(h.SiteId.Value))
			.Select(h => new { SiteId = h.SiteId!.Value, h.UserId, h.LoginUtc })
			.ToListAsync(cancellationToken);

		var lastAdminLoginBySite = new Dictionary<Guid, DateTimeOffset>();
		foreach (var group in loginRows.GroupBy(x => x.SiteId))
		{
			if (!adminUserIdsBySite.TryGetValue(group.Key, out var adminIds))
				continue;

			var adminLoginTimes = group.Where(x => adminIds.Contains(x.UserId)).Select(x => x.LoginUtc).ToList();
			if (adminLoginTimes.Count > 0)
				lastAdminLoginBySite[group.Key] = adminLoginTimes.Max();
		}

		return siteIds.ToDictionary(id => id, id =>
		{
			contactsBySite.TryGetValue(id, out var contact);
			lastLoginBySite.TryGetValue(id, out var lastLogin);
			lastAdminLoginBySite.TryGetValue(id, out var lastAdminLogin);

			return new SiteActivity(
				contact?.DisplayName,
				contact?.PreferredEmail,
				lastLoginBySite.ContainsKey(id) ? lastLogin : null,
				lastAdminLoginBySite.ContainsKey(id) ? lastAdminLogin : null);
		});
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

	// Backs every admin-uploaded logo field except LogoUrl (which is always
	// generated, never uploaded — see GeneratePtaLogoAsync). SchoolCrest is
	// the only field actually restricted to a SiteType; the others apply to
	// both Divisions and Local Units.
	public async Task<SiteAdminResult> UpdateSiteLogoAsync(
		Guid siteId, SiteLogoField field, string? url, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
		if (site is null)
			return new SiteAdminResult(false, "Site not found.");

		if (field == SiteLogoField.SchoolCrest && site.SiteType != SiteType.LocalUnit)
			return new SiteAdminResult(false, "Only Local Units have a school crest.");

		switch (field)
		{
			case SiteLogoField.PTALogoVariant:
				site.PTALogoVariantUrl = url;
				break;
			case SiteLogoField.SchoolCrest:
				site.SchoolCrestUrl = url;
				break;
			case SiteLogoField.DistrictLogo:
				site.DistrictLogoUrl = url;
				break;
			case SiteLogoField.PartnerLogo:
				site.PartnerLogoUrl = url;
				break;
		}

		site.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);

		return new SiteAdminResult(true, null, site);
	}

	// Explicit admin-requested regeneration ("Generate PTA Logo" button) —
	// always renders a fresh PNG and replaces LogoUrl, deleting the old
	// generated file. Never called automatically; see EnsureGeneratedLogoAsync
	// for the one-time lazy fallback a masthead render triggers instead.
	public async Task<SiteAdminResult> GeneratePtaLogoAsync(Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
		if (site is null)
			return new SiteAdminResult(false, "Site not found.");

		var previousUrl = site.LogoUrl;
		site.LogoUrl = await _logoGenerator.GenerateAsync(site.Id, site.SiteName, cancellationToken);
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);

		_logoGenerator.DeleteIfGenerated(previousUrl);

		return new SiteAdminResult(true, null, site);
	}

	// Lazy fallback for a site that has never had a LogoUrl generated (e.g.
	// created before this feature, or before an admin ever clicked
	// "Generate PTA Logo"). Generates and persists once, then never again —
	// a site with an existing LogoUrl is returned unchanged. Called from
	// DivisionLayout/UnitLayout on render, not from any admin action.
	public async Task<string?> EnsureGeneratedLogoAsync(Guid siteId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
		if (site is null)
			return null;

		if (!string.IsNullOrWhiteSpace(site.LogoUrl))
			return site.LogoUrl;

		site.LogoUrl = await _logoGenerator.GenerateAsync(site.Id, site.SiteName, cancellationToken);
		site.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await db.SaveChangesAsync(cancellationToken);

		return site.LogoUrl;
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
