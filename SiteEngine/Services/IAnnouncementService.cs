using SiteEngine.Entities;

namespace SiteEngine.Services;

public interface IAnnouncementService
{
	Task<IReadOnlyList<Announcement>> GetVisibleAnnouncementsAsync(CancellationToken cancellationToken = default);
}
