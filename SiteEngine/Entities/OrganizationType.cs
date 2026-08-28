using SiteEngine.Enums;

namespace SiteEngine.Entities;

// A kind of group the portal supports — PTA today, Boy Scouts/Girl Scouts/
// booster clubs/etc. later (see CLAUDE.md section 0: the portal is meant to
// be generic, not PTA-specific). Backs the public "who this portal serves"
// page and PortalHome's teaser section — both read this list instead of
// hardcoding prose, so adding a new group later is just a new row here, no
// code change. Deliberately minimal for now (no rules/tasks/branding link
// yet) — those are real future work once there's more than one group to
// actually need per-type behavior for.
public class OrganizationType
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	// FontAwesome class (e.g. "fa-solid fa-people-group"), matching
	// PortalTools.IconClass's convention. Optional — a null/blank value just
	// means the card renders without an icon.
	public string? IconClass { get; set; }

	public int SortOrder { get; set; }

	// Whether Organizations of this Type carry an external identifier value
	// (Organization.IdentifierValue) — off by default, since most Organization
	// Types won't need one. See IdentifierRequirement.
	public IdentifierRequirement IdentifierRequirement { get; set; } = IdentifierRequirement.NotUsed;

	// Display label for the identifier field — "PTA ID #" for PTA, a
	// different type might call it "Member Number" or "Charter #". Null
	// falls back to a generic "Identifier" label in the UI. Meaningless when
	// IdentifierRequirement is NotUsed.
	public string? IdentifierLabel { get; set; }
}
