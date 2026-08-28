# Central Portal — Architecture Summary for Future Event Framework Development

_Prepared 2026-08-26. Reflects the codebase as of commit `bc6fc7c` plus the uncommitted OrganizationType/Organizations work in progress._

This document exists to give whoever builds the Event Framework (Tool Registry, Director/Helper Controls, Display Tool, Poll Tool, Question Tool, Meeting Tool) an accurate picture of what already exists, what's scaffolded-but-dormant, and what's genuinely absent. Several things below look like they should already support events (a `PortalTools`/`ToolPermission` schema, an `EventsSmallBox` component, `Event.*` permission-key examples in code comments) — the honest read is that these are early scaffolding or placeholder UI, not a working event system. That distinction matters for planning: a lot of the *shape* is already there, almost none of the *behavior* is.

---

## 1. Current Architecture

### Projects
Two-project solution:

- **[SiteEngine](../SiteEngine/)** — class library. Owns the EF Core data model: `Entities/` (POCOs), `Identity/` (ASP.NET Core Identity + site-membership types), `Enums/`, `Data/AppDbContext.cs` + `Data/SeedData.cs`, and `Migrations/`. No business logic, no web dependencies — purely persistence + domain shape.
- **[WebApp](../WebApp/)** — the actual application. Blazor Server (`.AddRazorComponents().AddInteractiveServerComponents()`, .NET 9), ASP.NET Core Identity for auth. `Services/` holds all business logic as scoped/singleton DI services; `Components/` holds all UI, organized by the folder convention in [CLAUDE.md](../CLAUDE.md) section 7 (`SharedPages/`, `PortalPages/`, `DivisionPages/`, `UnitPages/`, `Layout/`, `Dashboard/`, `Boxes/`, `Shared/`). `Authentication/` holds the custom passwordless-login pipeline.

No API project, no separate backend service, no mobile client — everything is server-rendered Blazor in one process.

### Major modules
- **Setup wizard** (`SetupService`, `SetupStateService`, `SetupConnectionStringProvider`, `SetupSiteContext`, `MigrationRunner`) — first-run flow that provisions DB connection, runs migrations, and creates the initial SuperAdmin. Designed to degrade gracefully when no DB is configured yet (see the startup-check block in [Program.cs](../WebApp/Program.cs)).
- **Site resolution** (`SiteContextResolver`, `RuntimeSiteContext`, `SiteContext`) — host-based multitenancy: incoming hostname is matched against `Site.Domain` → `Site.Hostname` (subdomain) → `Site.PtaId` fallback → Portal site, in that order.
- **Identity / passwordless auth** (`PasswordlessSignInService`, `PasswordlessCodeStore`, `EmailLoginSender`, `LoginTrackingService`, `LoginAnalyticsService`) — email-code login, no passwords. Tracks per-login history and rollup summaries per school year.
- **Site administration** (`SiteAdminService`, `FileUploadService`, `PtaLogoGenerationService`) — branding, logo generation/upload, per-site theme.
- **Dashboard / directory** (`DashboardService`) — the single largest service; backs the authenticated Dashboard, the public Unit Sites directory, Global Admin's Roles/Users/Sites tabs, and PortalHome's "Who We Serve" teaser.
- **Global Admin** — a large admin console under `/globaladmin/**`, itself split into sub-areas (Identity & Access, Sites & Content, Branding, System & Developer) each with its own nested layout (`IdentityAccessLayout`, `SitesContentLayout`, `BrandingLayout`, `SystemDeveloperLayout`) under the top-level `GlobalAdminLayout`.
- **Moderation** (`UserModerationService`, `BannedEmail`) — email-ban list checked at login.

### Shared services
Everything is a scoped (mostly) service resolved via constructor injection, built directly against `IDbContextFactory<AppDbContext>` (never a cached `AppDbContext` — see the `LiveConnectionDbContextFactory` comment in Program.cs explaining why: the connection string can change live during setup). No repository layer, no CQRS/MediatR, no separate application layer — services query EF directly and return DTO `record`s defined at the top of the service file (see `DashboardService`'s `MembershipSummary`, `SystemStats`, etc.). This is the dominant pattern and is worth continuing for any Event services.

### Existing patterns worth reusing
- **DTO records co-located with the service that produces them**, not a separate `Models`/`DTOs` project.
- **`IDbContextFactory<AppDbContext>` per-operation `await using` context** — never inject `AppDbContext` directly into a long-lived service.
- **School-year scoping as a string** (`SchoolYear.Current()`, format `"2026-2027"`, boundary July 1) — `CustomRole`, `SiteUserRole`, `BoardPosition` all carry a `SchoolYear` column. Any Event Framework entity that's naturally annual (e.g. a recurring event series, an annual talent show) should probably follow this same convention rather than inventing a new year concept.
- **Nullable-with-inheritance-chain fields** for anything a Local Unit should be able to inherit from its parent Division unless overridden (see `Site`'s theme colors/logos and their `Resolved*()` extension methods). Likely relevant if events can be defined at Division level and inherited/opted-into by Local Units.
- **Layout-level access gating** — a layout's `OnInitializedAsync` resolves the role once and either renders `@Body` or a gate component (`NoAccess`, `MembersOnlyGate`), rather than each page re-checking. `GlobalAdminLayout` and `SiteLayoutBase`/`RefreshMembersOnlyGateAsync` are the canonical examples.

---

## 2. Organization Model

The model is deliberately flat and thin. **There is no Department or Committee entity in this codebase today** — searching the whole tree for `Department`/`Committee` returns nothing. Anything under those headings below is inferred from what *does* exist and would need to be built.

### Organizations
- **`Site`** ([SiteEngine/Entities/Site.cs](../SiteEngine/Entities/Site.cs)) is simultaneously "the organization" and "the website" — one row is both a Division/Local Unit's identity record and its hosting/branding config (hostname, domain, theme colors, logos, social links, status). There is no separate "Organization" entity distinct from Site.
- **`OrganizationType`** (new/uncommitted — [SiteEngine/Entities/OrganizationType.cs](../SiteEngine/Entities/OrganizationType.cs)) is a *taxonomy* row ("PTA", eventually "Boy Scouts" etc.), not a real organization — it's what PortalHome/`/organizations` render to describe the kinds of groups the portal supports. Deliberately minimal (name, description, icon, sort order) — explicitly called out in its own comment as "no rules/tasks/branding link yet... real future work once there's more than one group to actually need per-type behavior." An Event Framework that needs type-specific event behavior (e.g. a PTA-specific event category vs. a Scouts-specific one) will need to extend this.
- Hierarchy is exactly two levels: `Site.ParentSiteId` (self-referencing FK, `Restrict` delete) — Portal has no parent, Divisions have no parent (or Portal, ambiguous today), Local Units point at a Division or stand alone. **No sub-Division grouping, no committee-as-site.**

### Departments / Committees
Do not exist. The closest analog is `BoardPosition` (a named position like "Treasurer" held by a `SiteUser` at a `Site` for a `SchoolYear`) and free-text `PositionName`/`SiteUserRole.CustomRole.Name` strings — there's no structured "this position belongs to this committee" relationship. If the Event Framework needs committee-scoped events (e.g. a Ways & Means committee running a fundraiser), that's new schema.

### Roles
Two parallel role concepts that don't fully unify:
1. **`SiteRole`** enum ([SiteEngine/Enums/SiteRole.cs](../SiteEngine/Enums/SiteRole.cs)) — `Viewer < Member < Officer < SiteAdmin < DivisionAdmin < SuperAdmin`. Declaration order **is** privilege order (code relies on `SiteRole.Max()`/`>=` comparisons directly on the enum). This is the built-in, portal-wide role vocabulary.
2. **`CustomRole`** ([SiteEngine/Identity/CustomRole.cs](../SiteEngine/Identity/CustomRole.cs)) — a free-form, per-site, per-school-year named role ("Newsletter Editor", etc.) that a `SiteUserRole` can point to *instead of* a built-in `SiteRole`.

Per [CLAUDE.md](../CLAUDE.md) section 2, "Groups" (permission bundles like "Event Managers", "Bingo.Caller") are a **documented intent**, not implemented — there is no `Group`/`PermissionGroup` entity anywhere in the schema today. This is the single biggest gap between the architecture doc and the actual code, and it's directly relevant to Director/Helper Controls (see §10) since those will likely want exactly this kind of tool-scoped group.

### Memberships
**`SiteUserRole`** ([SiteEngine/Identity/SiteUserRole.cs](../SiteEngine/Identity/SiteUserRole.cs)) is the actual membership join table: `SiteUser` × `Site` × (`SiteRole` or `CustomRole`) × `SchoolYear`, with optional `StartUtc`/`EndUtc` for partial-year terms and an `IsPrimaryContact` flag. A user can hold multiple rows for the same site/year (e.g. both `SuperAdmin` and `SiteAdmin` — `DashboardService.GetMembershipsAsync` explicitly groups these back together for display). This is the row an Event Framework's "who can register/who's a director for this event" logic would join against.

---

## 3. Permission Architecture

### Role system
Privilege is resolved, not stored pre-computed: **`SiteRoleResolver`** ([WebApp/Services/SiteRoleResolver.cs](../WebApp/Services/SiteRoleResolver.cs)) looks up all of a user's `SiteUserRole` rows for a given site (plus, specially, any `DivisionAdmin`+ role held on the *parent* Division — that one relationship cascades down; nothing else does), and returns the max `SiteRole`. An authenticated user with zero role rows still resolves to `Viewer`, never null. `SuperAdmin` is resolved by checking the role against the Portal site's own ID specifically (`SeedData.DefaultPortalSiteId`), since a SuperAdmin isn't expected to hold a membership row on every site they can administer.

`HighestRole(portalRole, membershipRoles)` is the static helper for "what's the best privilege this user has anywhere" — used for Dashboard-style aggregate gating.

### Claims
No custom claims system. ASP.NET Identity's standard claims are used only for `ClaimTypes.NameIdentifier` (the user ID) to key everything above. There is no claims-transformation middleware, no per-request claim enrichment.

### Module permissions
**Scaffolded but unused.** `ToolPermission` ([SiteEngine/Entities/ToolPermission.cs](../SiteEngine/Entities/ToolPermission.cs)) has a free-text `PermissionKey` column with example values baked right into the source comment:
```
Event.View
Event.Create
Event.Note.View
Event.Note.Create
Event.Note.Admin
```
This is a real signal of prior intent — a dotted, hierarchical permission-key convention was already being designed with events specifically in mind — but **no code anywhere reads or writes `ToolPermission` today.** It's an empty table with no service layer. `ToolRule` (rate limits, export/batch/advanced-mode flags, expiry, priority) is similarly defined but unused. Both would need a real permission-check service before they do anything.

### Organization-scoped permissions
This is what `SiteRoleResolver` already provides — role is always resolved *against a specific site ID*, not globally (except SuperAdmin). Any Event Framework permission check should follow the same shape: "does this user have role X (or ToolPermission key Y) at site Z," not a flat global permission.

---

## 4. Existing Extensibility

### Modules / Plugins
None. There's no plugin loading, no `IModule` interface, no assembly-scanning registration. Every feature is compiled directly into WebApp.

### Registries
**`PortalTools`** ([SiteEngine/Entities/PortalTools.cs](../SiteEngine/Entities/PortalTools.cs)) is a real, EF-mapped tool-registry table — `ToolName`, `ToolDescription`, `PageURL`, `ToolScope` (string: "Public"/"Division"/"LocalUnit"), `Category`, `IconClass`, `IsEnabled`, `SortOrder`, `Version`. This is exactly the shape [CLAUDE.md](../CLAUDE.md) section 6 describes for "PortalTools Pattern" and section 8 describes for the GiveBacks tool's registry entry. **However: `DashboardService.GetEnabledToolsAsync()` exists and queries it correctly, but nothing calls that method, and `SeedData.cs` never seeds any `PortalTools` rows.** The live `/portal-tools` page ([Tools/Index.razor](../WebApp/Components/PortalPages/Tools/Index.razor)) ignores the database entirely and renders a hardcoded `_placeholderTools` list (including a "Bingo Caller" and "Event Calendar" placeholder) all marked "Coming soon." **The registry schema exists; the registry is not actually driving anything yet.** This is probably the single most important finding for Tool Registry planning: the table design is sound and events-aware (`ToolScope` already distinguishes Division vs. LocalUnit), but wiring `PortalTools` → nav/dashboard → actual tool page is greenfield work, not a refactor.

### Dynamic navigation
Also effectively absent. `TopNavBar.razor` and `GlobalAdminLayout.razor`'s section nav are both hand-written, hardcoded `<NavLink>` lists — no database-driven or reflection-driven menu construction anywhere. Adding a new tool today means editing a `.razor` file, not registering it.

### A dormant content-block system (relevant to the Display Tool)
Worth flagging even though it predates the current architecture and looks partially abandoned: `SitePage` ([SiteEngine/Entities/SitePage.cs](../SiteEngine/Entities/SitePage.cs)) models a page's content as a `Text` blob of "block markup," and `WebApp/Components/Boxes/` contains shortcode-style components — `EventsSmallBox.razor` (`{smallbox events}`) and `BoardListFullBox.razor` (`{fullbox boardlist}`) — that accept `RawContent` + a `Parameters` dictionary and fall back to hardcoded sample data (an `EventsSmallBox` literally ships two fake sample events). A `<BlockRender PageId="...">` component is referenced in the legacy `DivisionPages/Programs.razor` page but **does not exist in the codebase** — that page is largely commented-out/dead code from an earlier iteration. `SitePage` is also **not registered in `AppDbContext`** (no `DbSet<SitePage>`), so it isn't even persisted today. Read this whole subsystem as "an earlier attempt at a block/widget content system, now dormant" — it's a useful prior-art reference for a Display Tool's widget model (named block types + raw-content-or-generated-data + parameters), not something to build on directly.

---

## 5. Existing Real-Time Features

None. No SignalR (`grep` across the repo finds zero hub/`AddSignalR` usage outside a README mention), no `BackgroundService`/`IHostedService`, no Hangfire/Quartz, no message queue. Blazor Server's own circuit is the only "live" mechanism in the app (per-user UI state pushed over its own SignalR-based transport, but that's framework plumbing, not an application-level real-time feature). A Meeting Tool or live Poll/Display Tool that needs cross-user real-time updates (e.g. audience-facing poll results updating live) will need to introduce SignalR (or lean on Blazor Server's existing circuit + a shared in-memory/DB-polling state) from scratch — there is no existing pattern to extend.

---

## 6. Existing Event-Related Features

Practically none are functional; what exists is either placeholder UI or unused schema:

- **Calendars** — none. No `CalendarEvent`/`Event` entity, no calendar UI beyond the word "Event Calendar" as one line in the hardcoded Tools placeholder list.
- **Registration** — none.
- **Attendance** — none.
- **Scheduling** — none.
- **Communications** — the only real communications infra is transactional: `EmailLoginSender`/SMTP config on `PortalConfig` for passwordless login codes and admin test emails. No bulk/newsletter/event-reminder sending exists (the Tools placeholder list also lists a "Newsletter Builder," also unbuilt).

The only concrete "event" artifacts in the whole codebase are: the `EventsSmallBox` placeholder component (two hardcoded fake events), the `Event.*` example permission keys in `ToolPermission`'s doc comment, and "Event Calendar" as placeholder tool-list text. None of these are wired to real data.

---

## 7. Existing Talent Show Concepts Worth Reusing

None found — no talent-show-specific code, entities, or references anywhere in the repo. If a Talent Show module is planned, it has no prior art here to build from; treat it as a new domain built on top of whatever the Event Framework's core primitives turn out to be (an "event" with participant sign-ups, an ordered running list/schedule, and judged or unjudged scoring would be the natural composition, but none of those primitives exist yet either).

---

## 8. Existing Event Concepts Worth Reusing

Beyond the placeholder components already covered in §6, the genuinely reusable prior art is architectural rather than event-specific:
- The **`ToolScope`/`PortalToolsSessions`+`ToolRule`+`ToolPermission` cluster** is the closest thing to a pre-designed extensibility seam for a Tool Registry-hosted Event module, including a hierarchical permission-key convention already sketched around `Event.*`.
- The **Division→LocalUnit inheritance pattern** (nullable field + `Resolved*()` fallback, as used for theme/branding) is the right template for "a Division defines an event series/template that Local Units can inherit or override."
- The **`SchoolYear` scoping convention** is the right template for annual/recurring events.

---

## 9. Existing Display / Dashboard Concepts

- **`Dashboard.razor`** is the single landing page for every authenticated role (no separate per-role dashboard route) — role-specific sections render conditionally *inside* it (`MembershipsBox`, `ProfileBox`, `GlobalAdminBox`, and a generic `ComingSoonBox` used pervasively for anything not yet built — see Feature Flags, Requests, etc. in Global Admin). A "Coming Soon" box is the established placeholder convention project-wide; any Event Framework page not ready for real data should follow that same pattern rather than inventing a new one.
- **Box/Card component convention**: small, self-contained `.razor` components under `Components/Dashboard/` and `Components/Boxes/`, each optionally accepting `Title` + free-form `Parameters`. This is the nearest thing to a "widget" system for a future Display Tool.
- **Bootstrap-based, card-grid layout** (`row g-3`/`g-4`, `col-md-*`) is the consistent visual idiom across PortalHome, Organizations, Tools, and Dashboard — no separate design system or component library beyond Bootstrap + FontAwesome icon classes.
- **`GetOrganizationTypesAsync` feeding two different pages** (PortalHome teaser + `/organizations` full list) from one query is the established "don't hardcode content, read it from a small admin-editable table" pattern — a good template for how a Display Tool should source its content.

---

## 10. Architectural Recommendations

**Event Framework (core).** Model it as new `SiteEngine` entities following the existing conventions exactly: a `Guid Id`, `SiteId` FK (events belong to a Division or Local Unit `Site`, same as everything else), `SchoolYear` scoping if annual/recurring, and a `Resolved*()`-style inheritance path if a Division-level event should be adoptable by its Local Units. Do **not** invent a separate "Organization" concept — reuse `Site`. Give it its own `EventService` in `WebApp/Services` built on `IDbContextFactory<AppDbContext>`, returning DTO records the same way `DashboardService` does — don't introduce a new architectural layer (repository/CQRS) just for this module.

**Tool Registry.** This is the one place where "finish what's scaffolded" beats "build new." `PortalTools`/`ToolRule`/`ToolPermission` already model almost exactly what's needed; the real work is (1) actually seeding `PortalTools` rows in `SeedData`, (2) making `Tools/Index.razor` and `TopNavBar`/dashboard nav read `DashboardService.GetEnabledToolsAsync()` instead of a hardcoded list, and (3) building the missing permission-check layer that turns `ToolPermission.PermissionKey` from an unread string column into something `SiteLayoutBase`-style layouts can gate on (`HasToolPermission(user, tool, "Event.Create")`). Keep `ToolScope` (Public/Division/LocalUnit) as the scoping axis — it already lines up with the Site hierarchy.

**Director Controls / Helper Controls.** There's no existing "Groups" implementation to extend (CLAUDE.md documents the concept; the schema doesn't have it) — this is genuinely new. The cleanest fit with what exists: extend the `CustomRole`/`ToolPermission` combination rather than building a whole separate `Group` entity from scratch, since `SiteUserRole` already supports "a user has a named, site-scoped, year-scoped role" — a "Director" or "Helper" designation on an *event* is naturally an event-scoped analog of that same shape (event ID instead of/alongside site ID). If Groups-as-permission-bundles (CLAUDE.md §2) get built for real, Director/Helper roles should almost certainly be expressed as tool-scoped Groups (e.g. `Event.Director`, `Event.Helper`) rather than a parallel role system.

**Display Tool.** Treat the dormant `SitePage`/`Boxes/` shortcode system as a spec to learn from, not code to revive — it's unregistered in `AppDbContext`, has no parser, and its one real component ships fake sample data. A new Display Tool should follow the same *shape* (named widget type + admin-editable parameters + safe fallback content) but be built and registered fresh, EF-mapped from day one.

**Poll Tool / Question Tool / Meeting Tool.** All three need real-time updates that don't exist anywhere in the app today — this is the biggest net-new infrastructure need in the whole list (see §5). Introduce SignalR deliberately as shared infrastructure (a single hub, or a small number of purpose-built hubs) rather than letting each tool grow its own ad hoc polling loop. A `Meeting` naturally composes on top of core Event primitives (it's an event with an agenda/attendee list); Poll and Question tools are good candidates to be genuinely generic (usable inside a Meeting, inside a Talent Show, standalone) if they're built against a neutral "context ID" rather than hardwired to one parent feature.

---

## 11. Module-Specific vs. Shared Infrastructure

**Should be shared (WebApp/Services, following current conventions):**
- Event core entities/service (the `Event` itself, its site/school-year scoping, its Division→Unit inheritance).
- The Tool Registry once wired up (`PortalTools`/`ToolRule`/`ToolPermission` + the permission-check layer).
- Director/Helper role resolution, if built as an extension of `SiteUserRole`/`CustomRole` rather than a bespoke per-tool table.
- Any real-time transport (SignalR hub(s)) — one shared mechanism, not one per tool.
- File upload (`FileUploadService` already exists and is generic — reuse it for event photos/attachments rather than writing a new uploader).

**Should stay module-specific:**
- Talent Show domain logic (running order, scoring/judging) — compose on top of shared Event primitives, don't push show-specific fields onto the core `Event` entity.
- Poll/Question *content* (question text, answer options, response tallies) — the transport (SignalR) is shared, the data model is per-tool.
- Meeting-specific concepts (agenda items, minutes, motions/votes) — same reasoning; a Meeting is a specialization of Event, not a rename of it.
- Bingo Caller, GiveBacks Import, Newsletter Builder — already-scoped single-purpose tools per CLAUDE.md; no reason to entangle them with Event Framework internals beyond registering in the shared Tool Registry.

---

## 12. Potential Risks / Conflicts with the Current Architecture

- **No permission-check service exists yet.** Every access-gating decision in the app today is `SiteRole`-based (`role is SiteRole.SiteAdmin or SiteRole.DivisionAdmin or SiteRole.SuperAdmin` sprinkled directly in layouts). Introducing `ToolPermission`-key-based checks for Director/Helper Controls means either (a) building that missing layer now, as shared infra everyone benefits from, or (b) events ending up gated by the same coarse `SiteRole` checks as everything else, which won't scale to per-event director/helper distinctions. Decide this explicitly before building Director/Helper Controls — retrofitting later means revisiting every gate.
- **`Group` (CLAUDE.md §2) is documented but unbuilt.** If Director/Helper Controls are meant to be an instance of the documented Groups concept, Groups needs to be designed first (or alongside), or the two will diverge and someone will have to reconcile them later.
- **No real-time infrastructure at all.** Poll/Question/Meeting tools' core value proposition (live results, live attendance) is blocked on introducing SignalR as new shared infrastructure — this is a real scope item, not a footnote, and touches `Program.cs` DI/middleware setup that's currently very carefully sequenced around setup-mode/DB-availability (see the extensive comments in Program.cs about `IsConfigured` gating). Any hub registration needs to respect that same "must not blow up before the DB is configured" constraint.
- **The dormant `SitePage`/`BlockRender`/`Boxes` system is a trap for reuse.** It looks like existing infrastructure (there are real component files, a real entity) but is disconnected from the database (`SitePage` isn't in `AppDbContext`) and partly references a component (`BlockRender`) that doesn't exist. Anyone skimming the codebase for "how do we render dynamic content blocks" could easily start extending dead code. Worth flagging to the team so it's either finished, formally deprecated/removed, or clearly commented as historical.
- **`PortalTools` table is unseeded and unread by the UI it should drive.** Same trap as above at smaller scale: it looks finished (full entity, full DTO service method) but the last mile (seed data + UI wiring) was never done. Building new tools against this registry without first closing that gap means the registry still won't actually show anything.
- **`SiteType` is a strict two-level hierarchy (Portal → Division → LocalUnit) with no room for a third level.** If events ever need Division-wide, cross-unit sub-groupings (e.g. a Council of multiple Divisions running a joint Talent Show), that doesn't fit today's `Site.ParentSiteId` single-parent chain without schema change.
- **Everything is single-tenant-per-process EF Core against one `AppDbContext`.** There's no soft-delete, no auditing/event-sourcing, no optimistic concurrency tokens on any entity shown. An Event Framework that needs an audit trail (who registered whom, who changed a poll after votes were cast) will need to add that pattern itself — there's no existing convention to lean on.
