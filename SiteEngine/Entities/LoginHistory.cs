using SiteEngine.Enums;

namespace SiteEngine.Entities;

// One row per successful login. LoginTrackingService.RecordLoginAsync inserts
// these; the yearly cleanup routine rolls them up into LoginHistorySummary and
// prunes anything older than 3 school years (see LoginTrackingService).
public class LoginHistory
{
	public int Id { get; set; }

	public string UserId { get; set; } = string.Empty; // FK to AspNetUsers (ApplicationUser)

	public DateTimeOffset LoginUtc { get; set; }

	public Guid? SiteId { get; set; } // FK to Sites — null if site resolution failed at login time
	public Site? Site { get; set; }

	public string IpAddress { get; set; } = string.Empty;

	// Not populated by any classifier yet — see LoginNetworkType.
	public LoginNetworkType NetworkType { get; set; } = LoginNetworkType.Unknown;
}
