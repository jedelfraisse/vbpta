using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteEngine.Enums;

public enum SiteStatus
{
	Pending,
	MembersOnly,
	Active,
	Inactive,
	Archived,

	// Publicly listed in the directory, but not using the hosted portal —
	// the site shows its basic info (and ExternalUrl, if set) instead of
	// redirecting to a hosted site. Appended rather than inserted so the
	// existing enum's underlying int values (and anything already stored
	// in the database) don't shift.
	ActiveListed,

	// Distinct from Inactive: a Disabled site is deliberately pulled from
	// the directory entirely (same as Archived), whereas Inactive sites are
	// just not currently live but may still surface elsewhere in admin UI.
	Disabled
}

