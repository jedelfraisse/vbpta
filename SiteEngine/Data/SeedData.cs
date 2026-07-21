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

		db.SaveChanges();
	}
}
