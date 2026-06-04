namespace SiteEngine.Entities;

public class Announcement : ISiteScopedEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid SiteId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public Site? Site { get; set; }
}
