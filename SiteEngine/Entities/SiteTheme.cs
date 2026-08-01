using SiteEngine.Enums;

namespace SiteEngine.Entities;

// Global fallback theme — used when neither a Site nor (for a Local Unit)
// its parent Division defines a given value. Kept in sync with the CSS
// var() fallbacks in CityWideLayout.css / UnitLayout.razor.css so a site
// with nothing configured still renders sensibly even before Site data
// has loaded.
public static class SiteThemeDefaults
{
	public const string PrimaryColor = "#1F3A5F";
	public const string AccentColor = "#2E86AB";
	public const string TopBarColor = "#0F2440";
	public const string FooterColor1 = "#1F3A5F";
	public const string FooterColor2 = "#173252";
	public const string FooterColor3 = "#102540";
	public const string FooterColor4 = "#0A1930";
}

// Resolves a Site's effective theme: the Site's own value, else (Local Unit
// only) its parent Division's value, else the global default. Division
// sites fall straight through to the global default — only Local Units
// inherit from a parent.
public static class SiteThemeExtensions
{
	public static string ResolvedPrimaryColor(this Site site) =>
		Resolve(site, s => s.PrimaryColor, SiteThemeDefaults.PrimaryColor);

	public static string ResolvedAccentColor(this Site site) =>
		Resolve(site, s => s.AccentColor, SiteThemeDefaults.AccentColor);

	public static string ResolvedTopBarColor(this Site site) =>
		Resolve(site, s => s.TopBarColor, SiteThemeDefaults.TopBarColor);

	public static string ResolvedFooterColor1(this Site site) =>
		Resolve(site, s => s.FooterColor1, SiteThemeDefaults.FooterColor1);

	public static string ResolvedFooterColor2(this Site site) =>
		Resolve(site, s => s.FooterColor2, SiteThemeDefaults.FooterColor2);

	public static string ResolvedFooterColor3(this Site site) =>
		Resolve(site, s => s.FooterColor3, SiteThemeDefaults.FooterColor3);

	public static string ResolvedFooterColor4(this Site site) =>
		Resolve(site, s => s.FooterColor4, SiteThemeDefaults.FooterColor4);

	// No global default for background images — an unconfigured Site (and
	// unconfigured parent Division) simply renders with no image.
	public static string? ResolvedMenuBackgroundImageUrl(this Site site) =>
		ResolveImage(site, s => s.MenuBackgroundImageUrl);

	public static string? ResolvedPageBackgroundImageUrl(this Site site) =>
		ResolveImage(site, s => s.PageBackgroundImageUrl);

	private static string Resolve(Site site, Func<Site, string?> selector, string globalDefault)
	{
		return ResolveOwnOrParent(site, selector) ?? globalDefault;
	}

	private static string? ResolveImage(Site site, Func<Site, string?> selector)
	{
		return ResolveOwnOrParent(site, selector);
	}

	private static string? ResolveOwnOrParent(Site site, Func<Site, string?> selector)
	{
		var own = selector(site);
		if (!string.IsNullOrWhiteSpace(own))
			return own;

		if (site.SiteType == SiteType.LocalUnit && site.ParentSite is not null)
		{
			var parentValue = selector(site.ParentSite);
			if (!string.IsNullOrWhiteSpace(parentValue))
				return parentValue;
		}

		return null;
	}
}
