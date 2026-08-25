using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SiteEngine.Entities;
using SiteEngine.Enums;
using WebApp.Services;

namespace WebApp.Components.Layout;

public abstract class SiteLayoutBase : LayoutComponentBase
{
	[Inject]
	protected NavigationManager NavigationManager { get; set; } = default!;

	[Inject]
	protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

	[Inject]
	protected SiteRoleResolver SiteRoleResolver { get; set; } = default!;

	protected bool ShowAdminLink { get; private set; }

	protected bool IsAuthenticated { get; private set; }

	// Null = anonymous. Non-null = authenticated — Viewer at minimum, even
	// when the user has no SiteUserRole for the site at all.
	protected SiteRole? CurrentRole { get; private set; }

	// siteId is the site to resolve the role against. Pass null when the
	// caller doesn't have a real site to scope against yet — the user still
	// resolves to Viewer rather than silently staying unauthenticated-looking.
	protected async Task RefreshUserStateAsync(Guid? siteId = null)
	{
		var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		var user = authState.User;
		IsAuthenticated = user.Identity?.IsAuthenticated ?? false;

		if (!IsAuthenticated)
		{
			ShowAdminLink = false;
			CurrentRole = null;
			return;
		}

		var identityUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		CurrentRole = identityUserId is not null && siteId is not null
			? await SiteRoleResolver.ResolveAsync(identityUserId, siteId.Value)
			: SiteRole.Viewer;

		ShowAdminLink = CurrentRole is SiteRole.SiteAdmin or SiteRole.DivisionAdmin or SiteRole.SuperAdmin;
	}

	// CSS custom properties consumed by CityWideLayout.css / UnitLayout.razor.css
	// (--primary-color, --accent-color, --top-bar-color, --footer-color-1..4).
	// Centralized here so Division and Local Unit layouts resolve/name the
	// same theme variables identically. Returns "" (no inline style) while
	// site is still null/loading — CSS var() fallbacks cover that gap.
	// Public (not just protected) so admin pages that don't inherit this base
	// — e.g. SiteDetail.razor's live Branding preview — can reuse the exact
	// same variable names a real masthead consumes.
	public static string ThemeStyle(Site? site)
	{
		if (site is null)
			return string.Empty;

		return $"--primary-color:{site.ResolvedPrimaryColor()};" +
			$"--accent-color:{site.ResolvedAccentColor()};" +
			$"--top-bar-color:{site.ResolvedTopBarColor()};" +
			$"--footer-color-1:{site.ResolvedFooterColor1()};" +
			$"--footer-color-2:{site.ResolvedFooterColor2()};" +
			$"--footer-color-3:{site.ResolvedFooterColor3()};" +
			$"--footer-color-4:{site.ResolvedFooterColor4()};";
	}

	// Each masthead logo needs its own width/height/fit behavior (not just a
	// shared color/size CSS var), so unlike ThemeStyle this is computed
	// per-<img> rather than once for the whole shell — see DivisionLayout.razor.
	// width/height fall back to the site's masthead default box (88x220) when
	// a logo has no size of its own; explicit width+height plus object-fit is
	// what actually enforces "the logo is exactly this size" — a CSS
	// max-width/max-height alone only caps it, it doesn't fix it.
	public static string LogoBoxStyle(Site? site, int? width, int? height, bool preserveAspectRatio)
	{
		var w = width ?? site?.MastheadLogoDefaultWidth ?? 260;
		var h = height ?? site?.MastheadLogoDefaultHeight ?? 110;
		var fit = preserveAspectRatio ? "contain" : "fill";
		return $"width:{w}px;height:{h}px;object-fit:{fit};";
	}
}
