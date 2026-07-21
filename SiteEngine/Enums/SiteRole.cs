public enum SiteRole
{
	// Base roles
	Viewer,
	Member,
	Officer,

	// Site-level admin (Portal, Division, Unit)
	SiteAdmin,

	// Division-level admin (Division + Units)
	DivisionAdmin,

	// Global super admin
	SuperAdmin
}
