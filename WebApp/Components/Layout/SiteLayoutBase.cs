using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebApp.Components.Layout;

public abstract class SiteLayoutBase : LayoutComponentBase
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

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

        var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? user.Identity?.Name;
    }
}
