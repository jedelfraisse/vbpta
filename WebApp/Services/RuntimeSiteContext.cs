using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public class RuntimeSiteContext
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;

	public Site? CurrentSite { get; private set; }
	private string? _portalDomain;

	public RuntimeSiteContext(IDbContextFactory<AppDbContext> dbFactory)
	{
		_dbFactory = dbFactory;
	}

	public async Task InitializeAsync(string host)
	{
		host = host.ToLowerInvariant().Split(':')[0];

		await using var db = await _dbFactory.CreateDbContextAsync();

		// Hostname is stored as just the subdomain label (see SiteContext.
		// InitializeAsync for the full explanation), so it has to be matched
		// against the first label of the request host, not the full host.
		var subdomain = host.Split('.')[0];

		CurrentSite =
			await db.Sites.FirstOrDefaultAsync(s => s.Domain != "" && s.Domain.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType != SiteType.Portal && s.Hostname.ToLower() == subdomain)
			// PtaId (unique, always set) always works as a fallback access path
			// - "{PtaId}.{PortalDomain}" - for a site that hasn't had a Hostname
			// or Domain configured yet. See GetCanonicalRedirectUrl below.
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType != SiteType.Portal && s.PtaId.ToLower() == subdomain)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType == SiteType.Portal);

		_portalDomain = (await db.PortalConfigs.FirstOrDefaultAsync())?.PortalDomain;
	}

	// Statuses whose hosted subdomain shouldn't render the live layout at all:
	// Pending (not launched yet), ActiveListed (SiteStatus's own doc comment
	// says this status is explicitly "not using the hosted portal"), and
	// Inactive (deliberately paused). MembersOnly is NOT included here — that
	// status still renders its real hosted content, just gated by role once
	// inside. Checked before GetCanonicalRedirectUrl below, not after — a
	// Pending site visited on its PtaId fallback subdomain must not get
	// bounced to its real (still-Pending) hosted hostname first.
	private static readonly SiteStatus[] StatusesRedirectToDirectory =
		[SiteStatus.Pending, SiteStatus.ActiveListed, SiteStatus.Inactive];

	// Non-null means: this status isn't meant to expose live hosted content —
	// send the visitor to the portal-side directory stub instead (see
	// UnitSites/Details.razor, which shows this exact same info for these
	// same statuses rather than redirecting on to a hosted site).
	public string? GetDirectoryRedirectUrl()
	{
		if (CurrentSite is null || CurrentSite.SiteType == SiteType.Portal)
			return null;

		if (!StatusesRedirectToDirectory.Contains(CurrentSite.SiteStatus))
			return null;

		var url = SiteUrlHelper.BuildDirectoryDetailUrl(CurrentSite.PtaId, _portalDomain);
		return string.IsNullOrEmpty(url) ? null : url;
	}

	// If CurrentSite has a real Hostname or Domain configured and the visitor
	// didn't arrive on it (e.g. they came in on the PtaId fallback subdomain
	// above, or on an old Hostname after a custom Domain was added), this
	// returns the canonical URL to redirect them to. Null means the request
	// is already on the canonical host, or no better URL exists yet.
	public string? GetCanonicalRedirectUrl(string requestedHost)
	{
		if (CurrentSite is null || CurrentSite.SiteType == SiteType.Portal)
			return null;

		var canonicalUrl = SiteUrlHelper.BuildPublicUrl(CurrentSite, _portalDomain);
		if (string.IsNullOrEmpty(canonicalUrl))
			return null;

		var canonicalHost = new Uri(canonicalUrl).Host;
		var requestedHostOnly = requestedHost.ToLowerInvariant().Split(':')[0];

		return canonicalHost == requestedHostOnly ? null : canonicalUrl;
	}
}
