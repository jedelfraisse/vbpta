using SiteEngine.Entities;

namespace WebApp.Services;

// Every existing site-relative link (DivisionLayout, UnitLayout) assumes it's
// already rendering on that site's own host, so there was no prior helper for
// building an absolute URL to a *different* site from Global Admin.
public static class SiteUrlHelper
{
	// If a site has its own custom Domain, that's always the host. Otherwise
	// the site is only reachable at {Hostname}.{PortalDomain} — with no
	// configured PortalDomain there's no way to build a working host, so we
	// return empty rather than emit a broken URL (e.g. a bare hostname or an
	// unconfigured/placeholder PortalDomain value).
	public static string BuildPublicUrl(Site site, string? portalDomain)
	{
		var domain = site.Domain?.Trim() ?? string.Empty;
		if (!string.IsNullOrEmpty(domain))
			return $"https://{domain}";

		var hostname = site.Hostname?.Trim() ?? string.Empty;
		var portal = portalDomain?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(hostname) || string.IsNullOrEmpty(portal))
			return string.Empty;

		return $"https://{hostname}.{portal}";
	}

	public static string BuildAdminUrl(Site site, string? portalDomain)
	{
		var baseUrl = BuildPublicUrl(site, portalDomain);
		return string.IsNullOrEmpty(baseUrl) ? string.Empty : $"{baseUrl}/admin";
	}
}
