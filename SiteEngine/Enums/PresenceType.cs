namespace SiteEngine.Enums;

// How an Organization presents itself publicly — see
// OrganizationPublicExperience.md's "Community Presence Model". Deliberately
// NOT a stored field: it's computed from Organization.SiteId/ExternalUrl
// (see OrganizationSummary.PresenceType) so it can never drift out of sync
// with the fields it describes. Kept as a real enum here (not an inline
// string) since it's a genuine, reusable domain concept — used by both the
// Community Directory listing and the community detail page.
public enum PresenceType
{
	Hosted,
	External,
	DirectoryOnly
}
