using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Data;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class PagesAdminController : AdminBaseController
{
    private readonly IPageContentService _contentService;

    public PagesAdminController(IPageContentService contentService)
    {
        _contentService = contentService;
    }

    public async Task<IActionResult> Index()
    {
        var all = await _contentService.GetAllAsync();
        return View(all);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, string lang, string value)
    {
        await _contentService.SetAsync(key, lang, value);
        return RedirectToAction(nameof(Index));
    }
}
