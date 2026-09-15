using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class RecruitmentAdminController : AdminBaseController
{
    private readonly IPageContentService _content;

    public RecruitmentAdminController(IPageContentService content) => _content = content;

    private static readonly string[] Langs = ["vi", "ja", "en"];

    private static readonly string[] TextKeys =
    [
        "recruit.hero.tag",
        "recruit.hero.title",
        "recruit.hero.desc",
        "recruit.hero.income.label",
        "recruit.hero.income.value",
        "recruit.hero.contact",
        "recruit.stat1.label", "recruit.stat1.value",
        "recruit.stat2.label", "recruit.stat2.value",
        "recruit.stat3.label", "recruit.stat3.value",
        "recruit.schedule.heading",
        "recruit.schedule.note",
        "recruit.schedule.admin.label", "recruit.schedule.admin.value",
        "recruit.schedule.day.label", "recruit.schedule.day.value",
        "recruit.schedule.night.label", "recruit.schedule.night.value",
        "recruit.schedule.prod.label",
        "recruit.schedule.shift1", "recruit.schedule.shift2", "recruit.schedule.shift3",
        "recruit.schedule.fixed.label", "recruit.schedule.fixed.value", "recruit.schedule.fixed.note",
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Nội dung trang Tuyển dụng";

        var textContent = new Dictionary<string, Dictionary<string, string>>();
        foreach (var lang in Langs)
            textContent[lang] = await _content.GetPageAsync("recruit", lang);
        ViewData["TextContent"] = textContent;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLang(string lang, IFormCollection form)
    {
        if (!Langs.Contains(lang)) return BadRequest();

        foreach (var key in TextKeys)
        {
            if (form.TryGetValue(key, out var val))
                await _content.SetAsync(key, lang, val.ToString().Trim());
        }
        TempData["Success"] = $"Đã lưu nội dung Tuyển dụng ({lang.ToUpper()}).";
        return RedirectToAction(nameof(Index));
    }
}
