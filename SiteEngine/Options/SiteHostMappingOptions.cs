namespace SiteEngine.Options;

public class SiteHostMappingOptions
{
	public const string SectionName = "SiteHostMapping";

	public Dictionary<string, string> Hosts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
