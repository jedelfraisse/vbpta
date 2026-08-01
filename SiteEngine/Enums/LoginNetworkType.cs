namespace SiteEngine.Enums;

// Classifies a LoginHistory event by the network it came from, so
// LoginHistorySummary can break down yearly logins by network. Nothing
// currently sets this to anything but Unknown — IP-to-network classification
// (e.g. matching a site's known school IP ranges, or a mobile-carrier lookup)
// is not implemented yet.
public enum LoginNetworkType
{
	Unknown = 0,
	School = 1,
	Home = 2,
	Mobile = 3
}
