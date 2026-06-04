using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Sites;

namespace SiteEngine.Services;

public class AnnouncementService(IDbContextFactory<AppDbContext> dbContextFactory, ISiteContext siteContext) : IAnnouncementService
{
	private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;
	private readonly ISiteContext _siteContext = siteContext;

	public async Task<IReadOnlyList<Announcement>> GetVisibleAnnouncementsAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		var query = dbContext.Announcements
			.AsNoTracking()
			.Include(x => x.Site)
			.AsQueryable();

		if (!_siteContext.IsAdminContext)
		{
			if (_siteContext.CurrentSite is null)
			{
				return Array.Empty<Announcement>();
			}

			query = query.Where(x => x.SiteId == _siteContext.CurrentSite.Id);
		}

		var announcements = await query.ToListAsync(cancellationToken);
		return announcements
			.OrderByDescending(x => x.PublishedAtUtc)
			.ToList();
	}
}
