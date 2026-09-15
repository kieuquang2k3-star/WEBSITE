# Favicon Admin Management — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow admin to upload or enter a URL for the site favicon via the HomeSettings admin page, stored in `PageContent` and rendered dynamically in `_Layout.cshtml`.

**Architecture:** Reuses the existing `PageContentService` / `HomeSettingsAdminController` / `SaveFileAsync` pattern (same as logo management). Key `"site.favicon.path"` / lang `"global"` stores the path. `_Layout.cshtml` injects `IPageContentService` and reads the value at render time.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core (PageContent table), Razor

---

## File Map

| Action | File |
|---|---|
| Modify | `src/Mitech.Web/Data/SeedData.cs` |
| Modify | `src/Mitech.Web/Controllers/Admin/HomeSettingsAdminController.cs` |
| Modify | `src/Mitech.Web/Views/Admin/HomeSettings/Index.cshtml` |
| Modify | `src/Mitech.Web/Views/Shared/_Layout.cshtml` |

---

### Task 1: Seed default favicon in SeedData.cs

**Files:**
- Modify: `src/Mitech.Web/Data/SeedData.cs`

- [ ] **Step 1: Add `SeedSiteSettingsAsync` method and call it**

In `src/Mitech.Web/Data/SeedData.cs`, add a new private method after `SeedHomeContentAsync` and call it from `InitializeAsync`.

**In `InitializeAsync` — add one line after `await SeedHomeContentAsync(db);`:**
```csharp
await SeedSiteSettingsAsync(db);
```

**New method to add at the bottom of the class (before the closing `}`):**
```csharp
private static async Task SeedSiteSettingsAsync(ApplicationDbContext db)
{
    if (!await db.PageContents.AnyAsync(p => p.PageKey == "site.favicon.path"))
    {
        db.PageContents.Add(new PageContent
        {
            PageKey = "site.favicon.path",
            Lang = "global",
            Value = "/favicon.png"
        });
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Build to verify no errors**

```
dotnet build src/Mitech.Web
```

Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Mitech.Web/Data/SeedData.cs
git commit -m "feat(favicon): seed default site.favicon.path in PageContent"
```

---

### Task 2: Add favicon action to HomeSettingsAdminController

**Files:**
- Modify: `src/Mitech.Web/Controllers/Admin/HomeSettingsAdminController.cs`

- [ ] **Step 1: Add `ViewData["FaviconPath"]` to `Index()`**

In `HomeSettingsAdminController.cs`, inside the `Index()` method, add this line after the existing `ViewData["MarkLogoWidth"]` line:

```csharp
ViewData["FaviconPath"] = await _content.GetAsync("site.favicon.path", G);
```

The updated block should look like:
```csharp
ViewData["HeaderLogoPath"]  = await _content.GetAsync("logo.header.path",  G);
ViewData["HeaderLogoWidth"] = await _content.GetAsync("logo.header.width", G);
ViewData["FooterLogoPath"]  = await _content.GetAsync("logo.footer.path",  G);
ViewData["FooterLogoWidth"] = await _content.GetAsync("logo.footer.width", G);
ViewData["MarkLogoPath"]    = await _content.GetAsync("logo.mark.path",    G);
ViewData["MarkLogoWidth"]   = await _content.GetAsync("logo.mark.width",   G);
ViewData["FaviconPath"]     = await _content.GetAsync("site.favicon.path", G);
```

- [ ] **Step 2: Add `SaveFavicon` action**

Add this new action method to `HomeSettingsAdminController`, after `SaveLogo` and before `SaveVideo`:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> SaveFavicon(string? faviconUrl, IFormFile? faviconFile)
{
    if (faviconFile is { Length: > 0 })
        faviconUrl = await SaveFileAsync(faviconFile, "favicon");

    if (!string.IsNullOrWhiteSpace(faviconUrl))
        await _content.SetAsync("site.favicon.path", G, faviconUrl.Trim());

    TempData["Success"] = "Đã lưu favicon.";
    return RedirectToAction(nameof(Index));
}
```

- [ ] **Step 3: Build to verify**

```
dotnet build src/Mitech.Web
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Mitech.Web/Controllers/Admin/HomeSettingsAdminController.cs
git commit -m "feat(favicon): add FaviconPath to Index, add SaveFavicon action"
```

---

### Task 3: Add Favicon section to HomeSettings view

**Files:**
- Modify: `src/Mitech.Web/Views/Admin/HomeSettings/Index.cshtml`

- [ ] **Step 1: Add favicon variable in the `@{ }` block at the top**

In `Index.cshtml`, inside the top `@{ ... }` block, after the `var markWidth = ...` line, add:

```razor
var faviconPath = ViewData["FaviconPath"] as string ?? "/favicon.png";
```

- [ ] **Step 2: Add Favicon card section after the Logo card**

The Logo card ends with `</div>` on the line after `@* ====== VIDEO ====== *@` comment. Insert the following block **between** the closing `</div>` of the Logo card (line with `Lưu logo` button) and the `@* ====== VIDEO ====== *@` comment:

```razor
@* ====== FAVICON ====== *@
<div class="admin-card" style="margin-top:24px;">
    <h2>Favicon</h2>
    <form asp-controller="HomeSettingsAdmin" asp-action="SaveFavicon" method="post" enctype="multipart/form-data">
        @Html.AntiForgeryToken()
        <div style="display:flex;align-items:flex-start;gap:32px;flex-wrap:wrap;">
            <div>
                <label>Favicon hiện tại</label><br>
                <img src="@faviconPath" style="width:32px;height:32px;border:1px solid #eee;padding:4px;border-radius:4px;" alt="favicon">
                <p style="margin:4px 0 0;font-size:0.8rem;color:#666;">@faviconPath</p>
            </div>
            <div style="flex:1;min-width:260px;">
                <div class="form-group">
                    <label>Upload file mới (.ico, .png, .svg)</label>
                    <input type="file" name="faviconFile" class="form-control" accept=".ico,.png,.jpg,.svg">
                </div>
                <div class="form-group">
                    <label>Hoặc nhập URL</label>
                    <input type="text" name="faviconUrl" class="form-control" value="@faviconPath" placeholder="/favicon.png">
                </div>
                <button type="submit" class="btn btn-primary">Lưu favicon</button>
            </div>
        </div>
    </form>
</div>
```

- [ ] **Step 3: Build to verify Razor compiles**

```
dotnet build src/Mitech.Web
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Mitech.Web/Views/Admin/HomeSettings/Index.cshtml
git commit -m "feat(favicon): add Favicon section to HomeSettings admin view"
```

---

### Task 4: Make favicon dynamic in _Layout.cshtml

**Files:**
- Modify: `src/Mitech.Web/Views/Shared/_Layout.cshtml`

- [ ] **Step 1: Add `@inject` and replace hardcoded favicon link**

In `_Layout.cshtml`, find this line (currently line 30):
```html
<link rel="icon" href="/favicon.png" sizes="32x32">
```

Replace it with:
```razor
@inject Mitech.Web.Services.IPageContentService _faviconSvc
@{ var _faviconHref = await _faviconSvc.GetAsync("site.favicon.path", "global"); }
<link rel="icon" href="@(_faviconHref is { Length: > 0 } ? _faviconHref : "/favicon.png")" sizes="32x32">
```

- [ ] **Step 2: Build to verify**

```
dotnet build src/Mitech.Web
```

Expected: `Build succeeded.`

- [ ] **Step 3: Run app and verify favicon loads**

Start the app (if not running):
```
dotnet run --project src/Mitech.Web
```

Then open a browser and go to `http://localhost:5095`. Verify the favicon shows in the browser tab (it should still be `/favicon.png` since that's the seeded default).

- [ ] **Step 4: Commit**

```bash
git add src/Mitech.Web/Views/Shared/_Layout.cshtml
git commit -m "feat(favicon): read favicon path dynamically from PageContent in _Layout"
```

---

### Task 5: End-to-end manual verification

- [ ] **Step 1: Run the app**

```
dotnet run --project src/Mitech.Web
```

- [ ] **Step 2: Log in to admin and open Home Settings**

Navigate to `http://localhost:5095/admin/login`, log in with `admin@mitec.co.jp` / `Admin@123456`, then go to `http://localhost:5095/admin/homesettings`.

Verify: A **"Favicon"** section appears below the Logo section, showing the current `/favicon.png` preview (32×32).

- [ ] **Step 3: Upload a new favicon**

In the Favicon section, click the file input and select any PNG image from your computer. Click **"Lưu favicon"**.

Verify: Page redirects back with success message "Đã lưu favicon."

- [ ] **Step 4: Verify favicon changed on the public site**

Open `http://localhost:5095` in a browser. Hard-refresh (Ctrl+Shift+R). The browser tab favicon should now show the newly uploaded image.

- [ ] **Step 5: Run tests to confirm nothing broken**

```
dotnet test tests/Mitech.Tests
```

Expected: All tests pass (no regressions).

- [ ] **Step 6: Final commit (if any uncommitted changes)**

```bash
git status
# If clean: nothing to do
# If dirty: git add -A && git commit -m "fix: resolve any post-verification issues"
```
