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
        SanitizeStrings(job);

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

        SanitizeStrings(job);

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

    private static void SanitizeStrings(JobPosition job)
    {
        job.TitleVi     ??= string.Empty;
        job.TitleJa     ??= string.Empty;
        job.TitleEn     ??= string.Empty;
        job.ShortDescVi ??= string.Empty;
        job.ShortDescJa ??= string.Empty;
        job.ShortDescEn ??= string.Empty;
        job.DetailVi    ??= string.Empty;
        job.DetailJa    ??= string.Empty;
        job.DetailEn    ??= string.Empty;
        job.SalaryVi    ??= string.Empty;
        job.SalaryJa    ??= string.Empty;
        job.SalaryEn    ??= string.Empty;
        job.VideoUrl    ??= string.Empty;
        job.Slug        ??= string.Empty;
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
