namespace SiteEngine.Enums;

// Cross-organization access, granted explicitly by a ParentAccessGrant row —
// see OrganizationFramework.md's "Parent-Child Access" and Phase1's "Support
// Explicit Parent Access". There is deliberately no "Disabled" member: the
// absence of a ParentAccessGrant row between two Organizations already means
// no access, so a fourth enum value would just be a stored no-op — see
// ParentAccessGrant's own doc comment.
public enum AccessLevel
{
	View,
	Participation,
	Administrative
}
