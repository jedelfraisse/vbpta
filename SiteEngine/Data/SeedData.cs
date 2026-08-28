using System.Text.Json.Nodes;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace SiteEngine.Data;

public static class SeedData
{
	// Stable IDs
	public const int DefaultGlobalConfigId = 1;
	public static readonly Guid DefaultPortalSiteId =
		Guid.Parse("0F89AC2B-A0AC-40B8-B886-FD117E35903C");

	// Static fallback defaults (used if JSON missing)
	public static readonly PortalConfig DefaultGlobalConfig = new()
	{
		PortalName = "",
		PortalDomain = "",
		SmtpHost = "",
		SmtpPort = 587,
		SmtpFromAddress = "",
		SmtpUsername = "",
		SmtpPassword = "",
		UseSsl = true
	};

	public static readonly Site DefaultPortalSite = new()
	{
		SiteType = SiteType.Portal,
		PtaId = "00000000",
		Hostname = "",
		Domain = "",
		SiteName = "PTA Portal",
		LogoUrl = "/images/logo.png",
		BannerUrl = "/images/banner.png",
		PrimaryColor = "#003366",
		AccentColor = "#FFCC00",
		HeaderText = "Welcome to the PTA Portal.",
		CreatedAtUtc = DateTimeOffset.UtcNow,
		UpdatedAtUtc = DateTimeOffset.UtcNow
	};

	// ------------------------------------------------------------
	// Load defaults from portaldefaults.json (if present)
	// ------------------------------------------------------------
	private static JsonNode? LoadDefaultsJson()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "portaldefaults.json");
		if (!File.Exists(path))
			return null;

		return JsonNode.Parse(File.ReadAllText(path));
	}

	// ------------------------------------------------------------
	// Main seeding method
	// ------------------------------------------------------------
	public static void EnsureSeedData(AppDbContext db)
	{
		var json = LoadDefaultsJson();

		// ------------------------------------------------------------
		// 1. PortalConfig
		// ------------------------------------------------------------
		var config = db.PortalConfigs.FirstOrDefault(c => c.Id == DefaultGlobalConfigId);
		if (config == null)
		{
			var cfgJson = json?["PortalConfig"];

			var newConfig = new PortalConfig
			{
				Id = DefaultGlobalConfigId,
				PortalName = cfgJson?["PortalName"]?.ToString() ?? DefaultGlobalConfig.PortalName,
				PortalDomain = cfgJson?["PortalDomain"]?.ToString() ?? DefaultGlobalConfig.PortalDomain,
				SmtpHost = cfgJson?["SmtpHost"]?.ToString() ?? DefaultGlobalConfig.SmtpHost,
				SmtpPort = int.TryParse(cfgJson?["SmtpPort"]?.ToString(), out var port)
					? port : DefaultGlobalConfig.SmtpPort,
				SmtpFromAddress = cfgJson?["SmtpFromAddress"]?.ToString() ?? DefaultGlobalConfig.SmtpFromAddress,
				SmtpUsername = cfgJson?["SmtpUsername"]?.ToString() ?? DefaultGlobalConfig.SmtpUsername,
				SmtpPassword = cfgJson?["SmtpPassword"]?.ToString() ?? DefaultGlobalConfig.SmtpPassword,
				UseSsl = bool.TryParse(cfgJson?["UseSsl"]?.ToString(), out var ssl)
					? ssl : DefaultGlobalConfig.UseSsl
			};

			db.PortalConfigs.Add(newConfig);
		}

		// ------------------------------------------------------------
		// 2. Portal Site
		// ------------------------------------------------------------
		var site = db.Sites.FirstOrDefault(s => s.Id == DefaultPortalSiteId);
		if (site == null)
		{
			var siteJson = json?["PortalSite"];

			var newSite = new Site
			{
				Id = DefaultPortalSiteId,
				SiteType = SiteType.Portal,
				PtaId = siteJson?["PtaId"]?.ToString() ?? DefaultPortalSite.PtaId,
				Hostname = siteJson?["Hostname"]?.ToString() ?? DefaultPortalSite.Hostname,
				Domain = siteJson?["Domain"]?.ToString() ?? DefaultPortalSite.Domain,
				SiteName = siteJson?["SiteName"]?.ToString() ?? DefaultPortalSite.SiteName,
				LogoUrl = siteJson?["LogoUrl"]?.ToString() ?? DefaultPortalSite.LogoUrl,
				BannerUrl = siteJson?["BannerUrl"]?.ToString() ?? DefaultPortalSite.BannerUrl,
				PrimaryColor = siteJson?["PrimaryColor"]?.ToString() ?? DefaultPortalSite.PrimaryColor,
				AccentColor = siteJson?["AccentColor"]?.ToString() ?? DefaultPortalSite.AccentColor,
				HeaderText = siteJson?["HeaderText"]?.ToString() ?? DefaultPortalSite.HeaderText,
				CreatedAtUtc = DateTimeOffset.UtcNow,
				UpdatedAtUtc = DateTimeOffset.UtcNow
			};

			db.Sites.Add(newSite);
		}

		// ------------------------------------------------------------
		// 3. Organization Types
		// ------------------------------------------------------------
		if (!db.OrganizationTypes.Any())
		{
			db.OrganizationTypes.Add(new OrganizationType
			{
				Name = "PTA",
				Description = "Parent Teacher Associations connect families, teachers, and " +
					"schools — organizing everything from fundraisers and family events to " +
					"advocacy for students, all built on the National PTA's everychild.onevoice. " +
					"mission. A PTA is organized as a Division (e.g. a citywide or regional " +
					"council) made up of Local Units (individual school PTAs), each with its " +
					"own site on this portal.",
				IconClass = "fa-solid fa-people-roof",
				SortOrder = 1,
			});
		}

		db.SaveChanges();

		// ------------------------------------------------------------
		// 4. Organization Framework backfill
		// ------------------------------------------------------------
		BackfillOrganizations(db);

		db.SaveChanges();
	}

	// Phase 1's "Existing Data Backfill": every Division/Local Unit Site
	// becomes an Organization, so the Organization Framework has something
	// real to show from day one rather than starting empty next to years of
	// existing Sites. Idempotent — safe to call on every startup, same as
	// the rest of EnsureSeedData.
	//
	// The Portal site is deliberately excluded. It isn't a PTA (or any other)
	// community — it's the neutral hub — so it gets no Organization row.
	//
	// Backfilled Organizations are placed under "Division" (Rank 1) and
	// "Local Unit" (Rank 2) — two Levels created here under the "PTA" type,
	// mirroring exactly what SiteType already enforces today (Portal >
	// Division > LocalUnit), nothing richer. This deliberately does NOT
	// invent the fuller National/State/Region/Council/Unit style hierarchy
	// an admin might configure later through Global Admin > Organizations —
	// that's a real administrative decision for a real install to make, not
	// something a backfill should guess at. An admin can insert Levels above
	// "Division" at any time; existing Organizations don't need to move to
	// make room, since OrganizationLevel.Rank is just an integer, not a
	// fixed-size list.
	private static void BackfillOrganizations(AppDbContext db)
	{
		var ptaType = db.OrganizationTypes.FirstOrDefault(t => t.Name == "PTA");
		if (ptaType is null)
			return; // Nothing to backfill against — an admin removed/renamed the default type.

		var divisionLevel = db.OrganizationLevels.FirstOrDefault(l => l.OrganizationTypeId == ptaType.Id && l.Name == "Division");
		if (divisionLevel is null)
		{
			divisionLevel = new OrganizationLevel { OrganizationTypeId = ptaType.Id, Name = "Division", Rank = 1, IsSiteEligible = true };
			db.OrganizationLevels.Add(divisionLevel);
		}

		var unitLevel = db.OrganizationLevels.FirstOrDefault(l => l.OrganizationTypeId == ptaType.Id && l.Name == "Local Unit");
		if (unitLevel is null)
		{
			unitLevel = new OrganizationLevel { OrganizationTypeId = ptaType.Id, Name = "Local Unit", Rank = 2, IsSiteEligible = true };
			db.OrganizationLevels.Add(unitLevel);
		}

		db.SaveChanges(); // Levels need real Ids before Organizations can reference them below.

		var linkedSiteIds = db.Organizations.Where(o => o.SiteId != null).Select(o => o.SiteId!.Value).ToHashSet();

		// Divisions first — a Local Unit's backfilled Organization needs its
		// parent Division's Organization to already exist (and have an Id) so
		// ParentOrganizationId can be set correctly below.
		var divisionsBySiteId = new Dictionary<Guid, Organization>();
		foreach (var site in db.Sites.Where(s => s.SiteType == SiteType.Division && !linkedSiteIds.Contains(s.Id)))
		{
			var org = new Organization { Name = site.SiteName, OrganizationTypeId = ptaType.Id, OrganizationLevelId = divisionLevel.Id, SiteId = site.Id };
			db.Organizations.Add(org);
			divisionsBySiteId[site.Id] = org;
		}

		db.SaveChanges(); // Division Organizations need real Ids before Local Units can reference them as parents.

		foreach (var site in db.Sites.Where(s => s.SiteType == SiteType.LocalUnit && !linkedSiteIds.Contains(s.Id)))
		{
			Guid? parentOrgId = null;
			if (site.ParentSiteId is Guid parentSiteId)
			{
				if (divisionsBySiteId.TryGetValue(parentSiteId, out var parentOrg))
					parentOrgId = parentOrg.Id;
				else
					parentOrgId = db.Organizations.FirstOrDefault(o => o.SiteId == parentSiteId)?.Id;
			}

			db.Organizations.Add(new Organization
			{
				Name = site.SiteName,
				OrganizationTypeId = ptaType.Id,
				OrganizationLevelId = unitLevel.Id,
				ParentOrganizationId = parentOrgId,
				SiteId = site.Id,
			});
		}
	}
}
