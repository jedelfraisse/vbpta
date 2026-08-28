namespace SiteEngine.Entities;

// A concrete, dated operating period — "2026-2027" (School Year), "Summer
// 2026" (League Session), "FY2026" (Fiscal Year). See OrganizationFramework.md's
// "Operational Cycles": the platform must not assume every organization runs
// on a school year.
//
// Structured per the parent doc's decision (StartDate/EndDate/DisplayLabel/
// Type as real fields, not a free-text label like today's SchoolYear string)
// so date-range queries and cross-organization rollups work regardless of
// naming convention.
//
// Scoped to OrganizationType, not to an individual Organization — see
// "Cycle Ownership: OrganizationType, not Organization" in
// md/OrganizationFramework-Phase1-Notes.md for the reasoning. In short: every
// PTA organization shares the same School Year boundaries, so cycle
// instances belong once to the "PTA" type rather than being duplicated (or
// re-inherited down a chain) per Organization. Per-Organization override is
// deferred — nothing consumes OperationalCycle yet (membership migration is
// analysis-only this phase), so there's no real requirement to build it now.
public class OperationalCycle
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public int OrganizationTypeId { get; set; }
	public OrganizationType OrganizationType { get; set; } = null!;

	// "School Year", "League Session", "Calendar Year", "Fiscal Year", ... —
	// free text, not an enum. New Organization Types will invent their own.
	public string CycleTypeName { get; set; } = string.Empty;

	// "2026-2027", "Summer 2026", "FY2026" — for display only. StartDate/EndDate
	// are what queries and rollups actually use.
	public string DisplayLabel { get; set; } = string.Empty;

	public DateTimeOffset StartDate { get; set; }
	public DateTimeOffset EndDate { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
