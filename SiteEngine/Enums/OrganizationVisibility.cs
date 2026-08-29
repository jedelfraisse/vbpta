namespace SiteEngine.Enums;

// Whether an Organization appears in the public Community Directory — see
// OrganizationPublicExperience.md's "Public Visibility". Deliberately
// independent of Site visibility/status: an Organization can be Public in
// the directory while its linked Site is MembersOnly (the directory entry
// and detail page show, but the hosted site itself stays gated), and vice
// versa. Defaults to Public — see OrganizationService for why.
public enum OrganizationVisibility
{
	Public,
	Pending,
	Private,
	Archived
}
