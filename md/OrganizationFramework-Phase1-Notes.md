# Phase 1 Implementation Notes

Status: **Implemented**, 2026-08-26. Companion to [OrganizationFramework.md](OrganizationFramework.md) and [OrganizationFramework-Phase1.md](OrganizationFramework-Phase1.md) — this is the "stop and document" record for decisions made while actually building it, not covered explicitly by either planning doc.

---

## Decisions made during implementation

### Operational Cycle is owned by Organization Type, not Organization

Neither planning doc fully settles this. "Cycle Inheritance" describes a definition set once (at "National PTA") and inherited down a chain — which reads like a per-Organization, Theme-style inheritance walk. But every PTA Organization sharing the same School Year boundaries is really a property of *being a PTA*, not something each of hundreds of Local Units should carry its own (redundant, potentially drifting) copy of.

**Decision:** `OperationalCycle` rows belong to `OrganizationType` (`OrganizationTypeId` FK), not to an individual `Organization`. "National PTA defines it, everyone inherits" is satisfied by there being exactly one set of cycle instances for the whole "PTA" type — nothing to inherit because there's nothing per-Organization to override.

Per-Organization override (an individual Council running its own custom cycle) is **not implemented**. Deferred, because nothing consumes `OperationalCycle` yet — membership migration is analysis-only this phase (see Phase 1's "Resolved Decisions"), so there's no real requirement forcing the harder design now. Revisit when membership actually starts keying off cycles.

### "Organization Type Enhancements" = new relationships, not new columns

`OrganizationType` itself gained no new scalar fields. It's "enhanced" by `OrganizationLevel` and `OperationalCycle` both FK-referencing it — the type becomes meaningful by what points at it, not by carrying more of its own data. Flagging this in case "enhancements" was expected to mean literal new columns on the entity.

### Default Parent Access grant is a second, explicit call — not baked into org creation

`OrganizationService.CreateOrganizationAsync` does **not** automatically create a `ParentAccessGrant`. `GrantDefaultParentAccessAsync` is a separate method the UI calls right after a successful create-with-parent. Behaviorally this matches Phase 1's "Parent Access Recommendation" (parent gets View Access by default) — the grant row really does get created — but it stays a visible, ordinary, revocable `ParentAccessGrant`, not a side effect hidden inside organization creation. Matters if something later creates Organizations outside this UI (a future import tool, say) — it would need to remember to call `GrantDefaultParentAccessAsync` itself; the default isn't enforced at the data layer.

### Existing Data Backfill: what actually got backfilled, and how

Implemented as `SeedData.BackfillOrganizations`, called from `SeedData.EnsureSeedData`. Two things worth being explicit about:

1. **Wired into normal app startup, not just the setup wizard.** `SeedData.EnsureSeedData` was previously only reachable through `SetupService`'s wizard flow — fine for a brand-new install, useless for backfilling a database that predates this phase (like the dev database this was built against, which already had a real Division and two Local Units). Added a call in `Program.cs`'s existing startup migration-check block, right after pending migrations apply. It's fully idempotent (existence-checked throughout), so this is safe on every restart.
2. **Backfilled Levels mirror today's real two-level `SiteType` shape exactly** — "Division" (Rank 1) and "Local Unit" (Rank 2), both site-eligible — not the richer five-level example (National/State/Region/Council/Unit) used for validation below. A backfill shouldn't guess at a hierarchy an admin hasn't actually configured; it should reproduce what's really there today, one-for-one. An admin can insert Levels above "Division" later without moving anything — `Rank` is just an integer, not a fixed-size list.
3. **The Portal site is excluded** — it isn't a community in this model, it's the neutral hub. No Organization gets created for it.

Verified against the real dev database: the existing "Virginia Beach Council of PTAs" Division and its two Local Units ("Bayside Middle", "Luxford Elementary PTA") backfilled correctly, including the parent/child chain and the Site links.

---

## Validation approach (and why it's not a live browser click-through)

Phase 1's stated validation was "configure PTA/Scout/Billiards hierarchies through Global Admin." The pages exist and are wired up (`/globaladmin/organizations/**`), but driving them through an actual browser session requires signing in — and this app's only login path is passwordless email codes sent via real SMTP (`EmailLoginSender`), with the one-time code held only in an in-memory `PasswordlessCodeStore`. There's no dev-mode bypass and no way to read a generated code from outside the running process without adding a temporary debug hook — which felt like more risk (something forgotten and left in place) than it was worth for this pass.

**What was actually validated instead:** a standalone harness (outside the repo, in the scratch directory) referencing the real `OrganizationService` — the exact class the UI calls — running against the real dev database. It:

- Created "Scouts" and "Billiards Community" Organization Types alongside the existing "PTA" one.
- Created genuinely different-depth Level sets for all three (5 levels for PTA, 4 for Scouts, 4 for Billiards).
- Built a 3-node parent/child chain for each, confirming the default View-access grant fires correctly on every child creation.
- Attempted an invalid placement (a Unit as the parent of a State) and confirmed it's correctly rejected.
- Created two `OperationalCycle` rows with genuinely different shapes (a PTA School Year with real July–June dates; a Billiards League Session with real September–December dates) through the identical entity/service, no branching.

All checks passed, with **no code differences required between the three Organization Types** — the actual bar Phase 1 set. Separately, an anonymous HTTP smoke test confirmed all five new Global Admin routes return a clean 302 (redirect to login) rather than a server error, and the migration applied cleanly to the real database.

This is a real exercise of the production code path and schema, not a mock — but it is not the same as someone clicking through the UI in a browser. If/when there's a way to authenticate non-interactively (a dev-only login bypass, a test SMTP catcher), a real click-through pass is still worth doing before calling this phase fully closed.

**Byproduct:** the dev database now has this validation data left in it — "Scouts" and "Billiards Community" Organization Types, their Levels, and sample Organizations/Operational Cycles under them — visible (and deletable) through Global Admin → Organizations. Left in deliberately, as tangible proof the framework holds up across genuinely different org shapes, per Phase 1's "Organization Framework Test Configurations" deliverable. Delete through the UI whenever it's no longer wanted.

---

## What's still open after this phase

Everything Phase 1 itself deferred (membership migration, site resolution rewiring, Theme/Tool/Event Framework, SignalR, Controllers) is unchanged — see Phase 1's own "Deferred To Future Phases." Additionally, from this implementation pass specifically:

- A real Global Admin login smoke test, once a non-interactive auth path exists.
- Per-Organization Operational Cycle override, if/when membership migration actually needs it.
- A proper hierarchy tree/visual builder for the Organizations page — today it's a sortable flat list (Level Rank, then Name), which was enough to prove the model but isn't a great editing experience for a deep hierarchy.
