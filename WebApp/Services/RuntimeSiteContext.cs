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

		CurrentSite =
			await db.Sites.FirstOrDefaultAsync(s => s.Hostname.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.Domain.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType == SiteType.Portal);
	}
}
