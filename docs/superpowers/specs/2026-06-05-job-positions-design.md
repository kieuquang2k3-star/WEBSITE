# Job Positions — Design Spec
**Date:** 2026-06-05
**Scope:** DB-driven job positions with slider on /tuyen-dung, detail page per position, admin CRUD.

---

## Overview

Replace the hardcoded job position cards in `TuyenDung.cshtml` with a database-driven slider. Admin can add/edit/delete positions. Each position has a detail page at `/tuyen-dung/{slug}`. The slider shows 3 cards with prev/next buttons, implemented in vanilla JS + CSS (no external library).

---

## Data Model — `JobPosition`

```csharp
public class JobPosition
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;      // URL slug, unique, auto-gen from TitleVi

    public string TitleVi  { get; set; } = string.Empty;
    public string TitleJa  { get; set; } = string.Empty;
    public string TitleEn  { get; set; } = string.Empty;

    public string ShortDescVi { get; set; } = string.Empty;  // shown on card
    public string ShortDescJa { get; set; } = string.Empty;
    public string ShortDescEn { get; set; } = string.Empty;

    public string DetailVi { get; set; } = string.Empty;     // full HTML content on detail page
    public string DetailJa { get; set; } = string.Empty;
    public string DetailEn { get; set; } = string.Empty;

    public string SalaryVi { get; set; } = string.Empty;     // salary/requirements text
    public string SalaryJa { get; set; } = string.Empty;
    public string SalaryEn { get; set; } = string.Empty;

    public int  SortOrder { get; set; }
    public bool IsActive  { get; set; } = true;
}
```

### ApplicationDbContext changes

Add to `ApplicationDbContext.cs`:
```csharp
public DbSet<JobPosition> JobPositions => Set<JobPosition>();
```

Add index on `SortOrder` and unique index on `Slug` in `OnModelCreating`.

### EF Migration

Run: `dotnet ef migrations add AddJobPositions` then `dotnet ef database update`.

---

## Slug Generation

Helper: `SlugHelper.Generate(string input)` — Vietnamese-aware, strips diacritics, replaces spaces with `-`, lowercases.

- Auto-generated from `TitleVi` when creating
- Editable in admin form
- Unique enforced at DB level (unique index) and validated in controller before save

---

## Public Routes

| Route | Controller | Action | Notes |
|---|---|---|---|
| `/tuyen-dung` | `HomeController` (or `StaticPagesController`) | `TuyenDung` | Existing, modified to pass jobs |
| `/tuyen-dung/{slug}` | `RecruitmentController` | `Detail` | New controller |

### `/tuyen-dung` changes

The existing `TuyenDung` action loads `db.JobPositions.Where(j => j.IsActive).OrderBy(j => j.SortOrder).ToListAsync()` and passes to the view via `ViewData["Jobs"]`.

The view replaces the hardcoded `<div class="job-cards">` section with a `<div class="job-slider">` containing the DB-driven cards.

### `/tuyen-dung/{slug}` — detail page

New `RecruitmentController` (non-admin, no auth) with action:
```csharp
public async Task<IActionResult> Detail(string slug)
{
    var job = await _db.JobPositions
        .Where(j => j.Slug == slug && j.IsActive)
        .FirstOrDefaultAsync();
    if (job == null) return NotFound();
    return View(job);
}
```

View: `Views/Recruitment/Detail.cshtml`
- Displays title, salary/requirements, full detail HTML (`@Html.Raw`)
- CTA: "Liên hệ ngay" buttons (tel: 0901589826, tel: 0868841651)
- Breadcrumb: HOME > Tuyển dụng > {title}

---

## Slider — Frontend

### HTML structure

```html
<div class="job-slider-wrap">
  <button class="job-slider-prev" aria-label="Previous">&#8249;</button>
  <div class="job-slider-viewport">
    <div class="job-slider-track">
      <!-- repeated per job -->
      <div class="job-slide">
        <a href="/tuyen-dung/{slug}" class="job-card">
          <p class="job-card__title">{title}</p>
          <p class="job-card__desc">{shortDesc}</p>
          <span class="job-card__cta">Xem chi tiết →</span>
        </a>
      </div>
    </div>
  </div>
  <button class="job-slider-next" aria-label="Next">&#8250;</button>
</div>
```

### CSS

- `.job-slider-viewport`: `overflow: hidden`
- `.job-slider-track`: `display: flex; transition: transform 0.35s ease`
- `.job-slide`: `flex: 0 0 calc(100% / 3)` (desktop), `flex: 0 0 100%` (mobile ≤600px)
- Prev/next buttons: position absolute or flex beside track

### JS (~30 lines, inline in `<script>` at bottom of TuyenDung.cshtml)

```js
const track = document.querySelector('.job-slider-track');
const slides = document.querySelectorAll('.job-slide');
let current = 0;
const visible = () => window.innerWidth <= 600 ? 1 : 3;
const max = () => Math.max(0, slides.length - visible());

function goTo(n) {
  current = Math.max(0, Math.min(n, max()));
  track.style.transform = `translateX(-${current * (100 / visible())}%)`;
}

document.querySelector('.job-slider-prev').addEventListener('click', () => goTo(current - 1));
document.querySelector('.job-slider-next').addEventListener('click', () => goTo(current + 1));
window.addEventListener('resize', () => goTo(Math.min(current, max())));
```

---

## Admin — `/admin/jobs`

### Route

```csharp
app.MapControllerRoute("admin-jobs", "admin/jobs/{action=Index}/{id?}",
    defaults: new { controller = "JobsAdmin" });
```

### Controller: `JobsAdminController`

Extends `AdminBaseController`. Actions: `Index`, `Create` (GET/POST), `Edit` (GET/POST), `Delete` (POST).

- `Index`: list all ordered by SortOrder, shows title (VI), active status
- `Create` POST: auto-generate slug from TitleVi if slug field empty; check uniqueness
- `Edit` POST: update all fields; re-validate slug uniqueness (excluding self)
- `Delete` POST: remove record, redirect Index

### Views

- `Views/Admin/Jobs/Index.cshtml` — table with Title (VI), Slug, SortOrder, IsActive, Edit/Delete actions
- `Views/Admin/Jobs/Create.cshtml` — tabbed or stacked form: VI / JA / EN fields, Slug input, Salary, SortOrder, IsActive checkbox
- `Views/Admin/Jobs/Edit.cshtml` — same as Create, pre-filled

### Admin nav

Add "Vị trí tuyển dụng" link in `_DrawerMenu.cshtml` or `_AdminLayout.cshtml`.

---

## Helper

`src/Mitech.Web/Helpers/SlugHelper.cs`:
```csharp
public static class SlugHelper
{
    public static string Generate(string input)
    {
        // normalize Unicode, remove diacritics, lowercase, replace spaces with -
        // e.g. "Công nhân vận hành máy CNC" → "cong-nhan-van-hanh-may-cnc"
    }
}
```

---

## Files Changed / Created

| Action | File |
|---|---|
| Create | `src/Mitech.Web/Models/JobPosition.cs` |
| Modify | `src/Mitech.Web/Data/ApplicationDbContext.cs` |
| Create | `src/Mitech.Web/Helpers/SlugHelper.cs` |
| Create | EF migration `AddJobPositions` |
| Modify | Controller that serves TuyenDung (pass jobs list) |
| Modify | `src/Mitech.Web/Views/StaticPages/TuyenDung.cshtml` — slider section |
| Create | `src/Mitech.Web/Controllers/RecruitmentController.cs` |
| Create | `src/Mitech.Web/Views/Recruitment/Detail.cshtml` |
| Create | `src/Mitech.Web/Controllers/Admin/JobsAdminController.cs` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Index.cshtml` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Create.cshtml` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Edit.cshtml` |
| Modify | `src/Mitech.Web/Program.cs` — routes |
| Modify | `src/Mitech.Web/Views/Shared/_AdminLayout.cshtml` — nav link |
| Seed | 2 existing hardcoded positions as initial DB data |

---

## Error Handling

- Slug collision on create → show validation error "Slug đã tồn tại"
- Detail page with unknown slug → `NotFoundResult` → 404 page
- No active job positions → slider hidden, show "Hiện chưa có vị trí tuyển dụng"
- Empty SlugHelper input → returns `"vi-tri-" + Guid.NewGuid().ToString()[..8]`

---

## Out of Scope

- Image per job position
- Apply form / email sending
- Pagination of job positions
- Job position categories/tags
- Expiry date per position
