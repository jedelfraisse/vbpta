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

	private async Task<SiteResolutionResult?> ResolveFromDatabaseAsync(string host, CancellationToken cancellationToken)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var globalConfig = await dbContext.GlobalConfigs
			.AsNoTracking()
			.OrderBy(x => x.Id)
			.FirstOrDefaultAsync(cancellationToken);
		if (globalConfig is null)
		{
			var directMatch = await dbContext.Sites
				.AsNoTracking()
				.SingleOrDefaultAsync(x => x.Hostname == host || x.Domain == host, cancellationToken);
			return directMatch is null
				? null
				: new SiteResolutionResult(directMatch, directMatch.ToSiteConfig(), directMatch.IsAdminPortal);
		}

		var rootDomain = NormalizeHost(globalConfig.RootDomain);
		var platformDomain = NormalizeHost(globalConfig.PlatformDomain);
		var cityWideSite = await dbContext.Sites.AsNoTracking().SingleOrDefaultAsync(x => x.IsCityWide, cancellationToken);

		if (IsAdminHost(host, rootDomain, platformDomain))
		{
			var adminSite = await dbContext.Sites
				.AsNoTracking()
				.SingleOrDefaultAsync(
					x => x.PtaId == SeedData.DefaultAdminPtaId || x.IsAdminPortal,
					cancellationToken);
			if (adminSite is not null)
			{
				return new SiteResolutionResult(adminSite, adminSite.ToSiteConfig(), IsAdminContext: true);
			}
		}

		if (!string.IsNullOrWhiteSpace(rootDomain)
			&& string.Equals(host, rootDomain, StringComparison.OrdinalIgnoreCase)
			&& cityWideSite is not null)
		{
			return new SiteResolutionResult(cityWideSite, cityWideSite.ToSiteConfig(), IsAdminContext: false);
		}

		var subdomain = ExtractSubdomain(host, platformDomain);
		if (!string.IsNullOrWhiteSpace(subdomain))
		{
			var subdomainSite = await dbContext.Sites
				.AsNoTracking()
				.SingleOrDefaultAsync(
					x => x.Hostname == subdomain || x.Hostname == host,
					cancellationToken);
			if (subdomainSite is not null)
			{
				return new SiteResolutionResult(subdomainSite, subdomainSite.ToSiteConfig(), subdomainSite.IsAdminPortal);
			}

			if (cityWideSite is not null)
			{
				return new SiteResolutionResult(cityWideSite, cityWideSite.ToSiteConfig(), cityWideSite.IsAdminPortal, SiteNotFound: true);
			}
		}

		var customDomainSite = await dbContext.Sites
			.AsNoTracking()
			.SingleOrDefaultAsync(x => x.Domain == host, cancellationToken);
		if (customDomainSite is not null)
		{
			return new SiteResolutionResult(customDomainSite, customDomainSite.ToSiteConfig(), customDomainSite.IsAdminPortal);
		}

		if (cityWideSite is not null)
		{
			return new SiteResolutionResult(cityWideSite, cityWideSite.ToSiteConfig(), cityWideSite.IsAdminPortal, SiteNotFound: true);
		}

		return null;
	}

	private static string ExtractSubdomain(string host, string platformDomain)
	{
		if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(platformDomain))
		{
			return string.Empty;
		}

		var suffix = $".{platformDomain}";
		if (!host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}

		var leadingPart = host[..^suffix.Length];
		if (string.IsNullOrWhiteSpace(leadingPart) || leadingPart.Contains('.', StringComparison.Ordinal))
		{
			return string.Empty;
		}

		return leadingPart;
	}

	private static bool IsAdminHost(string host, string rootDomain, string platformDomain)
	{
		if (!host.StartsWith("admin.", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrWhiteSpace(rootDomain))
		{
			allowedHosts.Add($"admin.{rootDomain}");
		}
		if (!string.IsNullOrWhiteSpace(platformDomain))
		{
			allowedHosts.Add($"admin.{platformDomain}");
		}

		if (allowedHosts.Count == 0)
		{
			return true;
		}

		return allowedHosts.Contains(host);
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
