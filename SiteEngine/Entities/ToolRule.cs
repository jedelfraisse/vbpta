namespace SiteEngine.Entities;

public class ToolRule
{
    public int Id { get; set; }

    public int ToolId { get; set; }
    public PortalTools Tool { get; set; }

    public int? MaxSessionsPerDay { get; set; }
    public int? MaxSessionsPerMonth { get; set; }
    public int? MaxRequestsPerMinute { get; set; }

    public bool AllowExport { get; set; } = false;
    public bool AllowBatchProcessing { get; set; } = false;
    public bool AllowAdvancedMode { get; set; } = false;

    public DateTime? ExpiresOn { get; set; }
    public int Priority { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
