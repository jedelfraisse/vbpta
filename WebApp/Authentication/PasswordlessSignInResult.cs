using SiteEngine.Enums;

namespace WebApp.Authentication;

public record PasswordlessSignInResult(
	bool Succeeded,
	bool RequiresProfileCompletion,
	SiteRole? MembershipRole = null,
	bool Banned = false)
{
	public static readonly PasswordlessSignInResult Failed = new(Succeeded: false, RequiresProfileCompletion: false);
	public static readonly PasswordlessSignInResult BannedResult = new(Succeeded: false, RequiresProfileCompletion: false, Banned: true);
}
