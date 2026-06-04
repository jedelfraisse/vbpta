namespace WebApp.Authentication;

public class EmailLoginOptions
{
	public const string SectionName = "EmailLogin";

	public string? FromAddress { get; set; }
	public string? SmtpHost { get; set; }
	public int SmtpPort { get; set; } = 587;
	public string? SmtpUser { get; set; }
	public string? SmtpPassword { get; set; }
	public bool UseSsl { get; set; } = true;
}
