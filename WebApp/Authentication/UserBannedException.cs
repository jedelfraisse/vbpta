namespace WebApp.Authentication;

// Thrown by PasswordlessSignInService.RequestCodeAsync when the requested
// email is on the ban list. Deliberately a distinct type from
// InvalidOperationException (validation errors) — Login.razor needs to tell
// the two apart to redirect a banned visitor to /banned instead of showing
// the normal "check your email" message.
public class UserBannedException : Exception
{
	public UserBannedException()
		: base("This email address is not permitted to sign in.")
	{
	}
}
