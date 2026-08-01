using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public class SiteContext
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;
	private readonly SetupStateService _setup;

	public Site? CurrentSite { get; private set; }
	public bool IsReady { get; private set; }

	// True once InitializeAsync has run against a host that doesn't map to any
	// Division/Local Unit and isn't the portal's own configured domain — i.e.
	// the requested subdomain doesn't exist. CurrentSite still falls back to
	// the Portal site in this case so callers get the Portal layout/home
	// either way; this flag is what lets that fallback be distinguished from
	// a legitimate visit to the portal's own domain.
	public bool SiteNotFound { get; private set; }

	// The host that was actually requested, lowercased with the port
	// stripped — so the UI can tell the visitor what they typed.
	public string? RequestedHost { get; private set; }

	public bool IsAdminContext => CurrentSite?.SiteType == SiteType.Portal;
	public bool IsDivisionContext => CurrentSite?.SiteType == SiteType.Division;
	public bool IsUnitContext => CurrentSite?.SiteType == SiteType.LocalUnit;

	public SiteContext(IDbContextFactory<AppDbContext> dbFactory, SetupStateService setup)
	{
		_dbFactory = dbFactory;
		_setup = setup;

		var status = _setup.GetStatus();
		IsReady = status.IsFullyConfigured;
	}

	public async Task InitializeAsync(string host)
	{
		if (!IsReady)
		{
			CurrentSite = null;
			return;
		}

		host = host.ToLowerInvariant().Split(':')[0];
		RequestedHost = host;

		await using var db = await _dbFactory.CreateDbContextAsync();

		// Hostname is stored as just the subdomain label (see
		// SiteAdminService.CreateSiteAsync and SiteUrlHelper.BuildPublicUrl,
		// which builds "{Hostname}.{PortalDomain}"), so it has to be matched
		// against the first label of the request host, not the full host.
		// Domain, by contrast, holds a complete custom domain and is compared
		// in full.
		var subdomain = host.Split('.')[0];

		CurrentSite =
			await db.Sites.FirstOrDefaultAsync(s => s.Domain != "" && s.Domain.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType != SiteType.Portal && s.Hostname.ToLower() == subdomain);

		if (CurrentSite is not null)
		{
			SiteNotFound = false;
			return;
		}

		// PortalDomain is operator-entered free text and has been observed
		// stored with a trailing port (e.g. "localhost:5000") — strip it the
		// same way the request host already is, so the comparison below isn't
		// comparing "localhost" against "localhost:5000".
		var portalDomain = (await db.PortalConfigs.FirstOrDefaultAsync())?.PortalDomain?.Trim().ToLowerInvariant().Split(':')[0];

		// A hit on the portal's own configured domain (or no domain configured
		// yet) is a legitimate portal visit, not a failed subdomain lookup. If
		// no PortalDomain is configured yet, fall back to treating any
		// multi-label host (e.g. "blueschool.localhost") as an attempted
		// subdomain and a bare host ("localhost") as the portal root.
		SiteNotFound = !string.IsNullOrEmpty(portalDomain)
			? host != portalDomain
			: host.Contains('.');

		CurrentSite = await db.Sites.FirstOrDefaultAsync(s => s.SiteType == SiteType.Portal);
	}
}
