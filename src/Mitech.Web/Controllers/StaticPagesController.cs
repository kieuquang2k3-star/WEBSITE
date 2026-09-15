using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class StaticPagesController : BasePublicController
{
    private readonly ApplicationDbContext _db;
    private readonly IPageContentService _content;

    public StaticPagesController(ILanguageService langService, ApplicationDbContext db, IPageContentService content) : base(langService)
    {
        _db = db;
        _content = content;
    }

    public IActionResult Purchase()   => View();
    public IActionResult Privacy()    => View();
    public IActionResult Security()   => View();
    public async Task<IActionResult> TuyenDung()
    {
        var jobs = await _db.JobPositions
            .Where(j => j.IsActive)
            .OrderBy(j => j.SortOrder)
            .ToListAsync();
        ViewData["Jobs"] = jobs;
        ViewData["RecruitVideoUrl"] = await _content.GetAsync("recruitment.benefits.video", "global");
        ViewData["TripVideoUrl"]        = await _content.GetAsync("recruitment.trip.video",      "global");
        ViewData["InsuranceVideoUrl"]   = await _content.GetAsync("recruitment.insurance.video", "global");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuyenDung(string fullName, string phone, string position)
    {
        if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(phone))
        {
            _db.ContactMessages.Add(new ContactMessage
            {
                CompanyName = "TUYENDUNG",
                ContactName = fullName.Trim(),
                Phone       = phone.Trim(),
                Message     = position?.Trim() ?? "",
                ReceivedAt  = DateTime.UtcNow,
            });
            await _db.SaveChangesAsync();
            TempData["RecruitSuccess"] = "1";
        }
        else
        {
            TempData["RecruitError"] = "1";
        }
        return RedirectToAction(nameof(TuyenDung));
    }
}
