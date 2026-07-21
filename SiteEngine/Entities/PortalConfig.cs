namespace SiteEngine.Entities;

public class PortalConfig
{
	public int Id { get; set; }

	// NEW — required for setup
	public string PortalName { get; set; } = string.Empty;
	public string PortalDomain { get; set; } = string.Empty;

	// Legacy SMTP fields (still used)
	public string SmtpHost { get; set; } = string.Empty;
	public int SmtpPort { get; set; } = 587;
	public string SmtpFromAddress { get; set; } = string.Empty;
	public string SmtpUsername { get; set; } = string.Empty;
	public string SmtpPassword { get; set; } = string.Empty;
	public bool UseSsl { get; set; } = true;
}
