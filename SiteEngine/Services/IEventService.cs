using SiteEngine.Entities;
namespace SiteEngine.Services;

public interface IEventService
{
    Task<List<EventItem>> GetUpcomingEventsAsync();
}
