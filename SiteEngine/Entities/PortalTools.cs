namespace SiteEngine.Entities;

public class PortalTools
{
    public int Id { get; set; }

    public string ToolName { get; set; }
    public string ToolDescription { get; set; }
    public string PageURL { get; set; }

    // Public, Division, LocalUnit
    public string ToolScope { get; set; } = "Public";

    public string Category { get; set; }
    public string IconClass { get; set; }
    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;
    public string Version { get; set; }
    public string InternalNotes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
