# Favicon Admin Management — Design Spec
**Date:** 2026-06-05
**Scope:** Allow admin to configure the website favicon via the existing HomeSettings admin page.

---

## Overview

Add favicon upload/URL management to the admin panel. The favicon path is stored in `PageContent` (key `site.favicon.path`, lang `global`) and read dynamically in `_Layout.cshtml`. The admin UI is a new section in the existing `HomeSettingsAdminController` page, following the same pattern as logo management.

---

## Architecture

### Data

One new `PageContent` row seeded by default:

| PageKey | Lang | Value |
|---|---|---|
| `site.favicon.path` | `global` | `/favicon.png` |

No schema change — reuses existing `PageContent` table.

### Backend — HomeSettingsAdminController

**`Index()` (GET):** Add `ViewData["FaviconPath"] = await _content.GetAsync("site.favicon.path", G)`.

**`SaveFavicon(string? faviconUrl, IFormFile? faviconFile)` (POST, ValidateAntiForgeryToken):**
1. If `faviconFile` has content → call `SaveFileAsync(faviconFile, "favicon")` → override `faviconUrl` with returned path
2. If `faviconUrl` is non-empty → `await _content.SetAsync("site.favicon.path", G, faviconUrl.Trim())`
3. `TempData["Success"] = "Đã lưu favicon."` → redirect to Index

The `SaveFileAsync` helper already exists — no changes needed.

Accepted file types: `.ico`, `.png`, `.jpg`, `.svg` (validated client-side via `accept` attribute; no server-side MIME enforcement beyond the existing pattern).

### Frontend Admin — HomeSettings view

Add a **"Favicon"** card section to `Views/Admin/HomeSettings/Index.cshtml`, placed after the Logo section:

```
┌─────────────────────────────────────────┐
│ Favicon                                 │
│ [Current favicon preview 32×32]         │
│                                         │
│ URL: [text input]                       │
│ Hoặc upload: [file input .ico/.png/.svg]│
│                        [Lưu favicon]    │
└─────────────────────────────────────────┘
```

Form posts to `asp-action="SaveFavicon"`.

### _Layout.cshtml — dynamic favicon

Replace hardcoded:
```html
<link rel="icon" href="/favicon.png" sizes="32x32">
```

With Razor that injects `IPageContentService` and reads the stored value, falling back to `/favicon.png`:
```razor
@inject Mitech.Web.Services.IPageContentService _faviconContent
...
@{ var _faviconPath = await _faviconContent.GetAsync("site.favicon.path", "global"); }
<link rel="icon" href="@(_faviconPath is { Length: > 0 } ? _faviconPath : "/favicon.png")" sizes="32x32">
```

### SeedData

In `SeedData.cs` (inside a new private method `SeedSiteSettingsAsync`), seed the default favicon key if not present:
```csharp
if (!await db.PageContents.AnyAsync(p => p.PageKey == "site.favicon.path"))
    db.PageContents.Add(new PageContent { PageKey = "site.favicon.path", Lang = "global", Value = "/favicon.png" });
await db.SaveChangesAsync();
```

Call this method from `InitializeAsync`.

---

## Files Changed

| Action | File |
|---|---|
| Modify | `src/Mitech.Web/Controllers/Admin/HomeSettingsAdminController.cs` |
| Modify | `src/Mitech.Web/Views/Admin/HomeSettings/Index.cshtml` |
| Modify | `src/Mitech.Web/Views/Shared/_Layout.cshtml` |
| Modify | `src/Mitech.Web/Data/SeedData.cs` |

---

## Error Handling

- No file and no URL posted → do nothing (no save, no error message, just redirect)
- DB read failure in `_Layout.cshtml` → fallback `/favicon.png` via null-coalescing
- Invalid file type → browser `accept` attribute prevents selection; no server-side rejection needed (follows existing logo pattern)

---

## Testing

- Manual: upload a PNG, reload frontend → new favicon appears in browser tab
- Manual: enter a URL, save → favicon changes
- Existing unit tests for `PageContentService` cover the underlying read/write — no new unit tests required
- `HomeSettingsAdminController` already has no dedicated unit tests (consistent with other admin controllers of this type)

---

## Out of Scope

- Multiple favicon sizes (apple-touch-icon, etc.)
- SVG favicon support beyond file upload (no special handling)
- Cache-busting query string on the favicon URL
