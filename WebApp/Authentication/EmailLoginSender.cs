using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
using System.Net;
using System.Net.Mail;
using WebApp.Services;

namespace WebApp.Authentication;

public class EmailLoginSender : IEmailLoginSender
{
	private readonly IDbContextFactory<AppDbContext> _dbFactory;
	private readonly IDataProtector _protector;

	public EmailLoginSender(IDbContextFactory<AppDbContext> dbFactory, IDataProtectionProvider provider)
	{
		_dbFactory = dbFactory;
		_protector = provider.CreateProtector("PortalConfig.SmtpPassword");
	}

	public async Task SendLoginCodeAsync(string email, string code, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		// Load SMTP from PortalConfig
		var cfg = await db.PortalConfigs
			.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId, cancellationToken);

		if (cfg == null)
			throw new InvalidOperationException("PortalConfig not found. SMTP settings are not available.");

		// Decrypt password
		var smtpPassword = _protector.Unprotect(cfg.SmtpPassword);

		using var client = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
		{
			EnableSsl = cfg.UseSsl,
			Credentials = new NetworkCredential(cfg.SmtpUsername, smtpPassword)
		};

		var message = new MailMessage(cfg.SmtpFromAddress, email)
		{
			Subject = "Your PTA Portal Login Code",
			Body = $"Your login code is: {code}\n\n" +
				$"This code will expire in {PasswordlessCodeStore.Lifetime.TotalMinutes:0} minutes. " +
				"If you didn't request this code, you can safely ignore this email."
		};

		await client.SendMailAsync(message, cancellationToken);
	}
}
