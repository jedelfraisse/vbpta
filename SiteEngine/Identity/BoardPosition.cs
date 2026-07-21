using SiteEngine.Entities;

namespace SiteEngine.Identity;

public class BoardPosition
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid SiteUserId { get; set; }
	public SiteUser SiteUser { get; set; } = null!;

	public Guid SiteId { get; set; }
	public Site Site { get; set; } = null!;

	public string PositionName { get; set; } = string.Empty;

	public string SchoolYear { get; set; } = string.Empty;

	public DateTimeOffset? StartUtc { get; set; }
	public DateTimeOffset? EndUtc { get; set; }
}
