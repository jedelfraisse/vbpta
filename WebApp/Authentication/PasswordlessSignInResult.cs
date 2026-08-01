using SiteEngine.Enums;

namespace WebApp.Authentication;

public record PasswordlessSignInResult(
	bool Succeeded,
	bool RequiresProfileCompletion,
	SiteRole? MembershipRole = null)
{
	public static readonly PasswordlessSignInResult Failed = new(Succeeded: false, RequiresProfileCompletion: false);
}
