using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WebApp.Authentication;

public class SmtpEmailLoginSender(
	IOptions<EmailLoginOptions> options,
	ILogger<SmtpEmailLoginSender> logger) : IEmailLoginSender
{
	private readonly EmailLoginOptions _options = options.Value;
	private readonly ILogger<SmtpEmailLoginSender> _logger = logger;

	public async Task SendCodeAsync(string email, string code, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_options.SmtpHost) || string.IsNullOrWhiteSpace(_options.FromAddress))
		{
			_logger.LogWarning(
				"Email login code for {Email}: {Code}. SMTP is not configured yet; using log-only delivery.",
				email,
				code);
			return;
		}

		using var mailMessage = new MailMessage
		{
			From = new MailAddress(_options.FromAddress),
			Subject = "Your VBPTA login code",
			Body = $"Your one-time login code is: {code}",
			IsBodyHtml = false
		};
		mailMessage.To.Add(email);

		using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
		{
			EnableSsl = _options.UseSsl
		};

		if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
		{
			smtpClient.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);
		}

		cancellationToken.ThrowIfCancellationRequested();
		await smtpClient.SendMailAsync(mailMessage);
	}
}
