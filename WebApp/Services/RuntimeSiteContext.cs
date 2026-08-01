using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public class RuntimeSiteContext
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;

	public Site? CurrentSite { get; private set; }

	public RuntimeSiteContext(IDbContextFactory<AppDbContext> dbFactory)
	{
		_dbFactory = dbFactory;
	}

	public async Task InitializeAsync(string host)
	{
		host = host.ToLowerInvariant().Split(':')[0];

		await using var db = await _dbFactory.CreateDbContextAsync();

		// Hostname is stored as just the subdomain label (see SiteContext.
		// InitializeAsync for the full explanation), so it has to be matched
		// against the first label of the request host, not the full host.
		var subdomain = host.Split('.')[0];

		CurrentSite =
			await db.Sites.FirstOrDefaultAsync(s => s.Domain != "" && s.Domain.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType != SiteType.Portal && s.Hostname.ToLower() == subdomain)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType == SiteType.Portal);
	}
}
