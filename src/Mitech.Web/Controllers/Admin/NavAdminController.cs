using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class NavAdminController : AdminBaseController
{
    private readonly IPageContentService _content;

    public NavAdminController(IPageContentService content) => _content = content;

    private static readonly string[] Langs = ["vi", "ja", "en"];

    // Keys per language. recruit: để trống = ẩn link đó với ngôn ngữ đó.
    private static readonly string[] Keys =
    [
        "nav.top", "nav.products", "nav.about",
        "nav.company", "nav.locations", "nav.news",
        "nav.contact", "nav.recruit",
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cài đặt menu điều hướng";

        var textContent = new Dictionary<string, Dictionary<string, string>>();
        foreach (var lang in Langs)
            textContent[lang] = await _content.GetPageAsync("nav", lang);
        ViewData["NavContent"] = textContent;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLang(string lang, IFormCollection form)
    {
        if (!Langs.Contains(lang)) return BadRequest();

        foreach (var key in Keys)
        {
            if (form.TryGetValue(key, out var val))
                await _content.SetAsync(key, lang, val.ToString().Trim());
        }

        TempData["Success"] = $"Đã lưu menu ({lang.ToUpper()}).";
        return RedirectToAction(nameof(Index));
    }
}
