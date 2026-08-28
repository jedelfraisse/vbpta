using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SiteEngine.Data;
using SiteEngine.Entities;
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

	public async Task SendLoginCodeAsync(string email, string code, LoginEmailTemplate template, CancellationToken cancellationToken = default)
	{
		await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

		// Load SMTP from PortalConfig
		var cfg = await db.PortalConfigs
			.FirstOrDefaultAsync(c => c.Id == SeedData.DefaultGlobalConfigId, cancellationToken);

		if (cfg == null)
			throw new InvalidOperationException("PortalConfig not found. SMTP settings are not available.");

		// Decrypt password
		var smtpPassword = _protector.Unprotect(cfg.SmtpPassword);

		var (subject, intro) = template switch
		{
			LoginEmailTemplate.Welcome => ("Welcome — here's your sign-in code", "Welcome! We're glad you're here."),
			LoginEmailTemplate.WelcomeBack => ("Welcome back — here's your sign-in code", "Welcome back."),
			_ => throw new ArgumentOutOfRangeException(nameof(template), template, null)
		};

		var body = $"{intro}\n\n" +
			$"Your login code is: {code}\n\n" +
			$"This code will expire in {PasswordlessCodeStore.Lifetime.TotalMinutes:0} minutes. " +
			"If you didn't request this code, you can safely ignore this email.";

		await SmtpMailSender.SendAsync(
			cfg.SmtpHost, cfg.SmtpPort, cfg.UseSsl, cfg.SmtpUsername, smtpPassword,
			cfg.SmtpFromAddress, replyToAddress: null, toAddresses: [email],
			subject, body, cancellationToken);
	}
}
