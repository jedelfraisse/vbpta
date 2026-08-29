# OrganizationPublicExperience.md

## Status

Draft

---

# Purpose

This document defines how Organizations are presented to visitors and community members through the public-facing Central Portal experience.

The Organization Framework established:

- Organization Types
- Organizations
- Organization Levels
- Operational Cycles
- Parent Access Grants

This document defines how those concepts become visible and discoverable to the public.

---

# Current State vs. Future State

**Update 2026-08-28: Phase 1 is complete** — see [OrganizationPublicExperience-Phase1.md](OrganizationPublicExperience-Phase1.md), Status: Complete. The section below is left as originally written (pre-Phase-1) for history; here's what actually changed:

**Done:**
- Navigation labels: "Unit Sites" → "Community Directory"; the "Who We Serve" nav item removed entirely; the "Who We Serve" page → "Organization Types".
- Home page wording no longer implies every Organization is hosted by Central Portal.
- The Community Directory (`/unit-sites`) now reads `Organization` data, not `Site` — same URL, new query. It distinguishes Hosted/External/Directory-only and includes Organizations without a Site.
- `/communities/{identifier}` detail pages exist.
- `Organization.Visibility` (Public/Pending/Private/Archived) and `Organization.ExternalUrl` both exist and are enforced — only `Public` Organizations appear in the directory or resolve on their detail page.
- `Organization.Description` was added too — a field this document's "Organization Details" list called for that didn't exist until Phase 1 added it.

**Not yet done** (see Phase 1's own "Still Open" and "Deferred To Future Phases"): pagination and Level-based search on the directory, the old Site-based directory-redirect logic still pointing at `/unit-sites/{PtaId}` instead of `/communities/{identifier}`, and everything under Theme/Tool/Event Framework, Membership Migration, and the various "Future Enhancements" below.

---

*Original (pre-Phase-1) framing, kept for history:*

This document describes the target end-state. As of 2026-08-26, only the surface had moved — the underlying data had not.

---

# Resolved Decisions

Resolved after review, 2026-08-28.

## Organization Detail Route: `/communities/{identifier}`, Not `/organizations/{identifier}`

`/organizations` already exists — it's the Organization Types page (a taxonomy list), not a community. Nesting community detail pages under it (`/organizations/{identifier}`) would put "browse types" and "view one community" in a confusing parent/child relationship right at the URL level. Community detail pages get their own top-level route: `/communities/{identifier}`.

## Identifier Fallback: `IdentifierValue`, Else `Id`

`Organization.IdentifierValue` is optional — `OrganizationType.IdentifierRequirement` can be `NotUsed`, and even where it's `Optional` a given Organization may never have set one. The `{identifier}` route segment is `IdentifierValue` when the Organization has one, and falls back to `Organization.Id` (a plain Guid) when it doesn't. No new schema required — this only decides how the detail page resolves its route parameter.

## Contact Information Moves to "Optional", Sourced From Site When One Exists

Contact/primary-contact data today lives entirely on `SiteUserRole.IsPrimaryContact`, which is Site-scoped — Phase 1 deliberately didn't migrate membership to Organization. A Directory-only Organization (no Site) has no membership rows and therefore no contact record to show. "Contact Information" moves from the core detail-page fields to the Optional list (see "Organization Details" below), shown only when the Organization has a linked Site with a primary contact set. A dedicated Organization-level contact field is a reasonable future addition if Directory-only listings need their own — that's new schema this document isn't deciding now.

## `/unit-sites` Is Retired, Not Preserved, Once The Real Directory Ships

Once the Organization-backed `/communities` directory exists and is the primary discovery experience, `/unit-sites` and `/unit-sites/{PtaId}` go away rather than staying as a parallel, Site-backed directory or a permanent redirect target. This matches this project's established pre-launch policy (see [OrganizationFramework-Phase1.md](OrganizationFramework-Phase1.md)'s "architectural correctness takes precedence over preserving interim structures") — keeping two directory implementations alive at once would undercut the point of this document. Nothing changes about `/unit-sites` until the replacement is actually built and ready; this only resolves what happens once it is.

---

# Core Philosophy

Organizations are the primary public-facing entity.

Sites are optional.

A visitor should be able to discover and learn about a community regardless of whether that community:

- Uses a Central Portal hosted site
- Uses an external website
- Exists only as a directory listing

The public experience revolves around Communities and Organizations rather than Sites.

---

# Central Portal's Role

Central Portal serves two purposes:

## Community Directory

Helping people discover communities.

Examples:

- PTAs
- Scout Organizations
- Sports Communities
- Booster Clubs
- Community Groups
- Nonprofits

---

## Community Platform

Helping communities:

- Govern
- Organize
- Communicate
- Engage

through hosted tools and services.

Not every community will use every feature.

Not every community will host its website through Central Portal.

All communities may participate in the directory.

---

# Navigation

## Proposed Public Navigation

```text
Home

Community Directory

Tools

About

Login
```

---

## Removed Navigation

The following navigation concepts should be retired:

```text
Unit Sites

Who We Serve
```

These concepts are replaced by:

```text
Community Directory
```

and

```text
Organization Types
```

The nav labels above already changed (see "Current State vs. Future State"). The `/unit-sites` route itself is retired once the real `/communities` directory replaces it — see Resolved Decisions.

---

# Organization Types

Organization Types describe categories of communities.

Examples:

```text
PTA

Scouts

Billiards Community

HOA

Nonprofit
```

Organization Types help visitors understand what kinds of communities participate within Central Portal.

Organization Types are not communities themselves.

---

# Community Directory

The Community Directory becomes the primary discovery experience.

The directory contains Organizations.

Not Sites.

---

## Directory Goals

Allow visitors to:

- Search communities
- Browse communities
- View organization details
- Discover websites
- Explore organization hierarchies

---

## Directory Sources

The Community Directory should include:

### Hosted Communities

Organizations using Central Portal websites.

Example:

```text
Luxford PTA
```

Display:

```text
Hosted by Central Portal
```

---

### External Communities

Organizations using external websites.

Example:

```text
Bayside PTA
```

Display:

```text
External Website
```

Link:

```text
Visit Website
```

---

### Directory-Only Communities

Organizations without a website.

Display:

```text
Directory Listing Only
```

These organizations remain discoverable even without a web presence.

---

# Community Presence Model

Every Organization should fall into one of three visibility categories.

---

## Hosted

```text
Organization
        |
        +-- Site
```

Display:

```text
Visit Site
```

---

## External

```text
Organization
        |
        +-- External Website
```

Display:

```text
Visit Website
```

---

## Directory Only

```text
Organization
```

Display organizational information only.

---

# Organization Detail Pages

Every publicly listed Organization should have a detail page.

Route (see Resolved Decisions above):

```text
/communities/{identifier}
```

`{identifier}` is the Organization's `IdentifierValue` when it has one, otherwise its `Id`.

---

## Organization Details

Suggested information:

- Name
- Description
- Organization Type
- Organization Level
- Website Presence
- Parent Organization
- Child Organizations

Optional:

- Contact Information (see Resolved Decisions above — only available when the Organization has a linked Site with a primary contact set; a Directory-only Organization has no membership rows to source this from today)
- Social Media
- Region
- Membership Information
- Future Events

---

# Public Visibility

Visibility should be controlled at the Organization level.

Directory visibility is separate from Site visibility.

---

## Example

Organization:

```text
Public
```

Site:

```text
Members Only
```

Result:

```text
Organization appears in directory.
Website remains restricted.
```

---

## Suggested Organization Statuses

```text
Public

Pending

Private

Archived
```

Final design to be determined.

---

# Organization Hierarchy

Visitors should be able to understand organizational relationships.

---

## Parent Organization

Example:

```text
Luxford PTA

Parent:
Virginia Beach Council PTA
```

---

## Child Organizations

Example:

```text
Virginia Beach Council PTA

Children:

Luxford PTA
Bayside PTA
Arrowhead PTA
```

---

# Search

Search should operate against Organizations.

Search should not require a Site.

Support:

- Name
- Organization Type
- Organization Level

Future:

- Region
- City
- ZIP Code
- School

---

# Organization Types Page

The Organization Types page remains available.

Its purpose is explanatory rather than directory-oriented.

Examples:

```text
PTA

Scouts

Billiards Community
```

This page explains:

- Supported organization types
- How communities are structured
- Future community categories

The Organization Types page is not a replacement for the Community Directory.

---

# Relationship To Sites

Sites remain important.

Sites provide:

- Themes
- Branding
- Pages
- Tools

However:

```text
Organizations
```

become the primary public-facing object.

```text
Sites
```

become an optional capability of an Organization.

---

# Relationship To Future Frameworks

## Theme Framework

Themes apply to Sites.

Organizations may exist without themes.

---

## Tool Framework

Tools belong to Organizations.

Some tools may require a Site.

Others may not.

---

## Event Framework

Events belong to Organizations.

Events may be visible even when an Organization does not have a Site.

---

# Future Enhancements

Potential future additions:

- Community maps
- Geographic search
- Organization badges
- Verification status
- Community statistics
- Featured communities
- Hosted vs External filters

---

# Success Criteria

This initiative is successful when:

- Community Directory becomes the primary public discovery experience.
- Organizations become the primary public-facing entity.
- Hosted, External, and Directory-Only organizations are supported.
- Every public Organization has a detail page.
- Directory visibility is independent of Site visibility.
- Organization hierarchies are visible and understandable.
- Organization Types remain available as a supporting taxonomy.
- Future Themes, Tools, and Events naturally build upon the Organization model.