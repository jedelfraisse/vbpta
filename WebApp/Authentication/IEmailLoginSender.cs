namespace WebApp.Authentication;

public enum LoginEmailTemplate
{
	Welcome,
	WelcomeBack
}

public interface IEmailLoginSender
{
	Task SendLoginCodeAsync(string email, string code, LoginEmailTemplate template, CancellationToken cancellationToken = default);
}
