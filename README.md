# Virginia Beach Council of PTAs Website Platform (vbpta.org)
**Status: Draft / Proposal**

This repository contains the proposed new website platform for the **Virginia Beach Council of PTAs (VBCPTA)**.

The development version is currently hosted at:

https://vbpta.delfraisse.com

Once approved and finalized, the official production site will be published at:

https://vbpta.org

This project is a fresh rebuild, informed by archived versions of vbpta.org but not dependent on them. It includes both the **citywide PTA website** and a **multi‑unit website engine** that allows local PTA units to create customizable, template‑based sites.

---

## 🌟 Purpose

This platform provides:

- A modern, unified online home for the Virginia Beach Council of PTAs  
- A shared district‑wide calendar for school closures, holidays, and major events  
- A hub for council announcements, awards, and GiveBacks information  
- A simple, customizable website option for local PTA units  
- Easy access to Virginia PTA and National PTA resources  
- A consistent, professional platform for families and volunteers across the city  

### Helping Schools Build and Sustain Strong PTAs

Many schools want an active PTA but don’t know where to start, how to organize, or how to continue year after year.  
This platform provides a clear, structured foundation that helps new PTAs get started quickly and helps existing PTAs maintain continuity even as volunteers and board members change.

---

## 🏛️ Features for the Council Website

- Citywide announcements & updates  
- Shared district calendar (closures, holidays, council events)  
- Awards & recognition for local units  
- GiveBacks integration for membership and fundraising  
- Links to state and national PTA resources  
- Document library for bylaws, forms, guides, and meeting materials  
- Council‑level event listings  
- Support for elections, committees, and board transitions  

---

## 🏫 Features for Local PTA Units

Local units can create their own website under the VBCPTA umbrella, such as:

```
schoolname.vbpta.org
```

Each unit site includes:

- A template‑based design with customizable logo, colors, and branding  
- A unit‑specific calendar for school events, PTA meetings, and fundraisers  
- Automatic inclusion of unit events in the district‑wide shared calendar  
- Announcements, event pages, and document storage  
- Optional election tools  
- Integration with GiveBacks and SignUpGenius (pending API availability)  
- A simple admin interface for board members and volunteers  

---

## 🔐 Roles & Permissions

- **Council Admins** – manage the citywide site and oversee unit sites  
- **Unit Admins** – manage their school’s website, events, and documents  
- **Board Members** – access to year‑to‑year transition tools  
- **Volunteers** – limited access for event or content contributions  

A dedicated **Board Admin Section** helps units transition smoothly between school years.

---

## 🗺️ Roadmap

This project is in active development. The following roadmap outlines the planned phases for the Virginia Beach Council of PTAs website platform.

### Phase 1 — Core Council Website (In Progress)
- Council homepage and announcements  
- Shared district‑wide calendar  
- Links to Virginia PTA and National PTA  
- Awards and recognition listings  
- Document library  
- Development hosting at https://vbpta.delfraisse.com  

### Phase 2 — Multi‑Unit Website Engine
- Subdomain support (schoolname.vbpta.org)  
- Template‑based unit websites with customizable branding  
- Unit‑level announcements and event calendars  
- Automatic inclusion of unit events in the district calendar  
- Unit admin dashboard and permissions  

### Phase 3 — Board & Continuity Tools
- Year‑to‑year transition checklists  
- Officer role management  
- Document archiving and rollover helpers  
- Election tools (optional)  

### Phase 4 — Integrations & Engagement Tools
- SignUpGenius integration (pending API availability)  
- GiveBacks integration  
- Optional volunteer/event helper tools  

### Phase 5 — Public Launch
- Final deployment to https://vbpta.org  
- Council approval and onboarding of local units  
- Documentation and support materials  

### Future Enhancements (Exploratory)
- PTA health dashboard for units  
- Optional financial tools to complement or replace MoneyMinder  
- Mobile‑friendly dashboards for board members  
- Automated reminders for state‑level deadlines  

---

## 🤝 Contributing

Contributions are welcome and encouraged — and they are **not limited to code**.

This project supports the Virginia Beach Council of PTAs and its local units, so community input is incredibly valuable. Whether you're a developer, a PTA volunteer, a parent, or someone with ideas to improve the platform, your contributions matter.

### Ways to Contribute
- **Submit ideas** for new features or improvements  
- **Report issues** such as bugs, typos, or confusing pages  
- **Suggest enhancements** to usability, accessibility, or design  
- **Share feedback** on workflows or user experience  
- **Help refine documentation**  
- **Contribute code** (new features, fixes, refactoring, etc.)  

### How to Contribute
1. Go to the **Issues** tab in GitHub  
2. Create a new issue for:
   - Ideas  
   - Feature requests  
   - Bug reports  
   - Questions  
   - Suggestions  
3. If you're contributing code:
   - Fork the repository  
   - Create a feature branch:  
     `git checkout -b feature/my-improvement`  
   - Commit your changes with clear messages  
   - Push the branch:  
     `git push origin feature/my-improvement`  
   - Open a Pull Request describing your changes  

Please keep PRs focused and readable.  
All contributions — big or small — help make this platform better for every PTA unit in Virginia Beach.

---

## 🛠️ Development Setup

This project uses:

- **.NET 9**  
- **Blazor**  
- **SQL backend**  
- **Docker** (managed automatically by Aspire)  
- **.NET Aspire** for local orchestration and developer onboarding  

### Current local setup (tenant host + EF Core)

The active solution projects are:

- `WebApp` (Blazor host/UI)
- `SiteEngine` (entities, EF Core DbContext, site resolution, scoped services)

Local development uses SQL Server LocalDB by default:

- `WebApp/appsettings.Development.json` → `ConnectionStrings:DefaultConnection`
- default DB name: `VbptaWeb_Dev`

Create and apply migrations:

```bash
dotnet ef migrations add InitialCreate --project SiteEngine\SiteEngine.csproj --startup-project WebApp\WebApp.csproj
dotnet ef database update --project SiteEngine\SiteEngine.csproj --startup-project WebApp\WebApp.csproj
```

Automatic migration trigger marker:

- Place `run-migration.txt` at `WebApp/wwwroot/run-migration.txt`
- On startup, the app applies migrations and renames the marker to:
  - `run-migration.<timestamp>.done` on success
  - `run-migration.failed.<timestamp>.txt` on failure

Passwordless sign-in (email code) setup:

- Login page: `/login`
- Logout page: `/logout`
- Admin page requires both:
  - admin hostname context
  - authenticated user session
- Configure SMTP in `WebApp/appsettings*.json` under `EmailLogin`.
- If `EmailLogin:SmtpHost` is empty, sign-in codes are logged locally for development.

Initial platform setup and routing:

- If `Sites` is empty, requests are redirected to `/setup` to bootstrap:
  - admin email
  - city-wide site name + PTA ID
  - root/platform domains
  - SMTP settings
- Domain routing now resolves from DB-backed `GlobalConfig` and `Sites`:
  - `admin.<domain>` → admin site (`PtaId = 00000000`)
  - `<root domain>` → city-wide site (`IsCityWide = true`)
  - `<sub>.<platform domain>` → site by `Hostname`
  - custom host match → site by `Domain`
  - unknown host → city-wide fallback with not-found state

Per-site public asset folders:

- Site-specific public files are stored under:
  - `WebApp/wwwroot/site-data/{hostname}/images/`
- On startup, the app ensures these folders exist for all sites in the `Sites` table.
- New site creation also ensures folder creation automatically.
- If site creation uses default logo/banner values, defaults are copied to:
  - `logo.png`
  - `banner.png`

### Prerequisites
- .NET 9 SDK  
- Docker Desktop  
- Git  
- VS Code or Visual Studio 2022+  
- Node.js (optional frontend tooling)  

### Clone the Repository
```
git clone <repo-url>
cd vbpta.org
```

### Running the Development Environment

This project uses **.NET Aspire** to orchestrate the entire development environment.

Aspire will automatically:

- Start the SQL database container  
- Inject connection strings  
- Manage environment variables  
- Provide a dashboard to start/stop services  
- Show logs and health checks  

To start the full environment:

```
dotnet run --project VBCPTA.AppHost
```

Then open the Aspire dashboard (URL shown in console) to:

- View running services  
- Start/stop the SQL container  
- Inspect logs  
- Manage configuration  

### Database Setup

No manual Docker commands are required.  
Aspire handles:

- Container creation  
- Startup  
- Shutdown  
- Networking  
- Environment variables  

EF Core migrations can be applied manually if needed:

```
dotnet ef database update --project VBCPTA.Data
```

### Project Structure (Proposed)
```
/src
  /VBCPTA.Web           → Blazor UI
  /VBCPTA.Api           → API endpoints (if separated)
  /VBCPTA.Data          → EF Core models & migrations
  /VBCPTA.Core          → Shared logic & services
  /VBCPTA.AppHost       → Aspire orchestration

/tests
  /VBCPTA.Tests
```

### Environment Variables

Create a `.env` file if needed:

```
SIGNUPGENIUS_API_KEY=<your-key>
GIVEBACKS_API_KEY=<your-key>
```

### Hot Reload

```
dotnet watch --project VBCPTA.Web
```

---

## 📄 License

This project is licensed under the **MIT License**.  
See the `LICENSE` file for details.
