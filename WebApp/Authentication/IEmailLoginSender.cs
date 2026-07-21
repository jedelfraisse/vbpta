namespace WebApp.Authentication;

public interface IEmailLoginSender
{
	Task SendLoginCodeAsync(string email, string code, CancellationToken cancellationToken = default);
}
