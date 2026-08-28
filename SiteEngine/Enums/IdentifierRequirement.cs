namespace SiteEngine.Enums;

// Whether Organizations of a given OrganizationType carry an external
// identifier value (Organization.IdentifierValue) — e.g. PTA's familiar
// 8-digit "00000000" PTA ID #, or some other org type's own numbering
// scheme. A policy on the Type, not a fixed format: different Organization
// Types can use entirely different identifier shapes, or none at all.
public enum IdentifierRequirement
{
	NotUsed,
	Optional,
	Required
}
