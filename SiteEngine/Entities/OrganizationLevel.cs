namespace SiteEngine.Entities;

// A position within an Organization Type's hierarchy — "National", "Council",
// "Unit" for PTA; "Network", "League", "Team" for a billiards community. See
// OrganizationFramework.md's "Organizational Levels": levels are data-driven,
// not a fixed enum like SiteType, and different Organization Types define
// entirely different level sets/depths.
//
// This is a classification an Organization instance references — not a layer
// Organizations live "under" in the ownership chain. The actual parent/child
// structure among Organizations is Organization.ParentOrganizationId,
// self-referencing; OrganizationLevel just labels where in that structure a
// given Organization sits (see OrganizationFramework-Phase1.md's "Future
// Relationship Model").
public class OrganizationLevel
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public int OrganizationTypeId { get; set; }
	public OrganizationType OrganizationType { get; set; } = null!;

	public string Name { get; set; } = string.Empty;

	// 1 = top of this Organization Type's hierarchy (e.g. "National"),
	// increasing downward. Used to order levels for display and to sanity-check
	// that a parent/child Organization pair is placed at levels that make
	// sense relative to each other — see OrganizationService.
	public int Rank { get; set; }

	// Whether an Organization placed at this level is allowed to have a
	// hosted Site (its own subdomain/branding) — see OrganizationFramework.md's
	// "Not Every Level Requires a Website". A billiards "Team" or a PTA
	// "Region" might have members and roles with no public site at all;
	// whether that's true is a per-level admin decision, not an assumption
	// baked into the framework.
	public bool IsSiteEligible { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
