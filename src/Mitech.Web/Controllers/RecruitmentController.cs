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
