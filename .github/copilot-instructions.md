# Copilot Instructions for Central Portal

## Project context
- This solution uses **.NET 9**.
- Prefer patterns and examples aligned with **Blazor**.
- The primary goal is to implement a **tenant host** model where pages load data for a specific site/tenant.

## Current feature direction
- Add Entity Framework Core support for multi-site data access.
- Use a local SQL Server instance for local development.
- Introduce a default website that acts as a basic admin portal to monitor all websites.

## Architecture expectations
- Keep tenant/site concerns explicit in data models and queries.
- Avoid cross-tenant data leaks; always scope reads/writes by site unless in admin context.
- Keep changes minimal and consistent with existing code style.
- Prefer incremental implementation with clear milestones and migration steps.

## EF Core guidance
- Use EF Core code-first with explicit entities and relationships.
- Add migrations in small, reviewable increments.
- Seed a default admin site/tenant record for bootstrap scenarios.
- Keep connection string configuration environment-specific and safe for local dev.

## Local development defaults
- Assume local SQL Server for EF development.
- Keep developer setup documented when adding new required settings.

## Implementation style
- Favor simple, maintainable abstractions over premature complexity.
- Add tests for tenant scoping logic when feasible.
- Do not introduce unrelated refactors while implementing this feature set.
