using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SiteEngine.Sites;
using WebApp.Authentication;

namespace WebApp.Components.Layout;

public abstract class SiteLayoutBase : LayoutComponentBase
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected ISiteContext SiteContext { get; set; } = default!;

    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    protected bool ShowAdminLink { get; private set; }

    protected bool IsAuthenticated { get; private set; }

    protected bool IsGlobalAdmin { get; private set; }

    protected async Task RefreshUserStateAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
        if (!IsAuthenticated)
        {
            ShowAdminLink = false;
            IsGlobalAdmin = false;
            return;
        }

        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        IsGlobalAdmin = await SiteContext.UserHasGlobalAdminRoleAsync(userId);
        ShowAdminLink = await SiteContext.UserHasSiteAdminAccessAsync(userId);

        var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? user.Identity?.Name;
    }
}
