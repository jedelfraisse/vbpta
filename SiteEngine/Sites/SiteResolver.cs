using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Options;

namespace SiteEngine.Sites;

public class SiteResolver(
	IDbContextFactory<AppDbContext> dbContextFactory,
	IMemoryCache memoryCache,
	IHostEnvironment hostEnvironment,
	IOptions<SiteHostMappingOptions> mappingOptions) : ISiteResolver
{
	private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
	private readonly IMemoryCache _memoryCache = memoryCache;
	private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
	private readonly SiteHostMappingOptions _mappingOptions = mappingOptions.Value;

	public Task<SiteResolutionResult?> ResolveAsync(string host, CancellationToken cancellationToken = default)
	{
		var normalizedHost = NormalizeHost(host);
		if (string.IsNullOrWhiteSpace(normalizedHost))
		{
			return Task.FromResult<SiteResolutionResult?>(null);
		}

		var canonicalHost = _mappingOptions.Hosts.TryGetValue(normalizedHost, out var mappedHost)
			? NormalizeHost(mappedHost)
			: normalizedHost;

		if (string.IsNullOrWhiteSpace(canonicalHost))
		{
			return Task.FromResult<SiteResolutionResult?>(null);
		}

		if (_hostEnvironment.IsDevelopment())
		{
			return ResolveFromDatabaseAsync(canonicalHost, cancellationToken);
		}

		var cacheKey = $"site-resolution:{canonicalHost}";
		return _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return await ResolveFromDatabaseAsync(canonicalHost, cancellationToken);
		});
	}

	private async Task<SiteResolutionResult?> ResolveFromDatabaseAsync(string canonicalHost, CancellationToken cancellationToken)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		Site? site = await dbContext.Sites
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Hostname == canonicalHost, cancellationToken);

		if (site is null)
		{
			return null;
		}

		return new SiteResolutionResult(site, site.ToSiteConfig(), site.IsAdminPortal);
	}

	private static string NormalizeHost(string? host)
	{
		return host?.Trim().ToLowerInvariant() ?? string.Empty;
	}

	public void InvalidateHost(string host)
	{
		var normalizedHost = NormalizeHost(host);
		if (string.IsNullOrWhiteSpace(normalizedHost))
		{
			return;
		}

		_memoryCache.Remove($"site-resolution:{normalizedHost}");
		if (_mappingOptions.Hosts.TryGetValue(normalizedHost, out var mappedHost))
		{
			var normalizedMapped = NormalizeHost(mappedHost);
			if (!string.IsNullOrWhiteSpace(normalizedMapped))
			{
				_memoryCache.Remove($"site-resolution:{normalizedMapped}");
			}
		}
	}
}
