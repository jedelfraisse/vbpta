# Central Portal Organization Framework

## Status

Proposed

---

# Purpose

The Organization Framework defines how communities are represented within Central Portal.

It provides the structure for:

- Communities
- Organizations
- Organization Types
- Organizational Hierarchies
- Operational Cycles
- Branding Inheritance
- Permission Inheritance
- Memberships
- Events
- Controllers
- Tools

The Organization Framework is the foundation upon which all other major platform features are built.

---

# Resolved Decisions

Resolved after reviewing the current Central Portal architecture.

## Organization Owns Site(s)

Organization is the primary entity.

Site represents a hosted web presence.

Organizations may exist without a website.

Sites belong to Organizations.

---

## Not Every Level Requires a Website

Some hierarchy levels may require a website.

Some hierarchy levels may only exist for governance, reporting, or hierarchy purposes.

Whether a level is site-enabled is determined by the Organization Type.

---

## Parent-Child Access Is Explicit

Cross-organization access is controlled through configurable grants.

No access should be automatically inherited simply because an organization is a parent.

The existing implicit parent-child cascade should eventually be replaced with explicit relationship grants.

---

# Core Philosophy

Central Portal is a Community Management Platform.

Organizations may differ significantly in purpose and structure, but most share common needs:

- Governance
- Organization
- Communication
- Engagement

Central Portal provides a common platform while allowing each community to maintain its own identity and operational structure.

---

# Key Concepts

## Community

Community is the preferred user-facing term.

Examples:

- Luxford PTA
- Virginia PTA
- Scout Pack 123
- Hampton Roads Pool League
- Neighborhood Association

Communities are represented internally by Organizations.

---

## Organization

An Organization represents a real-world entity.

Examples:

- National PTA
- Virginia PTA
- Virginia Beach Council PTA
- Luxford PTA

Organizations provide:

- Identity
- Hierarchy
- Membership
- Roles
- Permissions
- Events
- Controllers
- Tools

Organizations may optionally have a Site.

---

## Site

A Site represents a hosted web presence.

Examples:

- luxfordpta.org
- luxford.centralportal.org

A Site provides:

- Branding
- Theme
- Navigation
- Pages
- Content
- Public Information

A Site belongs to an Organization.

Organizations may have:

- No Site
- One Site

Future versions may support multiple Sites per Organization if needed.

---

# Organization Types

Organization Types define how organizations operate.

Examples:

- PTA
- Scouts
- Booster Club
- HOA
- Civic Association
- Sports League
- Billiards Community
- Nonprofit

Organization Types may define:

- Organizational Levels
- Operational Cycles
- Available Controllers
- Available Tools
- Default Themes
- Governance Templates
- Branding Rules

Organization Types should provide recommendations and defaults rather than unnecessary restrictions.

---

# Operational Cycles

Different organization types operate on different schedules.

The platform should not assume every organization uses a School Year.

This generalizes the `SchoolYear` scoping used today by `SiteUserRole`, `CustomRole`, and `BoardPosition` — see [Relationship To Existing Membership Schema](#relationship-to-existing-membership-schema).

---

## PTA Example

Operational Cycle Type:

School Year

Examples:

- 2025-2026
- 2026-2027

Used for:

- Memberships
- Board Positions
- Officer Terms
- Awards
- Reports

---

## Billiards Example

Operational Cycle Type:

League Session

Examples:

- Spring 2026
- Summer 2026
- Fall 2026

Used for:

- Teams
- Leagues
- Rankings
- Statistics

---

## HOA Example

Operational Cycle Type:

Calendar Year

Examples:

- 2026
- 2027

Used for:

- Elections
- Budgets
- Board Terms

---

## Nonprofit Example

Operational Cycle Type:

Fiscal Year

Examples:

- FY2026
- FY2027

Used for:

- Budgets
- Governance Reporting
- Financial Reporting

---

## Operational Cycle Definition

An Organization Type may define:

- Operational Cycle Name
- Naming Convention
- Start Date
- End Date
- Reporting Periods

Organizations may customize when permitted.

**Representation:** an Operational Cycle should eventually be a structured record — `StartDate`, `EndDate`, `DisplayLabel`, `Type` — rather than a string label, so date-range queries and cross-organization rollups work regardless of naming convention. Today's `SchoolYear` (a raw string like "2026-2027", produced by `SchoolYear.Current()`) can remain a string during migration; it doesn't need to become structured before the rest of this framework lands.

---

## Operational Cycle Usage

Operational Cycles may be used by:

- Memberships
- Board Positions
- Officer Terms
- Roles
- Committees
- Events
- Reports
- Statistics
- Awards

Operational Cycle support should be configurable.

Not all features must require operational cycle tracking.

---

# Organizational Levels

Different organization types may define different hierarchy structures.

Levels should be data-driven.

The platform should not assume a fixed hierarchy depth.

---

## PTA Example

```text
National PTA
    |
State PTA
    |
Region
    |
Council
    |
Local Unit
```

---

## Scout Example

```text
National
    |
Council
    |
District
    |
Pack / Troop
```

---

## Billiards Example

```text
Community Network
    |
Region
    |
League
    |
Team
```

---

# Organization Level Definition

Each Organization Type defines its own hierarchy.

Example:

```text
Level 1: National

Level 2: State

Level 3: Region

Level 4: Council

Level 5: Local Unit
```

Levels should be configurable through administration rather than code.

---

# Organization Relationships

Organizations may have a parent organization.

Example:

```text
Virginia PTA
    Parent: National PTA

Virginia Beach Council PTA
    Parent: Virginia PTA

Luxford PTA
    Parent: Virginia Beach Council PTA
```

The framework should support hierarchy depths greater than today's structure.

---

# Organization Independence

Organizations may operate independently.

Example:

```text
Independent PTA

Parent: None
```

Independent organizations should have access to the same platform capabilities.

---

# Parent-Child Access

Parent organizations may receive access to child organizations.

Access is granted explicitly.

---

## View Access

Allows:

- Dashboards
- Reports
- Membership Information
- Event Information

---

## Participation Access

Allows:

- Shared administration
- Event cooperation
- Shared resources

---

## Administrative Access

Allows:

- User Management
- Content Management
- Tool Management
- Configuration

---

## Disabled

No access.

Parent receives no automatic permissions.

---

## Open Question

When a child organization is created:

Should the parent receive:

- No access
- View access
- Administrative access

by default?

This should be determined during implementation planning.

---

# Inheritance

Inheritance reduces duplicate configuration.

Organizations may:

- Inherit
- Override
- Extend

parent settings.

---

## Theme Inheritance

Example:

```text
National Theme
        ↓
State Theme
        ↓
Council Theme
        ↓
Unit Theme
```

Each level may override inherited settings.

---

## Tool Availability Inheritance

Example:

```text
State PTA

Enables Meeting Controller

        ↓

Council

Inherits Availability

        ↓

Unit

Receives Availability
```

Organizations may disable inherited functionality when allowed.

---

## Template Inheritance

Examples:

- Event Templates
- Meeting Templates
- Governance Templates
- Communication Templates

Templates may flow down the hierarchy.

---

## Operational Cycle Inheritance

Operational Cycle definitions may be inherited.

Examples:

```text
National PTA
    Defines:
    School Year
    July 1 - June 30

State PTA
    Inherits

Council
    Inherits

Unit
    Inherits
```

---

# Memberships

Users belong to Organizations.

Memberships determine:

- Visibility
- Participation
- Administration
- Eligibility

A user may belong to multiple Organizations.

Example:

```text
Luxford PTA

Virginia Beach Council PTA

Community Pool League
```

---

# Relationship To Existing Membership Schema

Current entities include:

- SiteUser
- SiteUserRole
- CustomRole
- BoardPosition

These should evolve rather than be replaced.

The long-term direction is shifting memberships from Site-centric ownership toward Organization-centric ownership.

These entities also currently scope by a string `SchoolYear` field (e.g. "2026-2027", July 1 – June 30). That scoping generalizes to [Operational Cycles](#operational-cycles) — `SiteUserRole`, `CustomRole`, and `BoardPosition` would eventually key off an Operational Cycle rather than a hardcoded school-year string.

---

# Roles

Organizations may define roles.

Examples:

- President
- Treasurer
- Secretary
- Board Member
- Volunteer
- Committee Chair

---

# Groups

Groups provide scoped permission bundles.

Examples:

- Event Managers
- Bingo Callers
- Meeting Moderators
- Talent Show Judges

Groups are separate from organizational leadership roles.

This avoids creating multiple permission systems.

Future Event and Tool permissions should leverage Groups.

---

# Communities vs Organizations

Public Interfaces:

```text
Community
```

Administrative Interfaces:

```text
Organization
```

Examples:

```text
Community:
Luxford PTA

Organization:
Luxford PTA
```

The platform should present community-focused language whenever possible.

---

# Relationship To Events

Organizations create Events.

Examples:

- Meetings
- Talent Shows
- Fundraisers
- Tournaments
- Trainings

Events belong to Organizations.

Permissions and defaults may inherit from the owning Organization.

---

# Relationship To Controllers

Controllers define event-specific behavior.

Examples:

- Meeting Controller
- Talent Show Controller
- Bingo Controller
- Tournament Controller

Controller availability may be restricted by Organization Type.

---

# Relationship To Tools

Tools provide reusable functionality.

Examples:

- Display Tool
- Poll Tool
- Question Tool
- Volunteer Tool
- Attendance Tool
- Judge Tool

Availability may be controlled by:

- Organization Type
- Organization Level
- Parent Policies
- Administrative Configuration

---

# Multi-Tenant Vision

Every Organization receives:

- Its own identity
- Its own memberships
- Its own roles
- Its own events
- Its own controllers
- Its own tools
- Its own settings

Organizations may optionally receive:

- A dedicated website
- Custom branding
- Custom domains

while remaining connected to the larger Central Portal ecosystem.

---

# Long-Term Goal

Create a flexible Organization Framework capable of supporting many community types while maintaining a consistent governance, organization, communication, and engagement experience.

Examples include:

- PTA Organizations
- Scout Organizations
- Civic Groups
- Sports Communities
- Billiards Networks
- Nonprofits
- Educational Organizations

without requiring major platform redesign when new organization types are introduced.