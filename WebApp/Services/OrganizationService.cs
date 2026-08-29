using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public record OrgOpResult(bool Success, string? Error);

public record OrganizationTypeSummary(
	int Id, string Name, string Description, string? IconClass, int SortOrder, int LevelCount, int OrganizationCount,
	IdentifierRequirement IdentifierRequirement, string? IdentifierLabel);
public record OrganizationLevelSummary(Guid Id, int OrganizationTypeId, string Name, int Rank, bool IsSiteEligible, int OrganizationCount);
public record OperationalCycleSummary(Guid Id, int OrganizationTypeId, string CycleTypeName, string DisplayLabel, DateTimeOffset StartDate, DateTimeOffset EndDate);
public record OrganizationSummary(
	Guid Id, string Name, string? Description, string? IdentifierValue,
	int OrganizationTypeId, string OrganizationTypeName,
	Guid OrganizationLevelId, string OrganizationLevelName, int LevelRank,
	Guid? ParentOrganizationId, string? ParentOrganizationName,
	Guid? SiteId, string? SiteName, string? SiteHostname, string? SiteDomain,
	string? ExternalUrl, OrganizationVisibility Visibility,
	int ChildCount)
{
	// Computed, not stored — see OrganizationPublicExperience-Phase1.md's
	// "Resolved Decisions". Never let this drift out of sync with
	// SiteId/ExternalUrl by making it a real column; derive it instead.
	public PresenceType PresenceType =>
		SiteId is not null ? PresenceType.Hosted
		: ExternalUrl is not null ? PresenceType.External
		: PresenceType.DirectoryOnly;
}
public record ParentAccessGrantSummary(
	Guid Id,
	Guid ParentOrganizationId, string ParentOrganizationName,
	Guid ChildOrganizationId, string ChildOrganizationName,
	AccessLevel AccessLevel);

// Backs Global Admin's "Organizations" section — Organization Types, Organization
// Levels, Operational Cycles, Organizations, and Parent Access Grants. See
// md/OrganizationFramework.md and md/OrganizationFramework-Phase1.md.
//
// Deliberately does not touch SiteUser/SiteUserRole/CustomRole/BoardPosition
// or SiteRoleResolver — Phase 1's "Membership Migration Is Analysis Only".
// DashboardService.GetOrganizationTypesAsync (the public-facing read used by
// PortalHome/Organizations.razor) is untouched too; this service owns
// everything new for framework administration.
public class OrganizationService(IDbContextFactory<AppDbContext> dbFactory)
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

	// ------------------------------------------------------------
	// Organization Type
	// ------------------------------------------------------------

	public async Task<List<OrganizationTypeSummary>> GetOrganizationTypeSummariesAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.OrganizationTypes
			.OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
			.Select(t => new OrganizationTypeSummary(
				t.Id, t.Name, t.Description, t.IconClass, t.SortOrder,
				db.OrganizationLevels.Count(l => l.OrganizationTypeId == t.Id),
				db.Organizations.Count(o => o.OrganizationTypeId == t.Id),
				t.IdentifierRequirement, t.IdentifierLabel))
			.ToListAsync(cancellationToken);
	}

	public async Task<OrganizationTypeSummary?> GetOrganizationTypeAsync(int id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.OrganizationTypes
			.Where(t => t.Id == id)
			.Select(t => new OrganizationTypeSummary(
				t.Id, t.Name, t.Description, t.IconClass, t.SortOrder,
				db.OrganizationLevels.Count(l => l.OrganizationTypeId == t.Id),
				db.Organizations.Count(o => o.OrganizationTypeId == t.Id),
				t.IdentifierRequirement, t.IdentifierLabel))
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<OrgOpResult> CreateOrganizationTypeAsync(
		string name, string description, string? iconClass, int sortOrder,
		IdentifierRequirement identifierRequirement = IdentifierRequirement.NotUsed, string? identifierLabel = null,
		CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (await db.OrganizationTypes.AnyAsync(t => t.Name == name, cancellationToken))
			return new OrgOpResult(false, $"An Organization Type named \"{name}\" already exists.");

		db.OrganizationTypes.Add(new OrganizationType
		{
			Name = name,
			Description = description.Trim(),
			IconClass = string.IsNullOrWhiteSpace(iconClass) ? null : iconClass.Trim(),
			SortOrder = sortOrder,
			IdentifierRequirement = identifierRequirement,
			IdentifierLabel = string.IsNullOrWhiteSpace(identifierLabel) ? null : identifierLabel.Trim(),
		});

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> UpdateOrganizationTypeAsync(
		int id, string name, string description, string? iconClass, int sortOrder,
		IdentifierRequirement identifierRequirement, string? identifierLabel,
		CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var type = await db.OrganizationTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
		if (type is null)
			return new OrgOpResult(false, "Organization Type not found.");

		if (await db.OrganizationTypes.AnyAsync(t => t.Id != id && t.Name == name, cancellationToken))
			return new OrgOpResult(false, $"An Organization Type named \"{name}\" already exists.");

		type.Name = name;
		type.Description = description.Trim();
		type.IconClass = string.IsNullOrWhiteSpace(iconClass) ? null : iconClass.Trim();
		type.SortOrder = sortOrder;
		type.IdentifierRequirement = identifierRequirement;
		type.IdentifierLabel = string.IsNullOrWhiteSpace(identifierLabel) ? null : identifierLabel.Trim();

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> DeleteOrganizationTypeAsync(int id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var type = await db.OrganizationTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
		if (type is null)
			return new OrgOpResult(false, "Organization Type not found.");

		db.OrganizationTypes.Remove(type);

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new OrgOpResult(false, "Can't delete this Organization Type while it still has Levels, Operational Cycles, or Organizations. Remove those first.");
		}

		return new OrgOpResult(true, null);
	}

	// ------------------------------------------------------------
	// Organization Level
	// ------------------------------------------------------------

	public async Task<List<OrganizationLevelSummary>> GetLevelsAsync(int organizationTypeId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.OrganizationLevels
			.Where(l => l.OrganizationTypeId == organizationTypeId)
			.OrderBy(l => l.Rank)
			.Select(l => new OrganizationLevelSummary(
				l.Id, l.OrganizationTypeId, l.Name, l.Rank, l.IsSiteEligible,
				db.Organizations.Count(o => o.OrganizationLevelId == l.Id)))
			.ToListAsync(cancellationToken);
	}

	public async Task<OrgOpResult> CreateLevelAsync(
		int organizationTypeId, string name, int rank, bool isSiteEligible, CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (!await db.OrganizationTypes.AnyAsync(t => t.Id == organizationTypeId, cancellationToken))
			return new OrgOpResult(false, "Organization Type not found.");

		if (await db.OrganizationLevels.AnyAsync(l => l.OrganizationTypeId == organizationTypeId && l.Name == name, cancellationToken))
			return new OrgOpResult(false, $"A Level named \"{name}\" already exists for this Organization Type.");

		db.OrganizationLevels.Add(new OrganizationLevel
		{
			OrganizationTypeId = organizationTypeId,
			Name = name,
			Rank = rank,
			IsSiteEligible = isSiteEligible,
		});

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> UpdateLevelAsync(
		Guid id, string name, int rank, bool isSiteEligible, CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var level = await db.OrganizationLevels.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
		if (level is null)
			return new OrgOpResult(false, "Level not found.");

		if (await db.OrganizationLevels.AnyAsync(l => l.Id != id && l.OrganizationTypeId == level.OrganizationTypeId && l.Name == name, cancellationToken))
			return new OrgOpResult(false, $"A Level named \"{name}\" already exists for this Organization Type.");

		level.Name = name;
		level.Rank = rank;
		level.IsSiteEligible = isSiteEligible;
		level.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> DeleteLevelAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var level = await db.OrganizationLevels.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
		if (level is null)
			return new OrgOpResult(false, "Level not found.");

		db.OrganizationLevels.Remove(level);

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new OrgOpResult(false, "Can't delete this Level while Organizations are placed at it. Move or remove those Organizations first.");
		}

		return new OrgOpResult(true, null);
	}

	// Renumbers every Level for this Organization Type to a clean 1..N
	// sequence, in current Rank order — e.g. deleting ranks 4 and 5 leaves
	// 1, 2, 3, 6; this turns that into 1, 2, 3, 4 without changing relative
	// order. Ties (two Levels sharing a Rank) break on Name for a
	// deterministic result. Organization.OrganizationLevelId references the
	// Level's Id, not its Rank, so no Organization's placement changes —
	// this only cleans up the display numbering.
	public async Task<OrgOpResult> RenumberLevelRanksAsync(int organizationTypeId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var levels = await db.OrganizationLevels
			.Where(l => l.OrganizationTypeId == organizationTypeId)
			.OrderBy(l => l.Rank).ThenBy(l => l.Name)
			.ToListAsync(cancellationToken);

		for (var i = 0; i < levels.Count; i++)
		{
			var expectedRank = i + 1;
			if (levels[i].Rank != expectedRank)
			{
				levels[i].Rank = expectedRank;
				levels[i].UpdatedAtUtc = DateTimeOffset.UtcNow;
			}
		}

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	// ------------------------------------------------------------
	// Operational Cycle
	// ------------------------------------------------------------

	public async Task<List<OperationalCycleSummary>> GetCyclesAsync(int organizationTypeId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.OperationalCycles
			.Where(c => c.OrganizationTypeId == organizationTypeId)
			.OrderByDescending(c => c.StartDate)
			.Select(c => new OperationalCycleSummary(c.Id, c.OrganizationTypeId, c.CycleTypeName, c.DisplayLabel, c.StartDate, c.EndDate))
			.ToListAsync(cancellationToken);
	}

	public async Task<OrgOpResult> CreateCycleAsync(
		int organizationTypeId, string cycleTypeName, string displayLabel, DateTimeOffset startDate, DateTimeOffset endDate,
		CancellationToken cancellationToken = default)
	{
		cycleTypeName = cycleTypeName.Trim();
		displayLabel = displayLabel.Trim();

		if (string.IsNullOrWhiteSpace(cycleTypeName))
			return new OrgOpResult(false, "Cycle type is required.");
		if (string.IsNullOrWhiteSpace(displayLabel))
			return new OrgOpResult(false, "Display label is required.");
		if (endDate <= startDate)
			return new OrgOpResult(false, "End date must be after start date.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (!await db.OrganizationTypes.AnyAsync(t => t.Id == organizationTypeId, cancellationToken))
			return new OrgOpResult(false, "Organization Type not found.");

		db.OperationalCycles.Add(new OperationalCycle
		{
			OrganizationTypeId = organizationTypeId,
			CycleTypeName = cycleTypeName,
			DisplayLabel = displayLabel,
			StartDate = startDate,
			EndDate = endDate,
		});

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> UpdateCycleAsync(
		Guid id, string cycleTypeName, string displayLabel, DateTimeOffset startDate, DateTimeOffset endDate,
		CancellationToken cancellationToken = default)
	{
		cycleTypeName = cycleTypeName.Trim();
		displayLabel = displayLabel.Trim();

		if (string.IsNullOrWhiteSpace(cycleTypeName))
			return new OrgOpResult(false, "Cycle type is required.");
		if (string.IsNullOrWhiteSpace(displayLabel))
			return new OrgOpResult(false, "Display label is required.");
		if (endDate <= startDate)
			return new OrgOpResult(false, "End date must be after start date.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var cycle = await db.OperationalCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
		if (cycle is null)
			return new OrgOpResult(false, "Operational Cycle not found.");

		cycle.CycleTypeName = cycleTypeName;
		cycle.DisplayLabel = displayLabel;
		cycle.StartDate = startDate;
		cycle.EndDate = endDate;

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> DeleteCycleAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var cycle = await db.OperationalCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
		if (cycle is null)
			return new OrgOpResult(false, "Operational Cycle not found.");

		db.OperationalCycles.Remove(cycle);
		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	// ------------------------------------------------------------
	// Organization
	// ------------------------------------------------------------

	// Flat, not a tree — sorted by Level Rank then Name so a hierarchy reads
	// top-to-bottom in the admin list even without a nested widget. Sufficient
	// for Phase 1's "configure PTA/Scouts/Billiards through admin screens"
	// validation; a real tree/hierarchy-builder view is a later-phase UX
	// improvement, not something this list needs to get right the first time.
	public async Task<List<OrganizationSummary>> GetOrganizationsAsync(int? organizationTypeId = null, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var query = db.Organizations.AsQueryable();
		if (organizationTypeId is not null)
			query = query.Where(o => o.OrganizationTypeId == organizationTypeId);

		return await query
			.OrderBy(o => o.OrganizationLevel.Rank).ThenBy(o => o.Name)
			.Select(o => new OrganizationSummary(
				o.Id, o.Name, o.Description, o.IdentifierValue,
				o.OrganizationTypeId, o.OrganizationType.Name,
				o.OrganizationLevelId, o.OrganizationLevel.Name, o.OrganizationLevel.Rank,
				o.ParentOrganizationId, o.ParentOrganization != null ? o.ParentOrganization.Name : null,
				o.SiteId, o.Site != null ? o.Site.SiteName : null,
				o.Site != null ? o.Site.Hostname : null, o.Site != null ? o.Site.Domain : null,
				o.ExternalUrl, o.Visibility,
				db.Organizations.Count(c => c.ParentOrganizationId == o.Id)))
			.ToListAsync(cancellationToken);
	}

	public async Task<OrganizationSummary?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.Organizations
			.Where(o => o.Id == id)
			.Select(o => new OrganizationSummary(
				o.Id, o.Name, o.Description, o.IdentifierValue,
				o.OrganizationTypeId, o.OrganizationType.Name,
				o.OrganizationLevelId, o.OrganizationLevel.Name, o.OrganizationLevel.Rank,
				o.ParentOrganizationId, o.ParentOrganization != null ? o.ParentOrganization.Name : null,
				o.SiteId, o.Site != null ? o.Site.SiteName : null,
				o.Site != null ? o.Site.Hostname : null, o.Site != null ? o.Site.Domain : null,
				o.ExternalUrl, o.Visibility,
				db.Organizations.Count(c => c.ParentOrganizationId == o.Id)))
			.FirstOrDefaultAsync(cancellationToken);
	}

	// Publicly listed Organizations only (Visibility == Public) — backs the
	// Community Directory (/unit-sites — see OrganizationPublicExperience-Phase1.md's
	// "Resolved Decisions": same URL, Organization-backed query). Unlike
	// GetOrganizationsAsync, this deliberately doesn't expose Pending/Private/
	// Archived Organizations — an admin previewing one of those does so
	// through Global Admin, not the public route.
	public async Task<List<OrganizationSummary>> GetDirectoryOrganizationsAsync(
		string? searchText = null, int? organizationTypeId = null, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var query = db.Organizations.Where(o => o.Visibility == OrganizationVisibility.Public);

		if (organizationTypeId is not null)
			query = query.Where(o => o.OrganizationTypeId == organizationTypeId);

		if (!string.IsNullOrWhiteSpace(searchText))
		{
			var pattern = $"%{searchText.Trim()}%";
			query = query.Where(o => EF.Functions.Like(o.Name, pattern));
		}

		return await query
			.OrderBy(o => o.Name)
			.Select(o => new OrganizationSummary(
				o.Id, o.Name, o.Description, o.IdentifierValue,
				o.OrganizationTypeId, o.OrganizationType.Name,
				o.OrganizationLevelId, o.OrganizationLevel.Name, o.OrganizationLevel.Rank,
				o.ParentOrganizationId, o.ParentOrganization != null ? o.ParentOrganization.Name : null,
				o.SiteId, o.Site != null ? o.Site.SiteName : null,
				o.Site != null ? o.Site.Hostname : null, o.Site != null ? o.Site.Domain : null,
				o.ExternalUrl, o.Visibility,
				db.Organizations.Count(c => c.ParentOrganizationId == o.Id)))
			.ToListAsync(cancellationToken);
	}

	// Backs the community detail page (/communities/{identifier} — see
	// OrganizationPublicExperience-Phase1.md). Tries IdentifierValue first
	// (the common case for Organization Types that use one), then falls back
	// to parsing the segment as a Guid Id — covers every Organization,
	// including ones whose Type never uses an identifier at all. Only
	// publicly visible Organizations resolve; anything else 404s the same as
	// "not found", same idiom as DashboardService.GetSiteDetailsAsync.
	public async Task<OrganizationSummary?> GetOrganizationByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var query = db.Organizations.Where(o => o.Visibility == OrganizationVisibility.Public);

		var byIdentifier = await query
			.Where(o => o.IdentifierValue == identifier)
			.Select(o => o.Id)
			.FirstOrDefaultAsync(cancellationToken);

		var resolvedId = byIdentifier != Guid.Empty
			? byIdentifier
			: Guid.TryParse(identifier, out var parsedId) ? parsedId : (Guid?)null;

		if (resolvedId is null)
			return null;

		var org = await GetOrganizationAsync(resolvedId.Value, cancellationToken);
		return org?.Visibility == OrganizationVisibility.Public ? org : null;
	}

	// Sites not already linked to an Organization — backs the Site picker on
	// the Organization create/edit form. A Site can belong to at most one
	// Organization (see Organization.SiteId's unique index).
	public async Task<List<Site>> GetUnlinkedSitesAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var linkedSiteIds = db.Organizations.Where(o => o.SiteId != null).Select(o => o.SiteId!.Value);

		return await db.Sites
			.Where(s => !linkedSiteIds.Contains(s.Id))
			.OrderBy(s => s.SiteName)
			.ToListAsync(cancellationToken);
	}

	public async Task<OrgOpResult> CreateOrganizationAsync(
		string name, int organizationTypeId, Guid organizationLevelId, Guid? parentOrganizationId, Guid? siteId,
		string? identifierValue = null, string? description = null, string? externalUrl = null,
		OrganizationVisibility visibility = OrganizationVisibility.Public, CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var validation = await ValidatePlacementAsync(db, organizationTypeId, organizationLevelId, parentOrganizationId, siteId, cancellationToken);
		if (validation is not null)
			return validation;

		var identifierResult = await ValidateIdentifierAsync(db, organizationTypeId, identifierValue, excludeOrganizationId: null, cancellationToken);
		if (identifierResult.Error is not null)
			return new OrgOpResult(false, identifierResult.Error);

		db.Organizations.Add(new Organization
		{
			Name = name,
			OrganizationTypeId = organizationTypeId,
			OrganizationLevelId = organizationLevelId,
			ParentOrganizationId = parentOrganizationId,
			SiteId = siteId,
			IdentifierValue = identifierResult.Value,
			Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
			ExternalUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl.Trim(),
			Visibility = visibility,
		});

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new OrgOpResult(false, "Could not save — the selected Site, or the identifier, may already be in use by another Organization.");
		}

		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> UpdateOrganizationAsync(
		Guid id, string name, Guid organizationLevelId, Guid? parentOrganizationId, Guid? siteId,
		string? identifierValue = null, string? description = null, string? externalUrl = null,
		OrganizationVisibility visibility = OrganizationVisibility.Public, CancellationToken cancellationToken = default)
	{
		name = name.Trim();
		if (string.IsNullOrWhiteSpace(name))
			return new OrgOpResult(false, "Name is required.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
		if (org is null)
			return new OrgOpResult(false, "Organization not found.");

		if (parentOrganizationId == id)
			return new OrgOpResult(false, "An Organization cannot be its own parent.");

		var validation = await ValidatePlacementAsync(db, org.OrganizationTypeId, organizationLevelId, parentOrganizationId, siteId, cancellationToken, excludeOrganizationId: id);
		if (validation is not null)
			return validation;

		var identifierResult = await ValidateIdentifierAsync(db, org.OrganizationTypeId, identifierValue, excludeOrganizationId: id, cancellationToken);
		if (identifierResult.Error is not null)
			return new OrgOpResult(false, identifierResult.Error);

		org.Name = name;
		org.OrganizationLevelId = organizationLevelId;
		org.ParentOrganizationId = parentOrganizationId;
		org.SiteId = siteId;
		org.IdentifierValue = identifierResult.Value;
		org.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
		org.ExternalUrl = string.IsNullOrWhiteSpace(externalUrl) ? null : externalUrl.Trim();
		org.Visibility = visibility;
		org.UpdatedAtUtc = DateTimeOffset.UtcNow;

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			return new OrgOpResult(false, "Could not save — the selected Site, or the identifier, may already be in use by another Organization.");
		}

		return new OrgOpResult(true, null);
	}

	// Enforces the owning Organization Type's IdentifierRequirement policy:
	// Required means non-blank, NotUsed silently clears whatever was passed
	// in (rather than erroring — a Type that later turns identifiers off
	// shouldn't strand existing Organizations with an error on their next
	// save), Optional accepts either. Returns the normalized value to store
	// (trimmed, or null) alongside an error, never both.
	private static async Task<(string? Value, string? Error)> ValidateIdentifierAsync(
		AppDbContext db, int organizationTypeId, string? identifierValue, Guid? excludeOrganizationId, CancellationToken cancellationToken)
	{
		var type = await db.OrganizationTypes.FirstOrDefaultAsync(t => t.Id == organizationTypeId, cancellationToken);
		if (type is null)
			return (null, "Organization Type not found.");

		var trimmed = string.IsNullOrWhiteSpace(identifierValue) ? null : identifierValue.Trim();

		if (type.IdentifierRequirement == IdentifierRequirement.NotUsed)
			return (null, null);

		if (type.IdentifierRequirement == IdentifierRequirement.Required && trimmed is null)
			return (null, $"{type.IdentifierLabel ?? "Identifier"} is required for {type.Name} Organizations.");

		if (trimmed is not null)
		{
			var inUse = await db.Organizations.AnyAsync(
				o => o.OrganizationTypeId == organizationTypeId && o.IdentifierValue == trimmed && o.Id != excludeOrganizationId,
				cancellationToken);
			if (inUse)
				return (null, $"\"{trimmed}\" is already in use as a {(type.IdentifierLabel ?? "identifier")} for another {type.Name} Organization.");
		}

		return (trimmed, null);
	}

	// Any Parent Access Grant naming this Organization on either side is
	// deleted along with it — a grant referencing a deleted Organization is
	// meaningless, and grants are cheap to recreate (unlike child
	// Organizations, which are NOT cascade-deleted here — see below). This
	// is the one exception to ParentAccessGrant's own "revoking access IS
	// deleting the row" rule being a purely explicit, admin-driven action:
	// here it's an unavoidable side effect of the Organization itself going
	// away, not a policy decision.
	public async Task<OrgOpResult> DeleteOrganizationAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
		if (org is null)
			return new OrgOpResult(false, "Organization not found.");

		var relatedGrants = await db.ParentAccessGrants
			.Where(g => g.ParentOrganizationId == id || g.ChildOrganizationId == id)
			.ToListAsync(cancellationToken);
		db.ParentAccessGrants.RemoveRange(relatedGrants);

		db.Organizations.Remove(org);

		try
		{
			await db.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException)
		{
			// Only child Organizations can still block this — grants above are
			// already gone. Deliberately not cascaded: silently deleting a
			// whole subtree is a much bigger, harder-to-undo action than
			// dropping a couple of access grants, so it stays a real decision
			// the admin makes explicitly (reparent or delete each child first —
			// see OrganizationDetail, which now lists them right on the page).
			return new OrgOpResult(false, "Can't delete this Organization while it still has child Organizations. Reparent or delete those first — they're listed below.");
		}

		return new OrgOpResult(true, null);
	}

	// Every grant naming this Organization on either side — backs
	// OrganizationDetail's inline "Parent Access Grants" section, so an admin
	// can see (and revoke, or just understand what deleting this Organization
	// will also remove) without leaving the page.
	public async Task<List<ParentAccessGrantSummary>> GetGrantsInvolvingAsync(Guid organizationId, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.ParentAccessGrants
			.Where(g => g.ParentOrganizationId == organizationId || g.ChildOrganizationId == organizationId)
			.OrderBy(g => g.ParentOrganization.Name).ThenBy(g => g.ChildOrganization.Name)
			.Select(g => new ParentAccessGrantSummary(
				g.Id,
				g.ParentOrganizationId, g.ParentOrganization.Name,
				g.ChildOrganizationId, g.ChildOrganization.Name,
				g.AccessLevel))
			.ToListAsync(cancellationToken);
	}

	// Shared by Create/Update: the Level must belong to the Organization's own
	// Type; a parent, if any, must be of the SAME Type and sit at a shallower
	// Level (lower Rank) — a child can't be its own ancestor's ancestor. A
	// linked Site, if any, must exist, not already be linked elsewhere, and
	// (soft check, not a DB constraint) the chosen Level should normally be
	// site-eligible — see OrganizationLevel.IsSiteEligible.
	private static async Task<OrgOpResult?> ValidatePlacementAsync(
		AppDbContext db, int organizationTypeId, Guid organizationLevelId, Guid? parentOrganizationId, Guid? siteId,
		CancellationToken cancellationToken, Guid? excludeOrganizationId = null)
	{
		var level = await db.OrganizationLevels.FirstOrDefaultAsync(l => l.Id == organizationLevelId, cancellationToken);
		if (level is null)
			return new OrgOpResult(false, "Level not found.");
		if (level.OrganizationTypeId != organizationTypeId)
			return new OrgOpResult(false, "The selected Level does not belong to this Organization Type.");

		if (parentOrganizationId is not null)
		{
			var parent = await db.Organizations
				.Include(o => o.OrganizationLevel)
				.FirstOrDefaultAsync(o => o.Id == parentOrganizationId, cancellationToken);

			if (parent is null)
				return new OrgOpResult(false, "Parent Organization not found.");
			if (parent.OrganizationTypeId != organizationTypeId)
				return new OrgOpResult(false, "Parent Organization must be the same Organization Type.");
			if (parent.OrganizationLevel.Rank >= level.Rank)
				return new OrgOpResult(false, $"\"{parent.Name}\" ({parent.OrganizationLevel.Name}) is not above the selected Level in the hierarchy.");
		}

		if (siteId is not null)
		{
			var site = await db.Sites.FirstOrDefaultAsync(s => s.Id == siteId, cancellationToken);
			if (site is null)
				return new OrgOpResult(false, "Site not found.");

			var alreadyLinked = await db.Organizations
				.AnyAsync(o => o.SiteId == siteId && o.Id != excludeOrganizationId, cancellationToken);
			if (alreadyLinked)
				return new OrgOpResult(false, "That Site is already linked to another Organization.");

			if (!level.IsSiteEligible)
				return new OrgOpResult(false, $"\"{level.Name}\" is not marked as site-eligible — enable that on the Level first, or choose a different Level.");
		}

		return null;
	}

	// ------------------------------------------------------------
	// Parent Access Grant
	// ------------------------------------------------------------

	public async Task<List<ParentAccessGrantSummary>> GetGrantsAsync(CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		return await db.ParentAccessGrants
			.OrderBy(g => g.ParentOrganization.Name).ThenBy(g => g.ChildOrganization.Name)
			.Select(g => new ParentAccessGrantSummary(
				g.Id,
				g.ParentOrganizationId, g.ParentOrganization.Name,
				g.ChildOrganizationId, g.ChildOrganization.Name,
				g.AccessLevel))
			.ToListAsync(cancellationToken);
	}

	public async Task<OrgOpResult> CreateGrantAsync(
		Guid parentOrganizationId, Guid childOrganizationId, AccessLevel accessLevel, CancellationToken cancellationToken = default)
	{
		if (parentOrganizationId == childOrganizationId)
			return new OrgOpResult(false, "An Organization cannot grant itself access.");

		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		if (!await db.Organizations.AnyAsync(o => o.Id == parentOrganizationId, cancellationToken))
			return new OrgOpResult(false, "Parent Organization not found.");
		if (!await db.Organizations.AnyAsync(o => o.Id == childOrganizationId, cancellationToken))
			return new OrgOpResult(false, "Child Organization not found.");

		if (await db.ParentAccessGrants.AnyAsync(g => g.ParentOrganizationId == parentOrganizationId && g.ChildOrganizationId == childOrganizationId, cancellationToken))
			return new OrgOpResult(false, "A grant between these two Organizations already exists — edit it instead of creating a new one.");

		db.ParentAccessGrants.Add(new ParentAccessGrant
		{
			ParentOrganizationId = parentOrganizationId,
			ChildOrganizationId = childOrganizationId,
			AccessLevel = accessLevel,
		});

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	public async Task<OrgOpResult> UpdateGrantAsync(Guid id, AccessLevel accessLevel, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var grant = await db.ParentAccessGrants.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
		if (grant is null)
			return new OrgOpResult(false, "Grant not found.");

		grant.AccessLevel = accessLevel;
		grant.UpdatedAtUtc = DateTimeOffset.UtcNow;

		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	// Revoking access IS deleting the row — see ParentAccessGrant's doc
	// comment. There's no "set to Disabled" path.
	public async Task<OrgOpResult> DeleteGrantAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		var grant = await db.ParentAccessGrants.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
		if (grant is null)
			return new OrgOpResult(false, "Grant not found.");

		db.ParentAccessGrants.Remove(grant);
		await db.SaveChangesAsync(cancellationToken);
		return new OrgOpResult(true, null);
	}

	// Applies the Phase 1 "Parent Access Recommendation": when a child
	// Organization is created under a parent, the parent receives View Access
	// by default. Called by the Organizations UI right after a successful
	// CreateOrganizationAsync with a parent set — not from CreateOrganizationAsync
	// itself, so the default stays an explicit, visible step rather than a side
	// effect buried in organization creation.
	public Task<OrgOpResult> GrantDefaultParentAccessAsync(Guid parentOrganizationId, Guid childOrganizationId, CancellationToken cancellationToken = default)
		=> CreateGrantAsync(parentOrganizationId, childOrganizationId, AccessLevel.View, cancellationToken);
}
