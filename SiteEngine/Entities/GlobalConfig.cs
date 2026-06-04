namespace SiteEngine.Entities;

public class GlobalConfig
{
	public int Id { get; set; }
	public string RootDomain { get; set; } = string.Empty;
	public string PlatformDomain { get; set; } = string.Empty;
	public string SmtpHost { get; set; } = string.Empty;
	public int SmtpPort { get; set; } = 587;
	public string SmtpFromAddress { get; set; } = string.Empty;
	public string SmtpUsername { get; set; } = string.Empty;
	public string SmtpPassword { get; set; } = string.Empty;
	public bool UseSsl { get; set; } = true;
}
