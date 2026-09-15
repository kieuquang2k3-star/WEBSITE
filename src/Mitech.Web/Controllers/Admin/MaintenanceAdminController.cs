using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Infrastructure;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class MaintenanceAdminController : AdminBaseController
{
    private const string G = "global";
    private const string EnabledKey = "site.maintenance.enabled";
    private const string MessageKey = "site.maintenance.message";
    private static readonly string[] Langs = ["vi", "ja", "en"];

    private readonly IPageContentService _content;

    public MaintenanceAdminController(IPageContentService content) => _content = content;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Chế độ bảo trì";
        ViewData["Enabled"] = string.Equals(await _content.GetAsync(EnabledKey, G), "true", StringComparison.OrdinalIgnoreCase);

        var messages = new Dictionary<string, string>();
        foreach (var lang in Langs)
            messages[lang] = await _content.GetAsync(MessageKey, lang);
        ViewData["Messages"] = messages;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string? enabled, IFormCollection form)
    {
        var isOn = enabled is "on" or "true";
        await _content.SetAsync(EnabledKey, G, isOn ? "true" : "false");

        foreach (var lang in Langs)
            if (form.TryGetValue($"message.{lang}", out var val))
                await _content.SetAsync(MessageKey, lang, val.ToString().Trim());

        MaintenanceMiddleware.InvalidateCache();

        TempData["Success"] = isOn
            ? "Đã BẬT chế độ bảo trì. Khách truy cập sẽ thấy trang thông báo."
            : "Đã TẮT chế độ bảo trì. Website hoạt động bình thường.";
        return RedirectToAction(nameof(Index));
    }
}
