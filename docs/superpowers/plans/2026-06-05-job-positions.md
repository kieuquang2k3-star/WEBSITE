# Job Positions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace hardcoded job cards on /tuyen-dung with a DB-driven prev/next slider, add detail page per position (/tuyen-dung/{slug}), and provide admin CRUD at /admin/jobs.

**Architecture:** New `JobPosition` EF model with 3-lang fields. `StaticPagesController.TuyenDung()` loads active positions from DB and passes to view. Vanilla JS + CSS slider (no external library). `RecruitmentController.Detail(slug)` serves individual position pages. `JobsAdminController` provides full CRUD following the existing ProductsAdmin pattern.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQLite dev / SQL Server prod), xUnit + Moq (tests)

---

## File Map

| Action | File |
|---|---|
| Create | `src/Mitech.Web/Models/JobPosition.cs` |
| Create | `src/Mitech.Web/Helpers/SlugHelper.cs` |
| Modify | `src/Mitech.Web/Data/ApplicationDbContext.cs` |
| Create | EF Migration `AddJobPositions` |
| Modify | `src/Mitech.Web/Data/SeedData.cs` |
| Modify | `src/Mitech.Web/Controllers/StaticPagesController.cs` |
| Modify | `src/Mitech.Web/Views/StaticPages/TuyenDung.cshtml` |
| Create | `src/Mitech.Web/Controllers/RecruitmentController.cs` |
| Create | `src/Mitech.Web/Views/Recruitment/Detail.cshtml` |
| Create | `src/Mitech.Web/Controllers/Admin/JobsAdminController.cs` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Index.cshtml` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Create.cshtml` |
| Create | `src/Mitech.Web/Views/Admin/Jobs/Edit.cshtml` |
| Modify | `src/Mitech.Web/Program.cs` |
| Modify | `src/Mitech.Web/Views/Shared/_AdminLayout.cshtml` |
| Create | `tests/Mitech.Tests/Helpers/SlugHelperTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/JobsAdminControllerTests.cs` |

---

### Task 1: JobPosition model + SlugHelper + DbContext

**Files:**
- Create: `src/Mitech.Web/Models/JobPosition.cs`
- Create: `src/Mitech.Web/Helpers/SlugHelper.cs`
- Modify: `src/Mitech.Web/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Write SlugHelper tests (RED)**

Create `tests/Mitech.Tests/Helpers/SlugHelperTests.cs`:

```csharp
using Mitech.Web.Helpers;

namespace Mitech.Tests.Helpers;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Công nhân vận hành máy CNC", "cong-nhan-van-hanh-may-cnc")]
    [InlineData("Kiểm tra ngoại quan (VIS)", "kiem-tra-ngoai-quan-vis")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  spaces  ", "spaces")]
    public void Generate_ProducesExpectedSlug(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.Generate(input));
    }

    [Fact]
    public void Generate_EmptyInput_ReturnsFallback()
    {
        var result = SlugHelper.Generate(string.Empty);
        Assert.StartsWith("vi-tri-", result);
        Assert.Equal(15, result.Length); // "vi-tri-" + 8 chars
    }

    [Fact]
    public void Generate_SpecialCharsOnly_ReturnsFallback()
    {
        var result = SlugHelper.Generate("!@#$%");
        Assert.StartsWith("vi-tri-", result);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~SlugHelperTests" -v normal
```

Expected: FAIL — `Mitech.Web.Helpers.SlugHelper` does not exist.

- [ ] **Step 3: Create `src/Mitech.Web/Helpers/SlugHelper.cs`**

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mitech.Web.Helpers;

public static class SlugHelper
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "vi-tri-" + Guid.NewGuid().ToString()[..8];

        var s = input.ToLowerInvariant()
            .Replace("đ", "d").Replace("ð", "d");

        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = NonAlphanumeric
            .Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-")
            .Trim('-');

        return string.IsNullOrEmpty(result)
            ? "vi-tri-" + Guid.NewGuid().ToString()[..8]
            : result;
    }
}
```

- [ ] **Step 4: Create `src/Mitech.Web/Models/JobPosition.cs`**

```csharp
namespace Mitech.Web.Models;

public class JobPosition
{
    public int    Id        { get; set; }
    public string Slug      { get; set; } = string.Empty;

    public string TitleVi      { get; set; } = string.Empty;
    public string TitleJa      { get; set; } = string.Empty;
    public string TitleEn      { get; set; } = string.Empty;

    public string ShortDescVi  { get; set; } = string.Empty;
    public string ShortDescJa  { get; set; } = string.Empty;
    public string ShortDescEn  { get; set; } = string.Empty;

    public string DetailVi     { get; set; } = string.Empty;
    public string DetailJa     { get; set; } = string.Empty;
    public string DetailEn     { get; set; } = string.Empty;

    public string SalaryVi     { get; set; } = string.Empty;
    public string SalaryJa     { get; set; } = string.Empty;
    public string SalaryEn     { get; set; } = string.Empty;

    public int    SortOrder    { get; set; }
    public bool   IsActive     { get; set; } = true;
}
```

- [ ] **Step 5: Add DbSet and indexes to `src/Mitech.Web/Data/ApplicationDbContext.cs`**

Add after `public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();`:
```csharp
public DbSet<JobPosition> JobPositions => Set<JobPosition>();
```

Add inside `OnModelCreating` after the existing `Product` index:
```csharp
builder.Entity<JobPosition>()
    .HasIndex(j => j.Slug)
    .IsUnique();

builder.Entity<JobPosition>()
    .HasIndex(j => j.SortOrder);
```

- [ ] **Step 6: Run SlugHelper tests (GREEN)**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~SlugHelperTests" -v normal
```

Expected: Passed: 6

- [ ] **Step 7: Build to verify**

```
dotnet build src/Mitech.Web /p:CopyBuildOutputToOutputDirectory=false
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 8: Commit**

```bash
git add src/Mitech.Web/Models/JobPosition.cs src/Mitech.Web/Helpers/SlugHelper.cs src/Mitech.Web/Data/ApplicationDbContext.cs tests/Mitech.Tests/Helpers/SlugHelperTests.cs
git commit -m "feat(jobs): add JobPosition model, SlugHelper, DbContext"
```

---

### Task 2: EF Migration

**Files:**
- Create: EF migration `AddJobPositions` (auto-generated)

- [ ] **Step 1: Run migration**

```
cd src/Mitech.Web
dotnet ef migrations add AddJobPositions
dotnet ef database update
cd ../..
```

Expected: Migration file created in `src/Mitech.Web/Migrations/`, database updated.

- [ ] **Step 2: Build to verify**

```
dotnet build src/Mitech.Web /p:CopyBuildOutputToOutputDirectory=false
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add src/Mitech.Web/Migrations/
git commit -m "feat(jobs): add EF migration AddJobPositions"
```

---

### Task 3: Seed 2 initial job positions

**Files:**
- Modify: `src/Mitech.Web/Data/SeedData.cs`

- [ ] **Step 1: Add `SeedJobPositionsAsync` call in `InitializeAsync`**

In `SeedData.cs`, in the `InitializeAsync` method, after the `await SeedNavContentAsync(db);` line, add:

```csharp
await SeedJobPositionsAsync(db);
```

- [ ] **Step 2: Add `SeedJobPositionsAsync` method at the bottom of `SeedData.cs` (before closing `}`)**

```csharp
private static async Task SeedJobPositionsAsync(ApplicationDbContext db)
{
    if (await db.JobPositions.AnyAsync()) return;

    db.JobPositions.AddRange(
        new JobPosition
        {
            Slug       = "van-hanh-may-cnc",
            TitleVi    = "Công nhân vận hành máy CNC",
            TitleJa    = "CNCオペレーター",
            TitleEn    = "CNC Machine Operator",
            ShortDescVi = "Vận hành máy bán tự động (tiện, mài...). Không làm việc theo dây chuyền.",
            ShortDescJa = "半自動機械（旋盤・研削など）を操作します。ライン作業はありません。",
            ShortDescEn = "Operates semi-automatic machines (turning, grinding, etc.). No assembly-line work.",
            DetailVi   = "<p>Vận hành máy bán tự động bao gồm tiện, mài và các loại máy CNC khác. Không làm việc theo dây chuyền sản xuất. Được đào tạo từ đầu, không yêu cầu kinh nghiệm.</p>",
            DetailJa   = "<p>半自動機械（旋盤・研削など）を操作します。ライン作業はありません。未経験でも研修から始められます。</p>",
            DetailEn   = "<p>Operates semi-automatic machines including lathes, grinders, and other CNC equipment. No assembly-line work required. Training provided from scratch.</p>",
            SalaryVi   = "8 – 15 triệu VNĐ/tháng",
            SalaryJa   = "月収 8〜15百万 VND",
            SalaryEn   = "8 – 15 million VND/month",
            SortOrder  = 1,
            IsActive   = true
        },
        new JobPosition
        {
            Slug       = "kiem-tra-ngoai-quan-vis",
            TitleVi    = "Công nhân kiểm tra ngoại quan (VIS)",
            TitleJa    = "外観検査員（VIS）",
            TitleEn    = "Visual Inspection Operator (VIS)",
            ShortDescVi = "Kiểm tra sản phẩm nhỏ trong phòng điều hòa. Công việc nhẹ nhàng, sạch sẽ.",
            ShortDescJa = "空調完備の室内にて小型部品を目視検査。軽作業・クリーンな環境です。",
            ShortDescEn = "Inspects small components in an air-conditioned room. Light, clean work environment.",
            DetailVi   = "<p>Kiểm tra ngoại quan sản phẩm nhỏ bằng mắt thường trong phòng có điều hòa. Công việc nhẹ nhàng, sạch sẽ, phù hợp cả nam và nữ. Không yêu cầu kinh nghiệm.</p>",
            DetailJa   = "<p>空調完備の室内にて小型部品を目視検査。軽作業・クリーンな環境で、男女問わず活躍できます。未経験でもOKです。</p>",
            DetailEn   = "<p>Visually inspects small components in an air-conditioned room. Light, clean work environment suitable for both men and women. No experience required.</p>",
            SalaryVi   = "8 – 15 triệu VNĐ/tháng",
            SalaryJa   = "月収 8〜15百万 VND",
            SalaryEn   = "8 – 15 million VND/month",
            SortOrder  = 2,
            IsActive   = true
        }
    );
    await db.SaveChangesAsync();
}
```

- [ ] **Step 3: Build to verify**

```
dotnet build src/Mitech.Web /p:CopyBuildOutputToOutputDirectory=false
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add src/Mitech.Web/Data/SeedData.cs
git commit -m "feat(jobs): seed 2 initial job positions"
```

---

### Task 4: StaticPagesController + TuyenDung slider view

**Files:**
- Modify: `src/Mitech.Web/Controllers/StaticPagesController.cs`
- Modify: `src/Mitech.Web/Views/StaticPages/TuyenDung.cshtml`

- [ ] **Step 1: Update `StaticPagesController.cs`**

Replace the entire file content with:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class StaticPagesController : BasePublicController
{
    private readonly ApplicationDbContext _db;

    public StaticPagesController(ILanguageService langService, ApplicationDbContext db) : base(langService)
    {
        _db = db;
    }

    public IActionResult Purchase()   => View();
    public IActionResult ActionPlan() => View();
    public IActionResult Privacy()    => View();
    public IActionResult Security()   => View();
    public IActionResult Sitemap()    => View();

    public async Task<IActionResult> TuyenDung()
    {
        var jobs = await _db.JobPositions
            .Where(j => j.IsActive)
            .OrderBy(j => j.SortOrder)
            .ToListAsync();
        ViewData["Jobs"] = jobs;
        return View();
    }
}
```

- [ ] **Step 2: Update `TuyenDung.cshtml` — remove hardcoded job variables from `@{ }` block**

In the string declaration (line 7), change:
```csharp
string secJobs, job1Title, job1Desc, job2Title, job2Desc;
```
To:
```csharp
string secJobs, secJobsCta;
```

- [ ] **Step 3: In each language block, replace the job variable assignments**

For the **Japanese** block, replace:
```csharp
secJobs   = "募集職種";
job1Title = "CNCオペレーター"; job1Desc = "半自動機械（旋盤・研削など）を操作します。ライン作業はありません。";
job2Title = "外観検査員（VIS）"; job2Desc = "空調完備の室内にて小型部品を目視検査。軽作業・クリーンな環境です。";
```
With:
```csharp
secJobs    = "募集職種";
secJobsCta = "詳細を見る →";
```

For the **English** block, replace:
```csharp
secJobs   = "Open Positions";
job1Title = "CNC Machine Operator"; job1Desc = "Operates semi-automatic machines (turning, grinding, etc.). No assembly-line work.";
job2Title = "Visual Inspection Operator (VIS)"; job2Desc = "Inspects small components in an air-conditioned room. Light, clean work environment.";
```
With:
```csharp
secJobs    = "Open Positions";
secJobsCta = "View details →";
```

For the **Vietnamese** block, replace:
```csharp
secJobs   = "Vị trí tuyển dụng";
job1Title = "Công nhân vận hành máy CNC"; job1Desc = "Vận hành máy bán tự động (tiện, mài...). Không làm việc theo dây chuyền.";
job2Title = "Công nhân kiểm tra ngoại quan (VIS)"; job2Desc = "Kiểm tra sản phẩm nhỏ trong phòng điều hòa. Công việc nhẹ nhàng, sạch sẽ.";
```
With:
```csharp
secJobs    = "Vị trí tuyển dụng";
secJobsCta = "Xem chi tiết →";
```

- [ ] **Step 4: Update CSS in `<style>` block — replace `.job-cards` with slider styles**

Find and replace the block:
```css
.job-cards { display:grid; grid-template-columns:1fr 1fr; gap:16px; margin-bottom:16px; }
@@media(max-width:600px){ .job-cards { grid-template-columns:1fr; } }
.job-card { border:1.5px solid #c8d3ef; border-radius:8px; padding:18px 20px; }
.job-card__title { font-weight:800; color:#1a2a5e; margin-bottom:8px; font-size:.95rem; }
.job-card__desc { font-size:.85rem; color:#555; line-height:1.65; }
```

With:
```css
/* ===== JOB SLIDER ===== */
.job-slider-wrap { position:relative; display:flex; align-items:center; gap:8px; }
.job-slider-viewport { flex:1; overflow:hidden; }
.job-slider-track { display:flex; transition:transform .35s ease; }
.job-slide { flex:0 0 calc(100% / 3); padding:0 8px; box-sizing:border-box; }
@@media(max-width:700px){ .job-slide { flex:0 0 50%; } }
@@media(max-width:480px){ .job-slide { flex:0 0 100%; } }
.job-card { display:block; border:1.5px solid #c8d3ef; border-radius:8px; padding:18px 20px;
            text-decoration:none; color:inherit; height:100%; transition:border-color .2s; }
.job-card:hover { border-color:#1a2a5e; text-decoration:none; }
.job-card__title { font-weight:800; color:#1a2a5e; margin-bottom:8px; font-size:.95rem; }
.job-card__desc { font-size:.85rem; color:#555; line-height:1.65; margin-bottom:12px; }
.job-card__cta { font-size:.82rem; color:#00418f; font-weight:600; }
.job-slider-btn { flex-shrink:0; width:36px; height:36px; border-radius:50%;
                  border:1.5px solid #c8d3ef; background:#fff; cursor:pointer;
                  font-size:1.4rem; line-height:1; display:flex; align-items:center;
                  justify-content:center; transition:border-color .2s; }
.job-slider-btn:hover { border-color:#1a2a5e; }
.job-slider-btn:disabled { opacity:.35; cursor:default; }
```

- [ ] **Step 5: Replace the hardcoded job-cards HTML with slider**

Find and replace:
```html
    <div class="recruit-section">
        <h2>@secJobs</h2>
        <div class="job-cards">
            <div class="job-card">
                <p class="job-card__title">@job1Title</p>
                <p class="job-card__desc">@job1Desc</p>
            </div>
            <div class="job-card">
                <p class="job-card__title">@job2Title</p>
                <p class="job-card__desc">@job2Desc</p>
            </div>
        </div>
    </div>
```

With:
```razor
    <div class="recruit-section">
        <h2>@secJobs</h2>
        @{
            var jobs = ViewData["Jobs"] as List<Mitech.Web.Models.JobPosition> ?? new();
        }
        @if (jobs.Count == 0)
        {
            <p style="color:#888;font-style:italic;">Hiện chưa có vị trí tuyển dụng.</p>
        }
        else
        {
            <div class="job-slider-wrap" id="jobSlider">
                <button class="job-slider-btn job-slider-prev" id="jobPrev" aria-label="Previous">&#8249;</button>
                <div class="job-slider-viewport">
                    <div class="job-slider-track" id="jobTrack">
                        @foreach (var job in jobs)
                        {
                            var jTitle = lang == "ja" ? job.TitleJa : (lang == "en" ? job.TitleEn : job.TitleVi);
                            var jDesc  = lang == "ja" ? job.ShortDescJa : (lang == "en" ? job.ShortDescEn : job.ShortDescVi);
                            <div class="job-slide">
                                <a href="/tuyen-dung/@job.Slug" class="job-card">
                                    <p class="job-card__title">@jTitle</p>
                                    <p class="job-card__desc">@jDesc</p>
                                    <span class="job-card__cta">@secJobsCta</span>
                                </a>
                            </div>
                        }
                    </div>
                </div>
                <button class="job-slider-btn job-slider-next" id="jobNext" aria-label="Next">&#8250;</button>
            </div>
        }
    </div>
```

- [ ] **Step 6: Add JS before the closing `</div>` of `widthBase` (before `<div class="mt-lg"></div>`)**

Add:
```html
    <script>
    (function(){
        var track = document.getElementById('jobTrack');
        if (!track) return;
        var slides = track.querySelectorAll('.job-slide');
        var prev = document.getElementById('jobPrev');
        var next = document.getElementById('jobNext');
        var current = 0;
        function visible(){ return window.innerWidth <= 480 ? 1 : window.innerWidth <= 700 ? 2 : 3; }
        function max(){ return Math.max(0, slides.length - visible()); }
        function update(){
            track.style.transform = 'translateX(-' + (current * (100 / visible())) + '%)';
            prev.disabled = current === 0;
            next.disabled = current >= max();
        }
        prev.addEventListener('click', function(){ if(current > 0){ current--; update(); } });
        next.addEventListener('click', function(){ if(current < max()){ current++; update(); } });
        window.addEventListener('resize', function(){ current = Math.min(current, max()); update(); });
        update();
    })();
    </script>
```

- [ ] **Step 7: Build to verify**

```
dotnet build src/Mitech.Web /p:CopyBuildOutputToOutputDirectory=false
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 8: Commit**

```bash
git add src/Mitech.Web/Controllers/StaticPagesController.cs src/Mitech.Web/Views/StaticPages/TuyenDung.cshtml
git commit -m "feat(jobs): DB-driven slider on tuyen-dung page"
```

---

### Task 5: RecruitmentController + Detail view + route

**Files:**
- Create: `src/Mitech.Web/Controllers/RecruitmentController.cs`
- Create: `src/Mitech.Web/Views/Recruitment/Detail.cshtml`
- Modify: `src/Mitech.Web/Program.cs`

- [ ] **Step 1: Create `src/Mitech.Web/Controllers/RecruitmentController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class RecruitmentController : BasePublicController
{
    private readonly ApplicationDbContext _db;

    public RecruitmentController(ApplicationDbContext db, ILanguageService langService) : base(langService)
    {
        _db = db;
    }

    public async Task<IActionResult> Detail(string slug)
    {
        var job = await _db.JobPositions
            .Where(j => j.Slug == slug && j.IsActive)
            .FirstOrDefaultAsync();

        if (job is null) return NotFound();
        return View(job);
    }
}
```

- [ ] **Step 2: Create `src/Mitech.Web/Views/Recruitment/Detail.cshtml`**

```razor
@model Mitech.Web.Models.JobPosition
@{
    var lang = ViewData["Lang"] as string ?? "vi";

    var title  = lang == "ja" ? Model.TitleJa  : (lang == "en" ? Model.TitleEn  : Model.TitleVi);
    var salary = lang == "ja" ? Model.SalaryJa : (lang == "en" ? Model.SalaryEn : Model.SalaryVi);
    var detail = lang == "ja" ? Model.DetailJa : (lang == "en" ? Model.DetailEn : Model.DetailVi);

    string breadHome, breadJobs, ctaLabel, salaryLabel;
    if (lang == "ja")
    {
        breadHome   = "HOME（MICROTECHNO Co., Ltd.）";
        breadJobs   = "募集職種";
        ctaLabel    = "今すぐ連絡する";
        salaryLabel = "給与・待遇";
    }
    else if (lang == "en")
    {
        breadHome   = "HOME（MICROTECHNO Co., Ltd.）";
        breadJobs   = "Recruitment";
        ctaLabel    = "Contact Us Now";
        salaryLabel = "Salary & Benefits";
    }
    else
    {
        breadHome   = "HOME（MICROTECHNO Co., Ltd.）";
        breadJobs   = "Tuyển dụng";
        ctaLabel    = "Liên hệ ngay";
        salaryLabel = "Mức lương & Đãi ngộ";
    }

    ViewData["Title"] = title;
    ViewData["BodyClass"] = "page-tuyen-dung";
}

@section PageCss {
    <link rel="stylesheet" href="/css/page.css">
    <style>
        .job-detail { max-width: 800px; margin: 0 auto; }
        .job-detail__salary {
            background: #f5f7fb;
            border: 1px solid #e4e9f2;
            border-radius: 6px;
            padding: 16px 20px;
            margin: 24px 0;
        }
        .job-detail__salary-label { font-size: .8rem; color: #888; margin-bottom: 4px; }
        .job-detail__salary-value { font-size: 1.25rem; font-weight: 800; color: #1a2a5e; }
        .job-detail__body { line-height: 1.8; margin: 24px 0; }
        .job-detail__cta { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 32px; }
        .job-detail__cta a {
            display: inline-block; padding: 12px 28px;
            background: #1a2a5e; color: #fff; border-radius: 4px;
            text-decoration: none; font-weight: 700; font-size: .95rem;
        }
        .job-detail__cta a:hover { background: #2d4090; color: #fff; }
    </style>
}

<header class="page-header inview">
    <div class="page-header__sub" lang="vi">tuyển dụng</div>
    <h1 class="page-header__main">@title</h1>
</header>

<div class="widthBase">
    <div class="breadcrumb" itemprop="breadcrumb">
        <a href="/" class="home">@breadHome</a> &gt;
        <a href="/tuyen-dung">@breadJobs</a> &gt;
        <span class="post post-page current-item">@title</span>
    </div>

    <div class="job-detail">
        <div class="job-detail__salary">
            <p class="job-detail__salary-label">@salaryLabel</p>
            <p class="job-detail__salary-value">@salary</p>
        </div>

        <div class="job-detail__body">
            @Html.Raw(detail)
        </div>

        <div class="job-detail__cta">
            <a href="tel:0901589826">📞 0901 589 826</a>
            <a href="tel:0868841651">📞 0868 841 651</a>
        </div>
    </div>

    <div class="mt-lg"></div>
</div>
@await Html.PartialAsync("_ConversionSection")
```

- [ ] **Step 3: Add route to `Program.cs`**

Add this line BEFORE the existing `"tuyen-dung"` route (before the line `app.MapControllerRoute("tuyen-dung", ...)`):

```csharp
app.MapControllerRoute("tuyen-dung-detail", "tuyen-dung/{slug}", defaults: new { controller = "Recruitment", action = "Detail" });
```

- [ ] **Step 4: Build to verify**

```
dotnet build src/Mitech.Web /p:CopyBuildOutputToOutputDirectory=false
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 5: Commit**

```bash
git add src/Mitech.Web/Controllers/RecruitmentController.cs src/Mitech.Web/Views/Recruitment/Detail.cshtml src/Mitech.Web/Program.cs
git commit -m "feat(jobs): add job detail page at /tuyen-dung/{slug}"
```

---

### Task 6: Admin CRUD — JobsAdminController + views + route + nav

**Files:**
- Create: `src/Mitech.Web/Controllers/Admin/JobsAdminController.cs`
- Create: `src/Mitech.Web/Views/Admin/Jobs/Index.cshtml`
- Create: `src/Mitech.Web/Views/Admin/Jobs/Create.cshtml`
- Create: `src/Mitech.Web/Views/Admin/Jobs/Edit.cshtml`
- Modify: `src/Mitech.Web/Program.cs`
- Modify: `src/Mitech.Web/Views/Shared/_AdminLayout.cshtml`
- Create: `tests/Mitech.Tests/Controllers/JobsAdminControllerTests.cs`

- [ ] **Step 1: Write admin controller tests (RED)**

Create `tests/Mitech.Tests/Controllers/JobsAdminControllerTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class JobsAdminControllerTests
{
    [Fact]
    public async Task Index_ReturnsJobs_OrderedBySortOrder()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.AddRange(
            new JobPosition { TitleVi = "B", SortOrder = 2, Slug = "b" },
            new JobPosition { TitleVi = "A", SortOrder = 1, Slug = "a" });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<JobPosition>>(view.Model).ToList();
        Assert.Equal("A", jobs[0].TitleVi);
        Assert.Equal("B", jobs[1].TitleVi);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<JobPosition>(view.Model);
    }

    [Fact]
    public async Task Create_Post_ValidJob_SavesAndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var job = new JobPosition { TitleVi = "Test Job", Slug = "test-job", SortOrder = 1 };

        var result = await controller.Create(job);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        using var verify = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(1, await verify.JobPositions.CountAsync());
    }

    [Fact]
    public async Task Create_Post_DuplicateSlug_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.JobPositions.Add(new JobPosition { TitleVi = "Existing", Slug = "existing", SortOrder = 1 });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));
        var result = await controller.Create(new JobPosition { TitleVi = "New", Slug = "existing", SortOrder = 2 });

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var seed = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            seed.JobPositions.Add(new JobPosition { TitleVi = "Test", Slug = "test", SortOrder = 1 });
            await seed.SaveChangesAsync();
        }
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var id = ctx.JobPositions.First().Id;
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Edit(id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<JobPosition>(view.Model);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Post_RemovesJob_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int jobId;
        using (var seed = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var j = new JobPosition { TitleVi = "Del", Slug = "del", SortOrder = 1 };
            seed.JobPositions.Add(j);
            await seed.SaveChangesAsync();
            jobId = j.Id;
        }
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new JobsAdminController(ctx));

        var result = await controller.Delete(jobId);

        Assert.IsType<RedirectToActionResult>(result);
        using var verify = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verify.JobPositions.Count());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~JobsAdminControllerTests" -v normal
```

Expected: FAIL — `JobsAdminController` does not exist.

- [ ] **Step 3: Create `src/Mitech.Web/Controllers/Admin/JobsAdminController.cs`**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Helpers;
using Mitech.Web.Models;

namespace Mitech.Web.Controllers.Admin;

public class JobsAdminController : AdminBaseController
{
    private readonly ApplicationDbContext _db;

    public JobsAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var jobs = await _db.JobPositions.OrderBy(j => j.SortOrder).ToListAsync();
        return View(jobs);
    }

    [HttpGet]
    public IActionResult Create() => View(new JobPosition());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPosition job)
    {
        if (string.IsNullOrWhiteSpace(job.Slug))
            job.Slug = SlugHelper.Generate(job.TitleVi);

        if (await _db.JobPositions.AnyAsync(j => j.Slug == job.Slug))
        {
            ModelState.AddModelError("Slug", "Slug này đã tồn tại. Vui lòng chọn slug khác.");
            return View(job);
        }

        _db.JobPositions.Add(job);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var job = await _db.JobPositions.FindAsync(id);
        if (job is null) return NotFound();
        return View(job);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, JobPosition job)
    {
        if (id != job.Id) return BadRequest();

        if (string.IsNullOrWhiteSpace(job.Slug))
            job.Slug = SlugHelper.Generate(job.TitleVi);

        if (await _db.JobPositions.AnyAsync(j => j.Slug == job.Slug && j.Id != id))
        {
            ModelState.AddModelError("Slug", "Slug này đã tồn tại.");
            return View(job);
        }

        _db.Update(job);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await _db.JobPositions.FindAsync(id);
        if (job is not null)
        {
            _db.JobPositions.Remove(job);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 4: Run tests (GREEN)**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~JobsAdminControllerTests" -v normal
```

Expected: Passed: 7

- [ ] **Step 5: Create `src/Mitech.Web/Views/Admin/Jobs/Index.cshtml`**

```razor
@model List<Mitech.Web.Models.JobPosition>
@{
    ViewData["Title"] = "Vị trí tuyển dụng";
    Layout = "_AdminLayout";
}

<div style="display:flex;justify-content:space-between;align-items:center;">
    <h1>Vị trí tuyển dụng</h1>
    <a href="/admin/jobs/create" class="btn btn-primary">+ Thêm vị trí</a>
</div>

<div class="admin-card">
    <table>
        <thead>
            <tr>
                <th style="width:60px">Thứ tự</th>
                <th>Tên vị trí (Tiếng Việt)</th>
                <th>Slug</th>
                <th style="width:100px">Trạng thái</th>
                <th style="width:140px">Thao tác</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var j in Model)
            {
                <tr>
                    <td>@j.SortOrder</td>
                    <td>@j.TitleVi</td>
                    <td><code>/tuyen-dung/@j.Slug</code></td>
                    <td>
                        @if (j.IsActive)
                        { <span style="color:green;">● Hiển thị</span> }
                        else
                        { <span style="color:#999;">○ Ẩn</span> }
                    </td>
                    <td>
                        <a href="/admin/jobs/edit/@j.Id" class="btn btn-secondary btn-sm">Sửa</a>
                        <form asp-controller="JobsAdmin" asp-action="Delete" asp-route-id="@j.Id" method="post"
                              style="display:inline;" onsubmit="return confirm('Xóa vị trí này?');">
                            @Html.AntiForgeryToken()
                            <button type="submit" class="btn btn-danger btn-sm">Xóa</button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

- [ ] **Step 6: Create `src/Mitech.Web/Views/Admin/Jobs/Create.cshtml`**

```razor
@model Mitech.Web.Models.JobPosition
@{
    ViewData["Title"] = "Thêm vị trí tuyển dụng";
    Layout = "_AdminLayout";
}

<h1>Thêm vị trí tuyển dụng</h1>
<a href="/admin/jobs" class="btn btn-secondary btn-sm" style="margin-bottom:16px;">← Quay lại</a>

<div class="admin-card">
    <form asp-controller="JobsAdmin" asp-action="Create" method="post">
        @Html.AntiForgeryToken()
        <div asp-validation-summary="ModelOnly" class="text-danger" style="margin-bottom:12px;"></div>

        <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:16px;">
            <div class="form-group">
                <label>Tiêu đề (Tiếng Việt) *</label>
                <input asp-for="TitleVi" class="form-control" required>
                <span asp-validation-for="TitleVi" class="text-danger"></span>
            </div>
            <div class="form-group">
                <label>Tiêu đề (Tiếng Nhật)</label>
                <input asp-for="TitleJa" class="form-control">
            </div>
            <div class="form-group">
                <label>Tiêu đề (Tiếng Anh)</label>
                <input asp-for="TitleEn" class="form-control">
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (VI)</label>
                <textarea asp-for="ShortDescVi" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (JA)</label>
                <textarea asp-for="ShortDescJa" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (EN)</label>
                <textarea asp-for="ShortDescEn" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (VI) — HTML</label>
                <textarea asp-for="DetailVi" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (JA) — HTML</label>
                <textarea asp-for="DetailJa" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (EN) — HTML</label>
                <textarea asp-for="DetailEn" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (VI)</label>
                <input asp-for="SalaryVi" class="form-control">
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (JA)</label>
                <input asp-for="SalaryJa" class="form-control">
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (EN)</label>
                <input asp-for="SalaryEn" class="form-control">
            </div>
            <div class="form-group">
                <label>Slug URL <small>(để trống → tự sinh từ tên VI)</small></label>
                <input asp-for="Slug" class="form-control" placeholder="vi-du-cong-nhan-cnc">
                <span asp-validation-for="Slug" class="text-danger"></span>
            </div>
            <div class="form-group">
                <label>Thứ tự hiển thị</label>
                <input asp-for="SortOrder" type="number" class="form-control" value="1">
            </div>
            <div class="form-group">
                <label>Trạng thái</label><br>
                <input asp-for="IsActive" type="checkbox" checked> Hiển thị công khai
            </div>
        </div>
        <button type="submit" class="btn btn-primary">Lưu</button>
    </form>
</div>
```

- [ ] **Step 7: Create `src/Mitech.Web/Views/Admin/Jobs/Edit.cshtml`**

```razor
@model Mitech.Web.Models.JobPosition
@{
    ViewData["Title"] = "Sửa vị trí tuyển dụng";
    Layout = "_AdminLayout";
}

<h1>Sửa vị trí tuyển dụng</h1>
<a href="/admin/jobs" class="btn btn-secondary btn-sm" style="margin-bottom:16px;">← Quay lại</a>

<div class="admin-card">
    <form asp-controller="JobsAdmin" asp-action="Edit" asp-route-id="@Model.Id" method="post">
        @Html.AntiForgeryToken()
        <input asp-for="Id" type="hidden">
        <div asp-validation-summary="ModelOnly" class="text-danger" style="margin-bottom:12px;"></div>

        <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:16px;">
            <div class="form-group">
                <label>Tiêu đề (Tiếng Việt) *</label>
                <input asp-for="TitleVi" class="form-control" required>
            </div>
            <div class="form-group">
                <label>Tiêu đề (Tiếng Nhật)</label>
                <input asp-for="TitleJa" class="form-control">
            </div>
            <div class="form-group">
                <label>Tiêu đề (Tiếng Anh)</label>
                <input asp-for="TitleEn" class="form-control">
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (VI)</label>
                <textarea asp-for="ShortDescVi" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (JA)</label>
                <textarea asp-for="ShortDescJa" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group">
                <label>Mô tả ngắn (EN)</label>
                <textarea asp-for="ShortDescEn" class="form-control" rows="2"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (VI) — HTML</label>
                <textarea asp-for="DetailVi" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (JA) — HTML</label>
                <textarea asp-for="DetailJa" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1">
                <label>Nội dung chi tiết (EN) — HTML</label>
                <textarea asp-for="DetailEn" class="form-control" rows="5"></textarea>
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (VI)</label>
                <input asp-for="SalaryVi" class="form-control">
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (JA)</label>
                <input asp-for="SalaryJa" class="form-control">
            </div>
            <div class="form-group">
                <label>Mức lương / Đãi ngộ (EN)</label>
                <input asp-for="SalaryEn" class="form-control">
            </div>
            <div class="form-group">
                <label>Slug URL</label>
                <input asp-for="Slug" class="form-control">
                <span asp-validation-for="Slug" class="text-danger"></span>
            </div>
            <div class="form-group">
                <label>Thứ tự hiển thị</label>
                <input asp-for="SortOrder" type="number" class="form-control">
            </div>
            <div class="form-group">
                <label>Trạng thái</label><br>
                <input asp-for="IsActive" type="checkbox"> Hiển thị công khai
            </div>
        </div>
        <button type="submit" class="btn btn-primary">Cập nhật</button>
    </form>
</div>
```

- [ ] **Step 8: Add admin route to `Program.cs`**

Add before the existing `"admin-homesettings"` line:
```csharp
app.MapControllerRoute("admin-jobs", "admin/jobs/{action=Index}/{id?}", defaults: new { controller = "JobsAdmin" });
```

- [ ] **Step 9: Add nav link to `_AdminLayout.cshtml`**

Find the line:
```html
            <li><a href="/admin/homesettings">Cài đặt</a></li>
```

Add BEFORE it:
```html
            <li><a href="/admin/jobs">Tuyển dụng</a></li>
```

- [ ] **Step 10: Run all tests**

```
dotnet test tests/Mitech.Tests -v normal
```

Expected: All tests pass (previous 66 + 7 new JobsAdmin + 6 new SlugHelper = 79 total).

- [ ] **Step 11: Commit**

```bash
git add src/Mitech.Web/Controllers/Admin/JobsAdminController.cs \
        src/Mitech.Web/Views/Admin/Jobs/ \
        src/Mitech.Web/Program.cs \
        src/Mitech.Web/Views/Shared/_AdminLayout.cshtml \
        tests/Mitech.Tests/Controllers/JobsAdminControllerTests.cs
git commit -m "feat(jobs): admin CRUD at /admin/jobs with full 3-lang form"
```
