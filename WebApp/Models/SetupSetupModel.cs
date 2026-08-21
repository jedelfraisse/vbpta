namespace WebApp.Models;

public class SetupSetupModel
{
	// Step 1 — SQL
	public string SqlServer { get; set; } = "";
	public string DatabaseName { get; set; } = "";
	public string SqlUsername { get; set; } = "";
	public string SqlPassword { get; set; } = "";

	// Step 2 — SMTP
	public string SmtpHost { get; set; } = "";
	public int SmtpPort { get; set; }
	public string SmtpUsername { get; set; } = "";
	public string SmtpPassword { get; set; } = "";
	public string SmtpFrom { get; set; } = "";
	public string SmtpReplyTo { get; set; } = "";
	public bool SmtpUseSsl { get; set; }

	// Step 3 — Admin
	public string AdminEmail { get; set; } = "";
	public string AdminName { get; set; } = "";

	// Step 4 — Portal Info
	public string PortalName { get; set; } = "";
	public string PortalDomain { get; set; } = "";
}
