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
		=> BuildPublicUrl(site.Hostname, site.Domain, portalDomain);

	// Same rule as the Site overload above, for callers (e.g. the /unit-sites
	// Details page) that only have a projected Hostname/Domain pair rather
	// than a full Site entity.
	public static string BuildPublicUrl(string? hostname, string? domain, string? portalDomain)
	{
		var trimmedDomain = domain?.Trim() ?? string.Empty;
		if (!string.IsNullOrEmpty(trimmedDomain))
			return $"https://{trimmedDomain}";

		var trimmedHostname = hostname?.Trim() ?? string.Empty;
		var portal = portalDomain?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(trimmedHostname) || string.IsNullOrEmpty(portal))
			return string.Empty;

		return $"https://{trimmedHostname}.{portal}";
	}

	public static string BuildAdminUrl(Site site, string? portalDomain)
	{
		var baseUrl = BuildPublicUrl(site, portalDomain);
		return string.IsNullOrEmpty(baseUrl) ? string.Empty : $"{baseUrl}/admin";
	}
}
