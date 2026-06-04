namespace WebApp.Authentication;

public interface IEmailLoginSender
{
	Task SendCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}
