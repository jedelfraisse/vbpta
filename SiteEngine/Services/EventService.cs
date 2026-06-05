using SiteEngine.Entities;
namespace SiteEngine.Services;

public class EventService : IEventService
{
    public Task<List<EventItem>> GetUpcomingEventsAsync()
    {
        // FUTURE: Load from DB
        return Task.FromResult(new List<EventItem>
        {
            new EventItem { Title = "Citywide PTA Leadership Training", Date = new DateTime(2026, 6, 15) },
            new EventItem { Title = "Council General Membership Meeting", Date = new DateTime(2026, 8, 15) }
        });
    }
}
