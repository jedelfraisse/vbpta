using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SiteEngine.Data;

namespace WebApp.Authentication;

public class SmtpEmailLoginSender(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailLoginOptions> options,
    ILogger<SmtpEmailLoginSender> logger) : IEmailLoginSender
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly EmailLoginOptions _options = options.Value;
    private readonly ILogger<SmtpEmailLoginSender> _logger = logger;

    public async Task SendCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var globalConfig = await dbContext.GlobalConfigs
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var smtpHost = globalConfig?.SmtpHost?.Trim() ?? _options.SmtpHost?.Trim() ?? string.Empty;
        var smtpPort = globalConfig?.SmtpPort ?? _options.SmtpPort;
        var smtpFromAddress = globalConfig?.SmtpFromAddress?.Trim() ?? string.Empty;
        var smtpUser = globalConfig?.SmtpUsername?.Trim() ?? _options.SmtpUser?.Trim() ?? string.Empty;
        var smtpPassword = globalConfig?.SmtpPassword?.Trim() ?? _options.SmtpPassword ?? string.Empty;
        var useSsl = globalConfig?.UseSsl ?? _options.UseSsl;

        var fromAddress = !string.IsNullOrWhiteSpace(smtpFromAddress)
            ? smtpFromAddress
            : (!string.IsNullOrWhiteSpace(_options.FromAddress)
                ? _options.FromAddress.Trim()
                : smtpUser);

        if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning(
                "Email login code for {Email}: {Code}. SMTP is not configured yet; using log-only delivery.",
                email,
                code);
            return;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromAddress),
            Subject = "Your VBPTA login code",
            Body = $"Your one-time login code is: {code}",
            IsBodyHtml = false
        };
        mailMessage.To.Add(email);

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = useSsl
        };

        if (!string.IsNullOrWhiteSpace(smtpUser))
        {
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await smtpClient.SendMailAsync(mailMessage);
    }
}
