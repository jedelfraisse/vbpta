using Microsoft.AspNetCore.Identity;
using SiteEngine.Entities;

namespace SiteEngine.Identity;

public class ApplicationUser : IdentityUser
{
	// True from account creation (first code request) until the first
	// successful SignInWithCodeAsync — drives which login email template
	// is sent ("Welcome" vs "Welcome back").
	public bool IsFirstLogin { get; set; } = true;

	// Login analytics — maintained by LoginTrackingService on every
	// successful sign-in. See LoginHistory for the per-event log these
	// summarize.
	public DateTimeOffset? FirstLoginUtc { get; set; }
	public DateTimeOffset? LastLoginUtc { get; set; }
	public Guid? LastLoginSiteId { get; set; }
	public int LoginCount { get; set; }
}
