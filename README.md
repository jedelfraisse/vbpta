# Central Portal
## One Platform, Many Communities

**Status: Active Development**

Central Portal is an open-source **community management platform** — a place for organizations to govern effectively, stay organized, communicate clearly, and engage their members.

It began as a PTA site engine. Through ongoing design work, that scope grew into something bigger: a platform built around **Communities** as its central concept, where a PTA, a booster club, a scout troop, an HOA, or any other member-driven group can each get its own branded site, membership, and tools — connected under one shared Portal.

**PTA is the first organization type Central Portal supports.** The platform is being architected to generalize from there to any community-driven organization.

> This document describes both **what Central Portal is today** and **where it's headed**. Sections describing the site hierarchy, membership, and branding describe the current, working platform. Sections describing Events, Controllers, Tools, and Displays describe intended architecture — most of it designed, little of it built yet. Those sections are marked **(Planned)**.

---

## What's Here Today

- **A multi-tenant site engine.** Every community — a Division or a Local Unit — gets its own hosted site: custom domain/subdomain, logo, color theme, banner, and social links, with Local Units inheriting a parent Division's branding unless they override it.
- **Passwordless authentication**, with per-community, per-school-year membership and role assignment (Officer, Site Admin, Division Admin, Super Admin, or a custom named role).
- **A public directory** of Divisions and Local Units, with per-site visibility status (active, members-only, pending launch, etc.).
- **A member dashboard** and a **Global Admin console** covering identity & access, sites & content, branding, and system settings.
- **PTA as the first configured organization type**, with the data model in place to add more without changing the core platform.

Everything from here forward describes the direction the platform is headed, not a feature list of what's shipped.

---

# Mission

Central Portal exists to help organizations:

## Govern

Support organizational structure, policies, leadership, elections, meetings, and continuity.

Examples:

- Board Management — *available: board positions are tracked per community per year*
- Officer Tracking — *available: officer roles are part of membership*
- Governance Support — *planned*
- Meeting Management — *planned*
- Voting Tools — *planned*
- Committee Support — *planned*

---

## Organize

Provide tools that help communities coordinate activities and operations.

Examples:

- Events
- Volunteer Coordination
- Scheduling
- Attendance
- Talent Shows
- Fundraisers
- Community Programs

**Status: Planned.** None of the above exist in the platform yet — this is the primary direction of near-term development. See [Events](#events-planned) below.

---

## Communicate

Improve communication between leaders, members, volunteers, supporters, and the community.

Examples:

- News
- Announcements
- Messaging
- Public Displays
- Broadcasts
- Notifications

**Status: Planned**, aside from the transactional email the platform already sends (login codes, admin notices).

---

## Engage

Encourage participation and involvement.

Examples:

- Polls
- Questions & Answers
- Raffles
- Quizzes
- Interactive Games
- Membership Incentives

**Status: Planned.** See [Event Tools](#event-tools-planned) below.

---

# Platform Architecture

The platform is built around **Communities**. Every Division and every Local Unit is a community — each gets its own website, branding, content, members, and administrators, while staying connected to the broader Central Portal ecosystem through the shared Portal site above it.

This part of the architecture is implemented today.

---

## Portal

The top-level Central Portal site.

Responsibilities:

- Public information
- Community directory
- Shared tools *(directory of tools is scaffolded; most listed tools are planned)*
- Shared resources
- Authentication
- Administration

---

## Divisions

Communities that support multiple local communities beneath them.

Examples:

- PTA Councils
- District Organizations
- Regional Associations

A division may provide:

- Shared branding — *available*
- Shared resources — *planned*
- Shared events — *planned*
- Shared communications — *planned*

---

## Local Units

Individual communities operating under a division or independently.

Examples:

- Individual PTAs
- Booster Clubs
- Neighborhood Associations
- Community Groups

Each unit maintains:

- Custom branding — *available*
- Memberships — *available*
- Announcements — *planned*
- Events — *planned*
- Documents — *planned*
- Tools — *planned*

---

# Organization Types

The platform is designed to support multiple organization types. This part is implemented — `OrganizationType` is a real, admin-editable list — though today it holds one entry.

Current focus:

- PTA

Future possibilities:

- Scout Organizations
- Booster Clubs
- HOAs
- Nonprofits
- Civic Associations
- Community Groups

Organization types may eventually determine:

- Available tools
- Default branding
- Recommended workflows
- Governance templates

---

# Events (Planned)

**Status: Not yet built.** This section describes the intended architecture, not a working feature.

Events are meant to be a core organizational feature — the activities a community runs, built on top of the same Community/Division/Local Unit structure described above.

Examples:

- PTA Meetings
- Talent Shows
- Bingo Nights
- Membership Drives
- Fundraisers
- Trainings
- Town Halls
- Podcasts
- Community Events

Every event will belong to a community.

---

# Event Controllers (Planned)

**Status: Not yet built.**

The intent is for each event to be managed by a Controller — a component that defines the primary behavior of an event.

Examples:

- Meeting Controller
- Talent Show Controller
- Bingo Controller
- Raffle Controller
- Training Controller

Controllers would manage event-specific workflows and data, sitting on top of a shared Event core rather than duplicating it.

---

# Event Tools (Planned)

**Status: Not yet built.**

Tools are meant to provide reusable capabilities that can be attached to events when needed, rather than being built once per event type.

Examples:

- Display Tool
- Poll Tool
- Question Tool
- Volunteer Tool
- Attendance Tool
- Judge Tool
- OBS Integration Tool
- Membership Incentive Tool

The intent is for tools to be enabled or disabled depending on the organization type and the specific event's requirements.

---

# Displays and Live Events (Planned)

**Status: Not yet built.** The platform has no real-time infrastructure today; this is a meaningful piece of new work, not a small extension.

Certain events may eventually utilize live displays:

- Projectors
- TVs
- Browser Sources
- OBS Overlays
- Information Kiosks
- Judge Screens
- Audience Displays

Displays would connect to the platform and receive:

- Assigned Templates
- Event Data
- Tool Data

Examples:

- Meeting Agenda Board
- Talent Show Main Screen
- Bingo Board
- Raffle Winner Screen

---

# Integrations (Planned)

The platform is designed to eventually work alongside existing tools and services communities already rely on.

Examples:

- Givebacks
- SignUpGenius
- Google Workspace
- Mailchimp
- Canva
- Streaming Platforms
- OBS Studio

**Status today:** outbound transactional email (SMTP) is the only integration implemented. The goal for the rest is not necessarily to replace these tools but to provide a centralized place to manage and connect them.

---

# Roles

Examples:

- Super Administrator — *available*
- Division Administrator — *available*
- Site Administrator — *available*
- Officers — *available*
- Board Members — *available (board positions are tracked per community per year)*
- Volunteers — *planned*
- Members — *available*
- Public Users — *available*

Today, roles are assigned per community. Future tool-specific permissions (see [Event Tools](#event-tools-planned)) will let communities delegate responsibility for a specific function — running a raffle, moderating a poll — without granting full administrative access.

---

# Open Source

Central Portal is fully open source.

Goals include:

- Self-hosting support
- SaaS deployment support
- Community contributions
- Long-term sustainability
- Reduced dependence on expensive third-party platforms

---

# Roadmap

A rough sense of the path from here, in order:

1. **Tool Registry & permissions.** Finish wiring the existing tool-registry data model into real navigation and access control, so future tools (and Director/Helper-style delegated permissions) have somewhere to plug in.
2. **Event core.** A shared `Event` concept that belongs to a community, with Division-to-Local-Unit inheritance the same way branding works today.
3. **Event Controllers.** The first controller (most likely Meetings or a simple community Event) built on the Event core, proving the controller pattern.
4. **Real-time infrastructure.** The platform has none today; this becomes necessary once Display/Poll/Question tools need live updates.
5. **Event Tools.** Display, Poll, Question, and similar reusable tools, attachable to any controller.
6. **Additional Controllers.** Talent Show, Bingo, Raffle, and other event types, built on the same core.
7. **Integrations.** Connecting to the external services communities already use.

---

# Long-Term Vision

Central Portal aims to become a complete community management platform that helps organizations:

- Govern effectively
- Stay organized
- Communicate clearly
- Engage their communities

while allowing every organization to maintain its own identity, branding, culture, and operating style.

One Platform.
Many Communities.
