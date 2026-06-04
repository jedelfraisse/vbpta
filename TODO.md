# **todo.md — Multi‑Tenant PTA Platform Setup + Routing (No Nulls Version)**

This file defines the required tasks for implementing dynamic multi‑tenant routing, initial setup, and the updated Site model.  
All fields under our control must be **NOT NULL**, using **empty strings** where appropriate.

---

## **1. Update the `Sites` Table**

Add the following fields (all NOT NULL):

### **New Columns**
| Column | Type | Null? | Default | Notes |
|--------|------|--------|---------|-------|
| `PtaId` | `char(8)` | **NOT NULL** | none | Always 8 digits, zero‑padded |
| `Domain` | `nvarchar(255)` | **NOT NULL** | `''` | Blank = no custom domain |
| `IsCityWide` | `bit` | **NOT NULL** | `0` | Only one site has `1` |

### **Existing Columns**
All existing columns should remain **NOT NULL** (as they already are).

---

## **2. Update the `Site` Entity Class**

Add:

```csharp
public string PtaId { get; set; } = "00000000";
public string Domain { get; set; } = string.Empty;
public bool IsCityWide { get; set; }
```

Ensure:

- `Hostname` remains NOT NULL (empty string allowed)
- No nullable reference types in this class

---

## **3. Add `GlobalConfig` Table (All NOT NULL)**

Fields:

| Field | Type | Null? | Default |
|-------|------|--------|---------|
| `RootDomain` | nvarchar(255) | **NOT NULL** | `''` |
| `PlatformDomain` | nvarchar(255) | **NOT NULL** | `''` |
| `SmtpHost` | nvarchar(255) | **NOT NULL** | `''` |
| `SmtpPort` | int | **NOT NULL** | `25` or `587` |
| `SmtpUsername` | nvarchar(255) | **NOT NULL** | `''` |
| `SmtpPassword` | nvarchar(255) | **NOT NULL** | `''` |
| `UseSsl` | bit | **NOT NULL** | `1` |

No nulls anywhere.

These values must be editable in the Admin site.

---

## **4. Initial Setup Wizard**

Displayed only when the Sites table is empty.

### Wizard collects:

1. **Admin Email** (NOT NULL)
2. **SMTP settings** (all NOT NULL)
3. **City‑Wide PTA Name** (NOT NULL)
4. **City‑Wide PTA ID (8 digits)** (NOT NULL)
5. **Root Domain Confirmation**  
   - Pre-filled from `Request.Host.Host`  
   - User may override  
   - Stored as NOT NULL

### After validation:

#### Seed Site 0 (Admin)
```
PtaId = "00000000"
Hostname = "admin"
Domain = ""
IsAdminSite = true
IsCityWide = false
```

#### Seed Site 1 (City‑Wide)
```
PtaId = <user input>
Hostname = ""
Domain = ""
IsAdminSite = false
IsCityWide = true
```

#### Save GlobalConfig
```
RootDomain = <detected or user-confirmed>
PlatformDomain = <same as RootDomain unless overridden>
```

All fields stored as NOT NULL.

---

## **5. Implement Dynamic Routing Middleware**

Routing must be **fully dynamic**.  
No hard-coded domains.

### Routing Rules:

```
host = Request.Host.Host
root = GlobalConfig.RootDomain
platform = GlobalConfig.PlatformDomain
```

### 1. Admin Site
```
if host starts with "admin.":
    return Site where PtaId == "00000000"
```

### 2. City‑Wide Site
```
if host == root:
    return Site where IsCityWide == true
```

### 3. Subdomain Routing (platform domain)
```
if host ends with platform:
    sub = extract leftmost label
    site = find Site where Hostname == sub
    if site exists:
        return site
    else:
        return CityWide with "Not found"
```

### 4. Custom Domain Routing
```
site = find Site where Domain == host
if site exists:
    return site
```

### 5. Fallback
```
return CityWide with "Not found"
```

---

## **6. Admin Site Settings Page**

Add UI to modify:

- RootDomain  
- PlatformDomain  
- SMTP settings  
- City‑wide PTA info  
- Logo, colors, etc. (already supported)

All fields must remain NOT NULL.

---

## **7. Testing Checklist**

### Domain Routing
- `admin.<domain>` → Admin site  
- `<root domain>` → City‑wide site  
- `<sub>.<platform domain>` → Local unit  
- Unknown subdomain → City‑wide + “Not found”  
- Custom domain → Correct local unit  

### Setup Wizard
- Runs only when Sites table is empty  
- Correctly seeds Site 0 and Site 1  
- Stores GlobalConfig values  
- Validates SMTP  

### Database
- Migration adds PtaId, Domain, IsCityWide  
- All fields NOT NULL  
- Domain uses empty string instead of null  
- PtaId always 8 digits  

---

## **8. Future Enhancements (Optional)**

- Add “Site Not Found” banner component  
- Add domain verification for custom domains  
- Add automatic SSL provisioning (Let's Encrypt)  
- Add multi‑city support (future multi‑tenant layer)
