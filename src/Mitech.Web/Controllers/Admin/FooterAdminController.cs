using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class FooterAdminController : AdminBaseController
{
    private const string G = "global";
    private readonly IPageContentService _content;

    public FooterAdminController(IPageContentService content) => _content = content;

    private static readonly string[] Langs = ["vi", "ja", "en"];

    private static readonly string[] GlobalKeys =
    [
        "footer.company.tel",
        "footer.hq.address",
        "footer.hq.tel",
        "footer.hq.fax",
        "footer.instagram.url",
    ];

    private static readonly string[] LangKeys =
    [
        "footer.company.name",
        "footer.company.address",
        "footer.hq.name",
        "footer.business.1",
        "footer.business.2",
        "footer.business.3",
        "footer.business.4",
        "footer.copyright",
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cài đặt Footer";

        foreach (var key in GlobalKeys)
            ViewData[key] = await _content.GetAsync(key, G);

        var textContent = new Dictionary<string, Dictionary<string, string>>();
        foreach (var lang in Langs)
            textContent[lang] = await _content.GetPageAsync("footer", lang);
        ViewData["TextContent"] = textContent;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGlobal(IFormCollection form)
    {
        foreach (var key in GlobalKeys)
        {
            if (form.TryGetValue(key, out var val))
                await _content.SetAsync(key, G, val.ToString().Trim());
        }
        TempData["Success"] = "Đã lưu thông tin chung của footer.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLang(string lang, IFormCollection form)
    {
        if (!Langs.Contains(lang)) return BadRequest();

        foreach (var key in LangKeys)
        {
            if (form.TryGetValue(key, out var val))
                await _content.SetAsync(key, lang, val.ToString().Trim());
        }
        TempData["Success"] = $"Đã lưu nội dung footer ({lang.ToUpper()}).";
        return RedirectToAction(nameof(Index));
    }
}
