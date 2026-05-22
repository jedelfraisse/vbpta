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

Contributions are welcome and encouraged. This project supports the Virginia Beach Council of PTAs and its local units, so clarity, accessibility, and maintainability are priorities.

### Ways to Contribute
- Fix bugs or improve existing features  
- Add new components or UI improvements  
- Improve documentation  
- Help refine the multi‑unit engine  
- Assist with integrations (SignUpGenius, GiveBacks, etc.)  

### Contribution Process
1. Fork the repository  
2. Create a feature branch:  
   `git checkout -b feature/my-improvement`  
3. Commit your changes with clear messages  
4. Push the branch:  
   `git push origin feature/my-improvement`  
5. Open a Pull Request describing your changes  

Please keep PRs focused and readable.

---

## 🛠️ Development Setup

This project uses:

- **.NET 9**  
- **Blazor**  
- **SQL backend**  
- **Docker** (managed automatically by Aspire)  
- **.NET Aspire** for local orchestration and developer onboarding  

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
