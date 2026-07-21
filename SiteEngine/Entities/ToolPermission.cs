namespace SiteEngine.Entities;

public class ToolPermission
{
    public int Id { get; set; }

    public int ToolId { get; set; }
    public PortalTools Tool { get; set; }

    public int UserId { get; set; }

    // Examples:
    // Event.View
    // Event.Create
    // Event.Note.View
    // Event.Note.Create
    // Event.Note.Admin
    public string PermissionKey { get; set; }

    public DateTime? ExpiresOn { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
