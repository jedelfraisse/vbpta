using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiteEngine.Data;
using SiteEngine.Entities;
using SiteEngine.Sites;

namespace SiteEngine.Services;

public class AnnouncementService(IServiceScopeFactory scopeFactory, ISiteContext siteContext) : IAnnouncementService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ISiteContext _siteContext = siteContext;

    public async Task<IReadOnlyList<Announcement>> GetVisibleAnnouncementsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
