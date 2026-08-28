using SiteEngine.Enums;

namespace SiteEngine.Entities;

// The ONLY mechanism for cross-organization access — see OrganizationFramework.md's
// "Parent-Child Access" and the Phase 1 "Resolved Decisions": there is no
// implicit privilege cascade. Holding an admin role on a parent Organization
// grants nothing on a child unless a grant row says so, explicitly, and a
// grant can name any two Organizations at any depth — it is not restricted
// to immediate parent/child pairs. ("Parent"/"Child" here names the intended
// direction of the grant, not a structural requirement that
// ParentOrganizationId literally be the child's ParentOrganizationId.)
//
// There is no "Disabled" AccessLevel — the absence of a row between two
// Organizations already means no access. Deleting a grant, not setting it to
// some Disabled value, is how access is revoked.
public class ParentAccessGrant
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public Guid ParentOrganizationId { get; set; }
	public Organization ParentOrganization { get; set; } = null!;

	public Guid ChildOrganizationId { get; set; }
	public Organization ChildOrganization { get; set; } = null!;

	public AccessLevel AccessLevel { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
