namespace SiteEngine.Entities;

public class SitePage
{
    public int Id { get; set; }

    public Guid SiteId { get; set; }
    public string PageId { get; set; } = default!;   // "citywide-home", "about", etc.

    public string Text { get; set; } = default!;     // Your block markup

    public Site Site { get; set; } = default!;
}

