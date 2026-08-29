# OrganizationFramework-Phase1.md

## Status

Complete (2026-08-28). See [OrganizationFramework-Phase1-Notes.md](OrganizationFramework-Phase1-Notes.md) for implementation decisions and validation, and the note under "SMTP Configuration" below for the one deliberate deviation from this doc as originally written.

---

# Purpose

Phase 1 establishes the Organization Framework as the foundational architecture of Central Portal.

The goal is to build the correct long-term organizational model before implementing:

- Theme Framework
- Tool Framework
- Event Framework
- Meeting Controllers
- Talent Show Controllers
- Community-Specific Features

This phase focuses on organizational architecture rather than feature development.

Central Portal has not officially launched.

Therefore, architectural correctness takes precedence over preserving interim development structures.

---

# Scope

This document defines the first implementation phase of the Organization Framework.

The Organization Framework establishes the foundational community and hierarchy model that future platform features will build upon.

Examples include:

- Themes
- Tools
- Events
- Membership Enhancements
- Community-Specific Workflows

---

# Relationship to Other Documents

This document implements the concepts defined in:

- README.md
- OrganizationFramework.md

This document focuses on implementation planning rather than platform vision.

---

# Resolved Decisions

Resolved after review.

## Membership Migration Is Analysis Only

Phase 1 adds the new framework schema and documents how current membership entities should eventually align.

Phase 1 does not:

- Re-point SiteUserRole
- Re-point CustomRole
- Re-point BoardPosition
- Modify SiteRoleResolver
- Change existing permission checks

Membership migration is deferred to a future phase.

---

## Site Resolution Is Untouched

Phase 1 does not alter:

- RuntimeSiteContext
- SiteContextResolver
- Hostname Resolution
- Domain Resolution

Current request routing remains unchanged.

Future routing enhancements are deferred until the Organization Framework is proven.

---

# Phase 1 Objectives

## Establish Organization As The Core Entity

Organizations become the primary logical entity within Central Portal.

Organizations represent:

- Community Identity
- Membership Ownership
- Hierarchy
- Roles
- Operational Cycles
- Permissions

Future systems should be built around Organizations instead of Sites.

---

## Separate Organizations From Websites

Organizations and Sites serve different purposes.

### Organization

Owns:

- Identity
- Memberships
- Roles
- Governance
- Hierarchy

### Site

Owns:

- Branding
- Public Presence
- Navigation
- Pages
- Themes

Organizations may have:

- No Site
- One Site

Future support for multiple Sites per Organization may be considered later.

---

## Support Flexible Hierarchies

The platform should not assume a fixed hierarchy.

Examples:

### PTA

```text
National
    |
State
    |
Region
    |
Council
    |
Unit
```

### Scouts

```text
National
    |
Council
    |
District
    |
Troop
```

### Billiards

```text
Network
    |
Region
    |
League
    |
Team
```

Hierarchy depth must be data-driven.

---

## Support Operational Cycles

The platform must not assume School Year as the only operational model.

Examples:

- School Year
- League Session
- Calendar Year
- Fiscal Year

Operational Cycles become a framework-level concept.

---

## Support Explicit Parent Access

Cross-organization access should be configurable.

Examples:

- View
- Participation
- Administrative

No grant should exist by default beyond the established defaults.

---

## Validate Through Administration

The Organization Framework should be usable through the Global Admin interface.

Phase 1 is not complete when only database entities exist.

Phase 1 is complete when a Global Administrator can:

- Create Organization Types
- Define Organization Levels
- Define Operational Cycle Types
- Create Organizations
- Build Organization Hierarchies
- Configure Parent Access Grants

without modifying code.

The administrative experience becomes the primary mechanism for validating the framework.

---

# Existing Infrastructure To Preserve

The following systems are considered foundational platform services and remain unchanged.

---

## Setup Wizard

Current Setup functionality remains.

Responsibilities:

- Database Configuration
- Connection Validation
- Initial System Setup
- Initial Administrator Creation

The Setup Wizard is platform infrastructure, not organization functionality.

---

## Authentication

Current Passwordless Authentication remains.

Responsibilities:

- Identity Management
- Login Verification
- User Authentication

---

## SMTP Configuration

Current SMTP functionality remains — with one deviation from the original plan, made after Phase 1's schema/UI work was otherwise done: every SMTP send (login codes, setup-wizard test, admin verification code, Global Admin test email) was migrated from `System.Net.Mail.SmtpClient` to MailKit, behind one shared `SmtpMailSender`. That legacy client couldn't parse a real SMTP server's EHLO response (a "250-AUTH=..." extension line some servers, including smtp4dev, advertise for old-client compatibility) — a genuine, blocking bug encountered while testing login, not organizational-framework scope creep. Responsibilities are unchanged:

- Login Emails
- Notifications
- Future Communications

---

## Site Resolution

Current hostname and domain resolution remain unchanged during Phase 1.

Future routing enhancements remain out of scope.

---

# New Concepts Introduced

## Organization

Represents:

- Community
- Identity
- Membership Ownership
- Hierarchy Placement

---

## Organization Type

Defines:

- Supported Hierarchies
- Operational Cycles
- Available Controllers
- Available Tools
- Default Themes

Examples:

- PTA
- Scout Organization
- Sports Community
- HOA

---

## Organization Level

Defines positions within an Organization Type hierarchy.

Examples:

- National
- State
- Region
- Council
- Unit

Levels must be data-driven.

---

## Operational Cycle

Represents an organizational operating period.

Structure:

```text
Type
DisplayLabel
StartDate
EndDate
```

Examples:

```text
2026-2027 School Year
```

```text
Summer 2026 League Session
```

```text
FY2026
```

---

## Parent Access Grant

Defines cross-organization permissions.

Examples:

- View
- Participation
- Administrative

Design Note:

"Disabled" is represented by the absence of a grant record.

---

# Current Entities Review

The following entities should be reviewed rather than immediately replaced:

- Site
- SiteUser
- SiteUserRole
- CustomRole
- BoardPosition
- OrganizationType

The objective is to determine:

- What can be reused
- What should evolve
- What should eventually be retired

---

# Future Relationship Model

```text
OrganizationType
    |
    +---- OrganizationLevel

Organization
    |
    +---- references OrganizationLevel

Organization
    |
    +---- Parent Organization

Organization
    |
    +---- Site (optional)
```

Organizations become the authoritative hierarchy.

Sites become optional web presences.

---

# Parent Access Recommendation

Default recommendation:

```text
View Access
```

When a parent organization creates a child organization:

Parent receives View Access.

Participation and Administrative Access require explicit configuration.

---

# Phase 1 Deliverables

## Organizational Model

Implement:

- Organization
- Organization Type Enhancements
- Organization Levels
- Operational Cycles
- Parent Access Grants

---

## Global Admin Organization Management

Build functional Global Admin pages for:

- Organization Types
- Organization Levels
- Operational Cycles
- Organizations
- Parent Access Grants

These pages should provide sufficient functionality to configure and manage organization hierarchies without direct database access.

---

## Hierarchy Model

Replace fixed hierarchy assumptions with configurable hierarchy definitions.

---

## Existing Data Backfill

Determine and implement how existing Site records become Organizations.

Options include:

- Automatic One-to-One Backfill
- Manual Recreation

Because the platform has not launched, architectural correctness takes priority over preserving interim structures.

---

## CLAUDE.md Update

Update CLAUDE.md to remove assumptions regarding:

```text
Portal
Division
Local Unit
```

and replace them with Organization Type driven hierarchies.

---

## Membership Analysis

Determine how:

- SiteUser
- SiteUserRole
- CustomRole
- BoardPosition

fit into the future Organization model.

This phase does not perform migration.

---

## Organization Framework Test Configurations

Create sample configurations through the Global Admin UI.

### PTA

```text
National
State
Region
Council
Unit
```

### Scouts

```text
National
Council
District
Troop
```

### Billiards Community

```text
Network
Region
League
Team
```

No code changes should be required between these configurations.

---

# Phase 1 Success Criteria

Phase 1 is successful when:

- Organization exists as a first-class EF entity.
- Organization supports parent-child relationships.
- OrganizationLevel is data-driven rather than code-driven.
- OperationalCycle exists as a structured entity.
- ParentAccessGrant exists and supports configurable cross-organization access.
- Global Admin pages exist for Organization Framework management.
- PTA, Scout, and Billiards hierarchies can be configured entirely through administration screens.
- Existing Setup, Authentication, and Site Resolution functionality remain unchanged. SMTP sending was migrated to MailKit (see "SMTP Configuration" above) — a justified deviation, not a regression: verified working, and every send site was migrated consistently rather than just the one that surfaced the bug.
- Membership migration strategy is documented.
- CLAUDE.md is updated to reflect the new hierarchy model.

---

# Deferred To Future Phases

The following initiatives are intentionally deferred:

- Theme Framework
- Tool Framework
- Event Framework
- Display Framework
- SignalR Infrastructure
- Meeting Controllers
- Talent Show Controllers
- Poll Tools
- Question Tools

These depend on completion of the Organization Framework.

---

# Future Documents

Depends On:

```text
OrganizationFramework.md
```

Enables:

```text
ThemeFramework.md

ToolFramework.md

EventFramework.md
```

The Organization Framework must be established before higher-level platform functionality is implemented.