# **CLAUDE.md (Updated with Generic Portal + Rich PortalHome Requirements)**

# VBPTA Portal — Architecture & Conventions

These rules govern all scaffolding, page generation, navigation, permissions, and terminology for this project. Apply them to all future code suggestions unless the user explicitly overrides them.

## **0. Project Identity (Important)**

The system originally began as a **VBPTA‑specific portal**, but has now evolved into a **generic Division/Local Unit portal** designed for:

- PTA groups  
- school organizations  
- booster clubs  
- councils  
- community groups  
- any future organization that needs a Division → Local Unit structure  

The **Portal site** is the neutral, central hub that introduces the platform, explains its purpose, and guides visitors to Divisions, Local Units, and tools.  
It must **not** be branded as “VBPTA Portal.”  
It must feel **inclusive**, **organizational**, and **open to all groups**.

---

## **1. Site Hierarchy**

Only two site types exist (SiteEngine/Enums/SiteType.cs):

Portal → Division → Local Unit

Division — e.g., Chesapeake, Norfolk, VB  
Local Unit — e.g., Luxford ES, Tallwood ES  
“Group” is never a site type. Always say “Division” when referring to a site.

---

## **2. Permission Groups**

Groups are permission bundles, not sites.  
They are assigned to users within a site’s scope and are unrelated to the site hierarchy.

Examples: Unit Admins, Division Admins, Event Managers, Membership Importers, Newsletter Editors, Finance Viewers, tool‑scoped groups (e.g., Bingo.Caller).

---

## **3. User Modes**

Anonymous  
Authenticated (Guest)  
Member  
Admin  
SuperAdmin

(Your previous descriptions remain unchanged.)

---

## **4. Navigation by Mode**

(Your previous navigation table remains unchanged.)

---

## **5. Global Admin Navigation**

(Your previous list remains unchanged.)

---

## **6. PortalTools Pattern**

(Your previous description remains unchanged.)

---

## **7. Page Folder Structure (Important)**

SharedPages/  
PortalPages/  
DivisionPages/  
UnitPages/  
Tools/<ToolName>/  

(Your previous folder descriptions remain unchanged.)

---

## **SharedPages/Home.razor**

SharedPages/Home.razor is the universal entry point for all site types.  
It detects the current SiteType and loads:

PortalHome.razor  
DivisionHome.razor  
UnitHome.razor

SharedPages/Home.razor must not contain site‑specific content directly.

---

# **PortalPages/PortalHome.razor (Expanded Requirements)**

PortalHome.razor is the home component for the **Central Portal**, the main entry point for PTA groups, school organizations, and community groups.

This page must be **rich, descriptive, and welcoming**.  
It should clearly explain what the platform is, who it serves, and how it works — **without requiring the user to click anything**.

### **Anonymous users should see a flowing, multi‑section introduction including:**

• A welcoming headline introducing the Central Portal  
• A short narrative explaining that the platform supports PTA groups, school organizations, booster clubs, councils, and community groups  
• A description of the Division → Local Unit structure  
• A section explaining what visitors can do here:  
 – browse Divisions and Local Units  
 – explore public information  
 – learn about tools  
 – understand how memberships work  
• A section explaining what registered users can do:  
 – apply for Division or Local Unit sites  
 – access member‑only content  
 – use tools  
 – manage their memberships  
• Clear call‑to‑action buttons:  
 – Browse Unit Sites  
 – Explore Tools  
 – Login / Create Account

### **Authenticated users should also see:**

• A “Go to Dashboard” button  
• A brief explanation of what the Dashboard provides

### **Styling and structure requirements:**

• Use headings, subheadings, and short paragraphs  
• Use friendly, inclusive language  
• Use placeholder text where needed  
• Do not implement business logic  
• Do not hard‑code VBPTA branding  
• The page should feel like a real landing page, not a placeholder

---

## 8. First Tool: GiveBacks Membership Import Tool

Purpose:  
Import a CSV of member emails for a Local Unit for the current school year (GiveBacks CSV export).

ToolScope: LocalUnit  
Access: Local Unit Admins only

Behavior:  
Upload CSV → parse emails → create/update membership records → mark admins if CSV includes admin emails → log import history

Folder structure:  
Tools/GiveBacksImportTool/  
 ImportPage.razor  
 ImportService.cs  
 ImportModel.cs

PortalTools entry:  
ToolName “GiveBacks Membership Import Tool”,  
ToolScope LocalUnit,  
Category Membership,  
IconClass fa-solid fa-file-import
