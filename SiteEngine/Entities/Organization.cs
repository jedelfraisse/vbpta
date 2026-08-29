using SiteEngine.Enums;

namespace SiteEngine.Entities;

// The primary entity of the Organization Framework — see OrganizationFramework.md
// and OrganizationFramework-Phase1.md. An Organization is the community itself:
// identity, hierarchy placement, and (in a later phase) membership ownership.
// It always exists once a community is created, regardless of whether it has
// a public website.
//
// Organization is deliberately new rather than a rename of Site — see the
// Phase 1 "Resolved Decisions": Organization owns Site(s), not the other way
// around. Site keeps meaning exactly what it means today (hosted presence:
// subdomain/domain, branding, theme) and is untouched by this phase; an
// Organization optionally points at one via SiteId.
//
// Phase 1 does NOT re-point SiteUser/SiteUserRole/CustomRole/BoardPosition at
// Organization — those stay Site-scoped exactly as today. See Phase 1's
// "Membership Migration Is Analysis Only".
public class Organization
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public int OrganizationTypeId { get; set; }
	public OrganizationType OrganizationType { get; set; } = null!;

	// Must be a level belonging to OrganizationTypeId — enforced by
	// OrganizationService, not a DB constraint (EF can't express "this FK's
	// target must share another FK's value" declaratively).
	public Guid OrganizationLevelId { get; set; }
	public OrganizationLevel OrganizationLevel { get; set; } = null!;

	// Self-referencing hierarchy — null means this Organization has no parent
	// (either it's the root of its hierarchy, or it's fully independent; see
	// OrganizationFramework.md's "Organization Independence"). This is the
	// ONLY structural parent/child relationship — OrganizationLevel does not
	// participate in it, it only classifies where an Organization sits.
	public Guid? ParentOrganizationId { get; set; }
	public Organization? ParentOrganization { get; set; }
	public ICollection<Organization> ChildOrganizations { get; set; } = new List<Organization>();

	public string Name { get; set; } = string.Empty;

	// Public-facing summary — shown on the community detail page and (excerpted)
	// in the Community Directory listing. Added during OrganizationPublicExperience
	// Phase 1; the original Organization Framework had no need for this, but the
	// public experience plan calls for it as a core detail-page field.
	public string? Description { get; set; }

	// The Organization's own external website (e.g. a Givebacks page) — distinct
	// from Site.ExternalUrl, which is a Site-level field for the now-legacy
	// Site-based directory. See OrganizationPublicExperience-Phase1.md's
	// "Resolved Decisions": this is one of only two real new stored fields this
	// phase adds (the other is Visibility) — PresenceType itself is computed
	// from whether this and SiteId are set, never stored.
	public string? ExternalUrl { get; set; }

	// Whether this Organization appears in the public Community Directory —
	// independent of Site visibility/status. Defaults to Public: existing
	// (backfilled) Organizations are real, already-live communities, and a
	// newly created Organization shouldn't silently vanish from the directory
	// with no explanation of why. An admin who wants to stage one before
	// launch sets this to Pending explicitly.
	public OrganizationVisibility Visibility { get; set; } = OrganizationVisibility.Public;

	// External identifier value — e.g. a PTA's familiar "00000000" PTA ID #.
	// Whether this is used at all, and whether it's required, is a policy on
	// OrganizationType (OrganizationType.IdentifierRequirement), not fixed
	// here. Unique within its Organization Type when set — see AppDbContext's
	// filtered unique index — but not necessarily unique across Types, since
	// different Types may run entirely separate numbering schemes.
	public string? IdentifierValue { get; set; }

	// Optional 0-or-1 hosted presence. Null is normal and expected for a
	// non-site-eligible level (see OrganizationLevel.IsSiteEligible) — Site's
	// own schema is untouched, this is purely an Organization-side pointer.
	public Guid? SiteId { get; set; }
	public Site? Site { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
