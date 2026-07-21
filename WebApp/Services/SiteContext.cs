using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Enums;

namespace WebApp.Services;

public class SiteContext
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;
	private readonly SetupStateService _setup;

	public Site? CurrentSite { get; private set; }
	public bool IsReady { get; private set; }

	public bool SiteNotFound => IsReady && CurrentSite is null;

	public bool IsAdminContext => CurrentSite?.SiteType == SiteType.Portal;
	public bool IsDivisionContext => CurrentSite?.SiteType == SiteType.Division;
	public bool IsUnitContext => CurrentSite?.SiteType == SiteType.LocalUnit;

	public SiteContext(IDbContextFactory<AppDbContext> dbFactory, SetupStateService setup)
	{
		_dbFactory = dbFactory;
		_setup = setup;

		var status = _setup.GetStatus();
		IsReady = status.IsFullyConfigured;
	}

	public async Task InitializeAsync(string host)
	{
		if (!IsReady)
		{
			CurrentSite = null;
			return;
		}

		host = host.ToLowerInvariant().Split(':')[0];

		await using var db = await _dbFactory.CreateDbContextAsync();

		CurrentSite =
			await db.Sites.FirstOrDefaultAsync(s => s.Hostname.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.Domain.ToLower() == host)
			?? await db.Sites.FirstOrDefaultAsync(s => s.SiteType == SiteType.Portal);
	}
}
