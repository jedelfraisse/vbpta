using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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
}
