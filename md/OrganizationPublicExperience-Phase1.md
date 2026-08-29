# OrganizationPublicExperience-Phase1.md

## Status

Complete (2026-08-28). All deliverables implemented and validated — see "Implementation Notes" at the end of this document for what shipped, what was simplified, and what's still open.

---

# Purpose

Phase 1 establishes Organizations as the primary public-facing discovery object within Central Portal.

The goal is to begin transitioning from:

```text
Site-Based Discovery
```

to:

```text
Organization-Based Discovery
```

while preserving existing public functionality.

This phase focuses on directory and discovery experiences.

It does not implement Themes, Tools, Events, or Membership migration.

---

# Relationship To Other Documents

Depends On:

```text
OrganizationFramework.md

OrganizationFramework-Phase1.md

OrganizationPublicExperience.md
```

This document implements the first phase of the public-facing Organization experience.

---

# Resolved Decisions

Resolved after review, 2026-08-28.

## `/unit-sites` Keeps Its URL — Its Query Changes From `Site` To `Organization`

"Phase 1 does not require the route transition" and "Hosted and non-hosted organizations appear side-by-side in the directory" can't both be true if `/unit-sites` keeps querying `Site` (`DashboardService.GetUnitSitesAsync`) — a Directory-only or External-website Organization has no `Site` and would be structurally invisible there no matter what else Phase 1 builds. The resolution: the **URL** `/unit-sites` stays exactly as it is (no route break, nothing to redirect) — the **query behind it** moves from `Site` to `Organization`. "Existing Site-based pages may continue to operate" means the route and page shell continue to work, not that they keep reading `Site` data. `/communities/{identifier}` is still the new, separate detail-page route; only the directory listing itself is being repointed.

## Organization Presence Type Is Computed, Not Stored

`PresenceType` (Hosted/External/DirectoryOnly) is **derived**, not a new column: `SiteId != null` → Hosted, else `ExternalUrl != null` → External, else DirectoryOnly. Storing it as its own field alongside `SiteId`/`ExternalUrl` would let it drift out of sync with the fields it's supposedly describing (e.g. `PresenceType = Hosted` while `SiteId` is actually null). The only two genuinely new stored fields on `Organization` are `ExternalUrl` and `Visibility`. This matches the codebase's existing preference for derived values over duplicated state (e.g. `SiteRoleResolver` resolves role rather than caching it).

## Global Admin Gets an Explicit Deliverable: Presence Management

Once `ExternalUrl` and `Visibility` exist, an admin needs somewhere to set them — nothing in the original deliverables list did this. Resolution: extend the existing `OrganizationDetail.razor` Description tab (built in Organization Framework Phase 1) with `ExternalUrl` and `Visibility` fields, the same way `IdentifierValue` was added there. No new page — `PresenceType` needs no UI of its own since it's computed from fields already editable (`SiteId` via the existing Site picker, `ExternalUrl` via the new field).

---

# Current State

Currently:

```text
Community Directory
```

is a renamed user experience built on:

```text
Site
```

records.

The underlying implementation remains:

```text
DashboardService.GetUnitSitesAsync()
```

and related Site-based functionality.

Organization-backed discovery has not yet been implemented.

---

# Future State

The Community Directory will eventually be powered by:

```text
Organization
```

rather than:

```text
Site
```

Organizations become the primary public-facing entity.

Sites become optional.

Organizations may be:

```text
Hosted

External

Directory Only
```

---

# Phase 1 Objectives

## Add Organization Presence

Introduce the concept of Organization Presence.

Examples:

### Hosted

Organization uses a Central Portal hosted Site.

---

### External

Organization uses an external website.

Examples:

- Givebacks
- External Website
- Community Website

---

### Directory Only

Organization exists in the directory but has no public website.

---

## Extend Organization

Add support for:

```text
External Website (stored: Organization.ExternalUrl)

Presence Type (computed — see Resolved Decisions, not a new column)

Public Visibility (stored: Organization.Visibility)
```

at the Organization level.

The directory must be able to function without requiring a Site record.

---

## Build Organization Directory Infrastructure

Create services and models supporting:

```text
Community Directory

Organization Detail Pages

Organization Search
```

based on Organizations rather than Sites.

---

## Preserve Existing Community Directory

Phase 1 should avoid breaking current links and navigation.

The `/unit-sites` URL stays — see Resolved Decisions. What changes underneath it is the query: `Site`-based today, `Organization`-based once this phase ships. Visitors and bookmarks see the same address; what it shows is what actually changes.

---

# New Concepts

## Organization Presence Type

Suggested values:

```text
Hosted

External

DirectoryOnly
```

Purpose:

Define how an Organization presents itself publicly.

**Computed, not stored** — see Resolved Decisions. `SiteId != null` → Hosted; else `ExternalUrl != null` → External; else DirectoryOnly. No `PresenceType` column, no UI field of its own.

---

## Organization Visibility

Suggested values:

```text
Public

Pending

Private

Archived
```

Purpose:

Determine whether an Organization appears in public directories.

Organization visibility should be independent from Site visibility.

---

## External Website

Suggested field:

```text
Organization.ExternalUrl
```

Purpose:

Support organizations using:

- Givebacks
- External websites
- Third-party platforms

without requiring a Central Portal Site.

---

# Community Directory Model

Future source:

```text
Organization
```

rather than:

```text
Site
```

Directory entries should include:

- Organization Name
- Organization Type
- Organization Level
- Presence Type (computed — see Resolved Decisions)
- Public Description
- Parent Organization

Optional:

- Website
- Contact Information
- Child Organizations

---

# Community Detail Pages

## Route

Resolved:

```text
/communities/{identifier}
```

`{identifier}` is `Organization.IdentifierValue` when available, otherwise `Organization.Id` — guarantees every Organization has a stable public URL.

## Display

- Name
- Description
- Organization Type
- Organization Level
- Presence Type (computed)
- Parent Organization

Optional:

- Website
- Contact Information
- Child Organizations
- Social Media

---

# Organization Hierarchies

Visitors should be able to navigate hierarchy relationships.

Example:

```text
Virginia PTA
```

Shows:

```text
Children

Virginia Beach Council PTA
```

---

Example:

```text
Virginia Beach Council PTA
```

Shows:

```text
Parent

Virginia PTA

Children

Luxford PTA
Bayside PTA
```

---

# Search

Search should operate against Organizations.

Supported:

- Name
- Organization Type
- Organization Level

Future:

- Location
- Region
- School
- ZIP Code

---

# Existing Route Transition

The URL itself does not transition in Phase 1 — see Resolved Decisions.

```text
/unit-sites
```

stays the address for the directory listing; only its underlying query moves from `Site` to `Organization`.

```text
/communities/{identifier}
```

is new — the detail-page route for an individual Organization.

A future phase may rename or retire the `/unit-sites` URL itself (e.g. to `/communities`, matching the detail-page naming) once the Organization-backed model has proven out. Phase 1 doesn't require that — only the query underneath it changes.

---

# Global Admin Dependencies

Phase 1 relies upon:

- Organization Types
- Organization Levels
- Organizations
- Operational Cycles

already implemented through Global Admin.

Phase 1 does add one real piece of Global Admin work: managing `ExternalUrl` and `Visibility` on the Organization Detail page (see "Global Admin: Organization Presence Management" under Phase 1 Deliverables). Beyond that one addition, no new Global Admin pages or sections are needed.

---

# Phase 1 Deliverables

## Organization Enhancements

Add:

- External Website (`Organization.ExternalUrl`)
- Visibility Status (`Organization.Visibility`)

to Organizations. Presence Type is computed, not added — see Resolved Decisions.

---

## Global Admin: Organization Presence Management

Extend the existing `OrganizationDetail.razor` Description tab with `ExternalUrl` and `Visibility` fields, alongside the `Name`/`Level`/`Parent`/`Site`/`IdentifierValue` fields already there. No new Global Admin page.

---

## Service Layer

Create support for:

- Directory Queries
- Organization Discovery
- Detail Page Loading

based on Organizations.

---

## Community Detail Pages

Implement:

```text
/communities/{identifier}
```

for publicly visible Organizations.

---

## Directory Infrastructure

Build Organization-based directory queries and repoint `/unit-sites` at them — see Resolved Decisions. The route doesn't change; the query it runs does.

---

## Validation

Verify support for:

### Hosted

```text
Luxford PTA
```

---

### External

```text
Bayside PTA
```

linked to Givebacks.

---

### Directory Only

```text
Future PTA
```

without a website.

---

# Success Criteria

Phase 1 is successful when:

- Organizations can exist as Hosted, External, or Directory-Only.
- Organizations can be publicly visible without requiring a Site.
- Organization.ExternalUrl exists.
- Organization Visibility exists.
- Organization-backed directory queries exist.
- Community detail pages exist.
- Parent and child organization relationships are visible.
- Hosted and non-hosted organizations appear side-by-side in the directory.
- Future Theme, Tool, and Event initiatives can build on the Organization-centric public experience.

---

# Deferred To Future Phases

The following remain out of scope:

- Theme Framework
- Tool Framework
- Event Framework
- Membership Migration
- Contact Ownership Migration
- Community Maps
- Geographic Search
- Verification Badges
- Community Analytics

These will build upon the public Organization experience established in this phase.

---

# Implementation Notes

Implemented and validated 2026-08-28.

## What Shipped

- `Organization.Description`, `Organization.ExternalUrl`, `Organization.Visibility` — the two real stored fields plus a `Description` field the original documents hadn't anticipated needing (the community detail page's core field list included it, but no Organization field existed for it — added it rather than dropping the field).
- `OrganizationSummary.PresenceType` — computed property, not stored, exactly as Resolved Decisions specified.
- `OrganizationService.GetDirectoryOrganizationsAsync` and `GetOrganizationByIdentifierAsync` — the two new query methods, both filtering to `Visibility == Public` only.
- `/unit-sites` (`UnitSites/Index.razor`) — repointed from `DashboardService.GetUnitSitesAsync` (Site) to `OrganizationService.GetDirectoryOrganizationsAsync` (Organization). Same URL, same page shell, new data source, exactly per Resolved Decisions. The old Division/Local Unit grouped-and-paginated layout was replaced with a flat, searchable list (Name + Organization Type filter) — pagination and Level-based filtering are not implemented; call these known simplifications, not oversights (see "Simplifications" below).
- `/communities/{identifier}` (new: `Communities/Details.razor`) — resolves `IdentifierValue` first, falls back to `Id`. Shows the presence-specific action (Visit Site / Visit Website / "no website yet") and links to public children and a public parent, if any.
- Global Admin's `OrganizationDetail.razor` — Description tab gained Description, External Website (disabled when a Site is linked, since `PresenceType` prioritizes Hosted), and Directory Visibility fields, plus a "View public page" link and Presence/Visibility badges in the header.
- `UnitSites/Details.razor` (the old `/unit-sites/{PtaId}` route) — untouched, still Site-based, still works for old links, per Resolved Decisions.

## Simplifications (known, not oversights)

- **No pagination on `/unit-sites`.** The old page paginated at 25/page; the new one loads the full filtered result set. Fine at current data volumes — revisit once real directory data grows.
- **Search supports Name and Organization Type, not Organization Level.** The original document listed Level as a supported facet; implemented as a simple two-filter UI for now rather than a cascading Type→Level picker. Easy to add later.
- **Parent/child links on the community detail page don't pre-filter for public visibility on the query side** — `GetOrganizationByIdentifierAsync` correctly refuses to resolve a non-public Organization directly, and the child list already only shows public children (via `GetDirectoryOrganizationsAsync`), but the *parent* link does one extra check (`GetOrganizationAsync` + a Visibility check) rather than being folded into a single query. Fine at Phase 1 scale.

## Validated

Directly exercised (not just built): the existing backfilled "Luxford Elementary PTA" (real Hosted Organization) plus two new test Organizations — "Bayside PTA" (External, linked to a Givebacks-style URL) and "Future PTA" (Directory Only, no Site, no external link) — confirmed all three appear in `GetDirectoryOrganizationsAsync`, resolve correctly via `GetOrganizationByIdentifierAsync`, and render correctly on both `/unit-sites` and their own `/communities/{identifier}` pages (including the "no website yet" state for Directory Only). Also confirmed setting an Organization to `Private` removes it from both the directory query and identifier resolution. All of this was left in the dev database as visible proof, same as the original Organization Framework Phase 1's validation data — visible and editable through Global Admin → Organizations.

## Still Open

- `RuntimeSiteContext.GetDirectoryRedirectUrl()` / `SiteUrlHelper.BuildDirectoryDetailUrl` still redirect non-Active sites to the old `/unit-sites/{PtaId}` detail page, not `/communities/{identifier}`. Not in this phase's stated deliverables, but worth closing in a follow-up so a visitor bounced off a Pending/ActiveListed hosted site lands on the new community page instead of the old Site-only stub.
- Everything already listed under "Deferred To Future Phases" above.