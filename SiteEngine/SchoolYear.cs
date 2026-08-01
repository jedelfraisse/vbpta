namespace SiteEngine;

// Membership and role records (CustomRole, SiteUserRole, BoardPosition) are all
// scoped to a "SchoolYear" string like "2026-2027". This is the single place
// that turns a date into that string, so every caller agrees on where the
// year boundary falls.
public static class SchoolYear
{
	// School years run July 1 – June 30.
	public static string Current(DateTimeOffset? asOf = null)
	{
		var now = asOf ?? DateTimeOffset.UtcNow;
		var startYear = now.Month >= 7 ? now.Year : now.Year - 1;
		return $"{startYear}-{startYear + 1}";
	}
}
