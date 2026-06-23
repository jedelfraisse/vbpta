# PTA Platform — Multi‑Division Website Engine & Public Tool Hub  
**Status: Active Development (Code Upload Pending)**

This repository contains the next‑generation PTA platform originally created for the **Virginia Beach Council of PTAs (VBCPTA)** and now evolving into a **multi‑division, multi‑unit website engine** with a **public‑facing tool hub**.

> **Note:**  
> The repository currently contains only documentation (`README.md`, `.gitignore`, `LICENSE`).  
> The application code will be uploaded once the core system reaches a stable baseline.

The platform is designed to support:

- **Division PTA councils**  
- **Local PTA units** under each division  
- **Independent PTAs** that do not belong to a division  
- **Public users** who want access to PTA‑friendly tools  

It is fully open source and will support both self‑hosting and managed SaaS deployments.

---

## 🌟 Overview

This project is evolving into a **PTA digital infrastructure platform** with three major layers:

### **1. Portal (Top Level)**  
The main public‑facing site that hosts tools, login, and shared resources.

### **2. Divisions (Middle Level)**  
Division PTA councils, each with their own site, announcements, and calendar.

### **3. Units (Bottom Level)**  
Local PTA units under each division, each with their own customizable site.

This structure mirrors how PTAs operate in many states while remaining flexible enough for independent PTAs.

---

## 🎯 Purpose

This platform exists to:

- Strengthen communication across divisions  
- Provide a consistent, professional web presence for all units  
- Support independent PTAs that report directly to state or national  
- Offer tools that any PTA — or even non‑PTA school groups — can use  
- Help new PTAs get started quickly  
- Support continuity as board members change  
- Reduce reliance on expensive third‑party services  
- Provide a free, open‑source option for PTAs that want to self‑host  

---

## 🔗 Integrating Existing PTA Tools (GiveBacks, SignUpGenius, etc.)

This platform is designed not only to **create new PTA tools**, but also to **integrate and enhance the tools PTAs already use today**.

Many PTAs rely on:

- **GiveBacks** for membership, fundraising, and ticketing  
- **SignUpGenius** for volunteer coordination  
- **Google Workspace** for documents and collaboration  
- **Mailchimp / Canva** for communication and design  

The platform’s mission is to:

- Work *with* these tools, not replace them  
- Provide a unified place to access them  
- Offer optional replacements when helpful  
- Fill the gaps where existing tools fall short  

---

## 🏛️ Features for Divisions (PTA Councils)

- Division‑level announcements  
- Shared district calendar  
- Awards & recognition  
- Document library  
- Links to state and national PTA resources  
- Division‑wide event listings  
- Multi‑unit management dashboard  
- Support for elections, committees, and transitions  

---

## 🏫 Features for Local PTA Units

Each unit (school‑level PTA) will have its own site under its division:

```
division.localhost
unit.division.localhost
```

or in staging:

```
division.ptaportal.delfraisse.com
unit.division.ptaportal.delfraisse.com
```

Unit sites include:

- Customizable logo, colors, and branding  
- Unit‑specific announcements  
- Unit‑level event calendar  
- Automatic inclusion of unit events in the division calendar  
- Document storage  
- Optional election tools  
- Simple admin interface for board members  
- Year‑to‑year continuity helpers  

---

## Independent PTAs (No Division)

Some PTAs do not belong to a division and report directly to:

- State PTA  
- National PTA  
- Or operate independently  

These PTAs can:

- Use the **public tool hub**  
- Host their own unit site under the portal  
- Or use their own custom domain  

This is why the **Portal** hosts the tools — so *any* PTA or school group can benefit, even without a division structure.

---

## Domain Structure and Flexibility

To keep things simple and predictable, the platform uses the following domain model:

### **Development Environment**
```
https://localhost
https://division.localhost
https://unit.localhost
```

### **Staging Environment**
```
https://ptaportal.delfraisse.com
https://division.ptaportal.delfraisse.com
https://unit.ptaportal.delfraisse.com
```

### **Custom Domains**
Each site (division or unit) may optionally attach a custom domain:

```
vbpta.org
luxfordpta.org
```

However:

> **All sites will always remain accessible via their platform subdomain**  
> (e.g., `unit.ptaportal.delfraisse.com`),  
> even when a custom domain is attached.

This ensures:

- Reliable routing  
- Consistent admin access  
- No dependency on external DNS  
- Easier debugging and support  

---

## 🔧 Public Tool Hub (New)

The main portal includes a login system where users can access PTA‑friendly tools such as:

### Available / Planned Tools

- **Bingo Runner**  
- **Digital Raffle Picker**  
  - Includes “last pick wins” animation  
- **Volunteer / Event Helpers**  
- **Randomized student or ticket selectors**  
- **Silent auction helpers**  
- **Future PTA utilities as they are developed**  

---

## 🔐 Roles & Permissions

- **Portal Admins** — manage the entire platform  
- **Division Admins** — manage division sites and oversee units  
- **Unit Admins** — manage their school’s website and events  
- **Board Members** — access continuity and transition tools  
- **Public Users** — log in to use helper tools  

---

## 🗺️ Roadmap

*(Roadmap unchanged — still accurate for pre‑code state.)*

---

# 🛠️ Development Setup (Pre‑Code Upload)

The codebase is currently being prepared for upload.  
This section describes the **intended development workflow** and the behavior of the platform’s **first‑run setup system**.

This project uses:

- **.NET 9**  
- **Blazor**  
- **SQL Server**  
- **EF Core**  
- **Passwordless authentication**  
- **(Coming Soon) Docker via .NET Aspire**  

---

## Local Development (Planned)

During development, the solution will include:

- **WebApp** — Blazor UI  
- **SiteEngine** — EF Core models, migrations, and site resolution logic  

There is **no default database name** and **no preconfigured connection string**.  
The application does **not** create or connect to a database until the developer provides the connection details.

---

## First‑Run Setup (SQL Configuration)

When the site is launched for the first time:

1. The application checks `appsettings.json`  
2. If `ConnectionStrings:DefaultConnection` is **blank or missing**  
3. The **SetupSetup** screen automatically appears  
4. The developer enters SQL Server connection information  
5. The connection string is saved to configuration  
6. The database is created  
7. EF Core migrations are applied automatically  

This ensures:

- No accidental connections to the wrong SQL instance  
- No reliance on LocalDB or hard‑coded defaults  
- A consistent onboarding experience for all developers  
- A smooth transition to Aspire‑managed SQL containers later  

Once Aspire is implemented, it will automatically provision and configure the SQL container, replacing the manual setup step.

---

## SMTP & Developer Login Testing (Recommended)

The platform uses **passwordless authentication**, meaning users log in by receiving a one‑time email link.

During development, the application behaves exactly as it does in production:

- It sends real passwordless login emails  
- It uses the configured SMTP server  
- It expects the developer to click the login link  

To avoid sending real emails, developers can run **smtp4dev**, a lightweight local SMTP server that captures outgoing messages for inspection.

When smtp4dev is running:

- The app “thinks” it is sending real emails  
- smtp4dev receives them instantly  
- Developers open the email in smtp4dev’s web UI  
- Click the login link  
- And authenticate normally  

This provides a **realistic login flow** without requiring external email services.

smtp4dev will also be added to the Aspire environment once Aspire is configured.

**smtp4dev GitHub:**  
[https://github.com/rnwood/smtp4dev](https://github.com/rnwood/smtp4dev)

---

## Testing the Published Version (No IIS Required)

A batch file will be included to:

- Publish the site  
- Run the published output locally  
- Avoid IIS entirely  

This allows developers to test the **actual published build** exactly as it will run in production.

---

## 🤝 Contributing

Contributions are welcome once the initial codebase is uploaded.

---

## 📄 License

This project is licensed under the **MIT License**.  
See the `LICENSE` file for details.
