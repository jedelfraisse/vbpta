namespace SiteEngine.Entities;

public interface ISiteScopedEntity
{
	Guid SiteId { get; }
}
