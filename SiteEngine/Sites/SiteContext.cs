using SiteEngine.Config;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Services;

namespace SiteEngine.Sites;

public class SiteContext(ISiteResolver siteResolver, ISiteUserService siteUserService) : ISiteContext
{
	private readonly ISiteResolver _siteResolver = siteResolver;
	private readonly ISiteUserService _siteUserService = siteUserService;

	public Site? CurrentSite { get; private set; }

	public SiteConfig SiteConfig { get; private set; } = SeedData.DefaultAdminSite.ToSiteConfig();

	public bool IsAdminContext { get; private set; }

	public async Task InitializeAsync(string host, CancellationToken cancellationToken = default)
	{
		var resolved = await _siteResolver.ResolveAsync(host, cancellationToken);

		CurrentSite = resolved?.Site;
		IsAdminContext = resolved?.IsAdminContext ?? false;
		SiteConfig = resolved?.SiteConfig ?? SeedData.DefaultAdminSite.ToSiteConfig();
	}

	public async Task<bool> IsUserAuthorizedAtCurrentSiteAsync(string? userId)
	{
		if (string.IsNullOrEmpty(userId) || CurrentSite == null)
			return false;

		var roles = await _siteUserService.GetUserRolesAtSiteAsync(userId, CurrentSite.Id);
		return roles.Any();
	}

	public async Task<bool> UserHasRoleAtCurrentSiteAsync(string? userId, SiteRole role)
	{
		if (string.IsNullOrEmpty(userId) || CurrentSite == null)
			return false;

		return await _siteUserService.UserHasRoleAsync(userId, CurrentSite.Id, role);
	}

	public async Task<bool> UserHasGlobalAdminRoleAsync(string? userId)
	{
		if (string.IsNullOrEmpty(userId))
		{
			return false;
		}

		return await _siteUserService.UserHasRoleAsync(userId, SeedData.DefaultAdminSiteId, SiteRole.Admin);
	}

	public async Task<bool> UserHasSiteAdminAccessAsync(string? userId)
	{
		if (CurrentSite == null || string.IsNullOrEmpty(userId))
		{
			return false;
		}

		var hasLocalAdminRole = await _siteUserService.UserHasRoleAsync(userId, CurrentSite.Id, SiteRole.Admin);
		if (hasLocalAdminRole)
		{
			return true;
		}

		return await UserHasGlobalAdminRoleAsync(userId);
	}

	public async Task<string?> GetCurrentSiteAdminEmailAsync()
	{
		if (CurrentSite == null)
			return null;

		return await _siteUserService.GetSiteAdminEmailAsync(CurrentSite.Id);
	}
}
