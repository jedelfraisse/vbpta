namespace SiteEngine.Entities;

public class SiteEvent : ISiteScopedEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid SiteId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? Location { get; set; }
	public DateTimeOffset StartsAtUtc { get; set; }
	public DateTimeOffset? EndsAtUtc { get; set; }

	public Site? Site { get; set; }
}
