# SPEC — マイクロテクノ株式会社 Website (.NET Core)

## 1. Objective

Rebuild the **マイクロテクノ株式会社** (Micro Techno Co., Ltd.) corporate website as an ASP.NET Core MVC application.

- **Pixel-perfect match** with the existing mockup HTML/CSS/JS (already scraped under `mockup/`)
- **Dynamic content management** via an admin panel backed by SQL Server
- **Trilingual support** (Japanese / English / Vietnamese) — all content editable per language
- **Contact form** submissions saved to the database (viewable in admin)

**Target users**:
- Public visitors — read-only company website
- Internal staff / admin — manage content via `/admin` panel (login required)

---

## 2. Pages & Routes

| Route | Page | Notes |
|---|---|---|
| `/` | Home | Hero slider, news highlights, product intro, CTA |
| `/products` | Products | Grid of product cards (image, title, spec fields) |
| `/about-us` | About Us (JA) | Company philosophy, quality policy, environment policy |
| `/company-en` | About Us (EN) | English version of about page |
| `/company-vi` | About Us (VI) | Vietnamese version of about page |
| `/company` | Company Overview | Corporate profile table |
| `/locations` | Locations | Office/factory list with map embeds |
| `/news` | News List | Paginated, filterable by category |
| `/news/{id}` | News Detail | Individual article |
| `/news/category/{slug}` | News by Category | お知らせ / 展示会出展 / 技術更新 |
| `/purchase` | Purchase Policy | Static policy page |
| `/action-plan` | Action Plan | Static page |
| `/contact` | Contact Form | Form → save to DB → redirect to `/thanks` |
| `/thanks` | Thank You | Post-contact confirmation page |
| `/privacy` | Privacy Policy | Static page |
| `/security` | Security Policy | Static page |
| `/sitemap` | Sitemap | Auto-generated list of all pages |
| `/admin` | Admin Dashboard | Protected by Identity login |

---

## 3. Tech Stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB for dev, full SQL Server for prod) |
| Auth | ASP.NET Core Identity (admin-only, no public login) |
| Frontend | Static assets from mockup (CSS/JS/images served from `wwwroot/`) |
| Image storage | Local filesystem (`wwwroot/uploads/`) |
| Email | Not required (contact saved to DB) |

---

## 4. Project Structure

```
Mitech/
├── mockup/                        # Original scraped HTML (reference only)
├── SPEC.md
└── src/
    └── Mitech.Web/                # Single ASP.NET Core MVC project
        ├── Controllers/
        │   ├── HomeController.cs
        │   ├── ProductsController.cs
        │   ├── AboutController.cs
        │   ├── CompanyController.cs
        │   ├── LocationsController.cs
        │   ├── NewsController.cs
        │   ├── ContactController.cs
        │   ├── StaticPagesController.cs  # purchase, action-plan, privacy, security, sitemap
        │   └── Admin/
        │       ├── AdminBaseController.cs
        │       ├── DashboardController.cs
        │       ├── ProductsAdminController.cs
        │       ├── NewsAdminController.cs
        │       ├── PagesAdminController.cs
        │       └── ContactAdminController.cs
        ├── Models/
        │   ├── Domain/
        │   │   ├── Product.cs
        │   │   ├── NewsArticle.cs
        │   │   ├── NewsCategory.cs
        │   │   ├── PageContent.cs     # key-value CMS for editable text
        │   │   ├── ContactMessage.cs
        │   │   └── SiteImage.cs
        │   └── ViewModels/
        │       ├── HomeViewModel.cs
        │       ├── ProductsViewModel.cs
        │       ├── NewsListViewModel.cs
        │       ├── NewsDetailViewModel.cs
        │       └── ContactViewModel.cs
        ├── Views/
        │   ├── Shared/
        │   │   ├── _Layout.cshtml          # Main layout (header + nav + footer)
        │   │   ├── _AdminLayout.cshtml     # Admin layout
        │   │   └── _DrawerMenu.cshtml
        │   ├── Home/Index.cshtml
        │   ├── Products/Index.cshtml
        │   ├── About/Index.cshtml
        │   ├── Company/Index.cshtml  (+ En.cshtml, Vi.cshtml)
        │   ├── Locations/Index.cshtml
        │   ├── News/ (Index, Detail, Category)
        │   ├── Contact/ (Index, Thanks)
        │   ├── StaticPages/ (Purchase, ActionPlan, Privacy, Security, Sitemap)
        │   └── Admin/
        │       ├── Dashboard/Index.cshtml
        │       ├── Products/ (Index, Create, Edit)
        │       ├── News/ (Index, Create, Edit)
        │       ├── Pages/ (Index, Edit)
        │       └── Contact/Index.cshtml
        ├── Data/
        │   ├── ApplicationDbContext.cs
        │   └── Migrations/
        ├── Services/
        │   ├── IPageContentService.cs    # reads PageContent by key + lang
        │   └── PageContentService.cs
        ├── wwwroot/
        │   ├── css/         # copied from mockup theme
        │   ├── js/          # copied from mockup theme
        │   ├── images/      # copied from mockup theme
        │   └── uploads/     # runtime image uploads
        ├── appsettings.json
        └── Program.cs
```

---

## 5. Data Models

### `Product`
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| TitleJa / TitleEn / TitleVi | string | Product category name |
| SpecName | string | e.g. "ボディ" |
| SpecMaterial | string | e.g. "アルミ6000系" |
| SpecDimension | string | e.g. "φ32" or "70mm" |
| ImagePath | string | relative path under wwwroot/uploads/ |
| SortOrder | int | display sequence |
| IsActive | bool | |

### `NewsCategory`
| Field | Type |
|---|---|
| Id | int |
| Slug | string | e.g. "information", "event", "technology" |
| NameJa / NameEn / NameVi | string |

### `NewsArticle`
| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| TitleJa / TitleEn / TitleVi | string | |
| BodyJa / BodyEn / BodyVi | string (HTML) | rich text |
| CategoryId | int | FK → NewsCategory |
| PublishedAt | DateTime | |
| IsPublished | bool | |
| CreatedAt | DateTime | |

### `PageContent`
Key-value store for all editable text snippets on static pages.

| Field | Type | Notes |
|---|---|---|
| Id | int | PK |
| PageKey | string | e.g. "home.hero.title" |
| Lang | string | "ja", "en", "vi" |
| Value | string (HTML allowed) | |

### `ContactMessage`
| Field | Type |
|---|---|
| Id | int |
| CompanyName | string |
| ContactName | string |
| Email | string |
| Phone | string |
| Inquiry | string (text) |
| ReceivedAt | DateTime |
| IsRead | bool |

---

## 6. Multilingual Strategy

- Lang is passed via route prefix or query string: `/en/products`, `/vi/news` — **or** via a language switcher cookie/session (match mockup's approach of separate pages per lang for about-us).
- `IPageContentService.Get(key, lang)` reads from `PageContent` table with fallback to `"ja"` if translation is missing.
- Razor views use `@inject IPageContentService Content` and call `@Content.Get("home.headline", currentLang)`.
- News and Product titles/bodies have per-language columns on the entity itself (simpler than a separate translation table for this scale).
- The language for static pages (about-us, company-en, company-vi) follows the mockup: separate routes.

---

## 7. Admin Panel

- Route prefix: `/admin`
- Protected by `[Authorize(Roles = "Admin")]`
- Admin account seeded in `Program.cs` via `appsettings.json` (email + password)
- Features:
  - **Products**: CRUD, image upload, sort order drag-and-drop
  - **News**: CRUD with HTML body editor (use a simple `<textarea>` — no heavy WYSIWYG dependency required for v1)
  - **Page Content**: table of all `PageContent` keys, editable inline by language tab
  - **Contact Messages**: read-only list, mark as read/unread, delete

---

## 8. Static Assets Strategy

The mockup theme CSS/JS/images are already finalized. They will be copied verbatim:

- `mockup/wp/wp-content/themes/rcnt/css/` → `wwwroot/css/`
- `mockup/wp/wp-content/themes/rcnt/js/` → `wwwroot/js/`
- `mockup/wp/wp-content/themes/rcnt/images/` → `wwwroot/images/`
- `mockup/wp/wp-content/uploads/` → `wwwroot/uploads/`
- `mockup/wp/wp-content/plugins/` (fancybox, pagenavi CSS) → `wwwroot/css/plugins/`
- jQuery files from `mockup/wp/wp-includes/js/jquery/` → `wwwroot/js/jquery/`

Razor views reconstruct the HTML structure from the mockup exactly, replacing hard-coded content with `@Model` bindings.

---

## 9. Code Style

- **Language**: C# 12, nullable reference types enabled
- **Naming**: PascalCase for classes/properties, camelCase for locals
- **Views**: Razor `.cshtml`, no inline JavaScript beyond what mockup has
- **No comments** unless a constraint or workaround is non-obvious
- **No ViewBag** — use typed ViewModels for all data passed to views
- **No direct DbContext in controllers** — use service layer or repository pattern
- **EF Core**: code-first migrations; seed data in `Program.cs`
- **appsettings**: connection string + admin seed credentials only; no secrets in source

---

## 10. Testing Strategy

- **Unit tests**: Service layer (`PageContentService`, news filtering logic) — xUnit
- **Integration tests**: Not required for v1
- **Manual UI verification**: Each page visually compared to `mockup/*.html` in browser before marking complete

---

## 11. Boundaries

### Always do
- Match mockup HTML structure exactly — use same CSS class names as mockup
- Serve all existing CSS/JS/images without modification
- Validate contact form inputs server-side before saving
- Redirect POST → GET after form submission (PRG pattern)

### Ask before doing
- Adding third-party NuGet packages beyond EF Core + Identity
- Changing database schema after initial migration is created
- Any change that removes or renames existing mockup CSS classes

### Never do
- No public user registration — admin account only
- No client-side routing (SPA) — server-rendered pages only
- No sending actual emails (contact saves to DB only, for v1)
- No breaking changes to the visual design (CSS/JS from mockup is frozen)
