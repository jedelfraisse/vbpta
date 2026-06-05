namespace SiteEngine.Entities;

public class BlockResult
{
    public BlockType Type { get; set; }
    public BlockSize Size { get; set; }
    public string? Html { get; set; }
    public string? HelperName { get; set; }

    // NEW
    public string? Title { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}
