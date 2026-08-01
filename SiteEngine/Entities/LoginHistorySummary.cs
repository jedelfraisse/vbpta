namespace SiteEngine.Entities;

// Yearly rollup of a user's LoginHistory rows, one per (UserId, SchoolYear).
// Built by the placeholder cleanup routine in LoginTrackingService, which then
// deletes the detailed LoginHistory rows it summarized (kept for 3 school
// years). SiteId is the site the user logged into most often that year, not
// a scope key — UniqueSites is the distinct-site count across the whole year.
public class LoginHistorySummary
{
	public int Id { get; set; }

	public string UserId { get; set; } = string.Empty; // FK to AspNetUsers (ApplicationUser)

	public string SchoolYear { get; set; } = string.Empty; // e.g. "2026-2027" — see SchoolYear.Current()

	public Guid? SiteId { get; set; } // Most-frequently-logged-into site this year
	public Site? Site { get; set; }

	public int TotalLogins { get; set; }

	// Network-type breakdown — always 0 today; see LoginNetworkType.
	public int SchoolNetworkLogins { get; set; }
	public int HomeNetworkLogins { get; set; }
	public int MobileNetworkLogins { get; set; }

	public int UniqueSites { get; set; }

	public DateTimeOffset FirstLoginUtc { get; set; }
	public DateTimeOffset LastLoginUtc { get; set; }
}
