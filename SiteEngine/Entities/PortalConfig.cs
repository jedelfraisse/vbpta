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

	// Reply-To for outbound mail (login codes, admin test emails, etc). Optional —
	// when blank, senders fall back to SmtpFromAddress.
	public string SmtpReplyToAddress { get; set; } = string.Empty;

	// The one global logo template (a real logo graphic, e.g. exported from
	// Canva/PPT/Word) that PtaLogoGenerationService stamps a site's name onto,
	// in place of the code-drawn placeholder badge. Null/empty means no
	// template has been configured yet — generation falls back to the badge.
	// Box coordinates are percentages (0-100) of the template image's own
	// width/height, not pixels, so they stay correct regardless of what size
	// the template was exported at. Set once in Global Admin and reused for
	// every site's logo — see LogoTemplateService.
	public string? LogoTemplateUrl { get; set; }
	public double LogoTemplateBoxXPct { get; set; }
	public double LogoTemplateBoxYPct { get; set; }
	public double LogoTemplateBoxWidthPct { get; set; } = 50;
	public double LogoTemplateBoxHeightPct { get; set; } = 20;
	public string LogoTemplateFontFamily { get; set; } = "Arial Black";
	public string LogoTemplateFontColor { get; set; } = "#000000";
	public string LogoTemplateTextAlign { get; set; } = "Center";
}
