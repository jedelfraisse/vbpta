using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebApp.Services;

// Centralizes every outbound SMTP send in the app (login codes, setup-wizard
// tests, Global Admin's test email) behind MailKit instead of the legacy
// System.Net.Mail.SmtpClient. That legacy client has a known parser gap
// against servers that advertise the older "250-AUTH=..." (equals-sign)
// EHLO extension syntax alongside the modern "250-AUTH ..." line — smtp4dev
// does exactly this for old-client compatibility — and reports the whole
// EHLO response as invalid even though the server is perfectly real and
// RFC-valid. MailKit parses it correctly. See EmailLoginSender and
// SetupService for the callers this replaced.
public static class SmtpMailSender
{
	// A login/test email has no legitimate reason to take anywhere near
	// MailKit's own default connect/send timeouts; fail fast against a
	// genuinely misconfigured host instead of leaving a caller (e.g.
	// Login.razor's button) waiting. 30s (not less) specifically because a
	// dev-time SMTP relay hosted on something like a Fly.io machine with
	// auto-stop enabled can take several seconds to wake from idle on the
	// first request after a gap — the TCP connect succeeds instantly via the
	// platform's edge proxy well before the app itself is actually ready to
	// speak SMTP, so a too-short timeout reads as a hard failure when it's
	// really just a cold start.
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	public static async Task SendAsync(
		string host, int port, bool useSsl, string username, string password,
		string fromAddress, string? replyToAddress, IEnumerable<string> toAddresses,
		string subject, string body, CancellationToken cancellationToken = default)
	{
		var message = new MimeMessage();
		message.From.Add(MailboxAddress.Parse(fromAddress));

		foreach (var to in toAddresses)
			message.To.Add(MailboxAddress.Parse(to));

		if (!string.IsNullOrWhiteSpace(replyToAddress))
			message.ReplyTo.Add(MailboxAddress.Parse(replyToAddress));

		message.Subject = subject;
		message.Body = new TextPart("plain") { Text = body };

		using var client = new SmtpClient { Timeout = (int)Timeout.TotalMilliseconds };

		// Auto/None mirrors what System.Net.Mail.SmtpClient.EnableSsl meant:
		// true negotiated TLS (STARTTLS — that client never supported implicit
		// TLS on connect), false meant plaintext only, never negotiate.
		var socketOptions = useSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
		await client.ConnectAsync(host, port, socketOptions, cancellationToken);

		if (!string.IsNullOrEmpty(username))
			await client.AuthenticateAsync(username, password, cancellationToken);

		await client.SendAsync(message, cancellationToken);
		await client.DisconnectAsync(true, cancellationToken);
	}
}
