using SiteEngine.Config;
using SiteEngine.Entities;
using SiteEngine.Sites;

namespace VbptaWeb.Tests.Support;

internal sealed class SiteContextStub : ISiteContext
{
	public Site? CurrentSite { get; init; }
	public SiteConfig SiteConfig { get; init; } = new();
	public bool IsAdminContext { get; init; }

	public Task InitializeAsync(string host, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public Task<bool> IsUserAuthorizedAtCurrentSiteAsync(string? userId)
	{
		return Task.FromResult(false);
	}

	public Task<bool> UserHasRoleAtCurrentSiteAsync(string? userId, SiteRole role)
	{
		return Task.FromResult(false);
	}

	public Task<bool> UserHasGlobalAdminRoleAsync(string? userId)
	{
		return Task.FromResult(false);
	}

	public Task<bool> UserHasSiteAdminAccessAsync(string? userId)
	{
		return Task.FromResult(false);
	}

	public Task<string?> GetCurrentSiteAdminEmailAsync()
	{
		return Task.FromResult<string?>(null);
	}
}
