using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public abstract class BasePublicController : Controller
{
    private readonly ILanguageService _langService;

    protected BasePublicController(ILanguageService langService)
    {
        _langService = langService;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewData["Lang"] = _langService.GetCurrent(HttpContext);

        var content = HttpContext.RequestServices.GetRequiredService<IPageContentService>();
        ViewData["Logo.Header.Path"]  = OrDefault(await content.GetAsync("logo.header.path",  "global"), "/images/logo.png");
        ViewData["Logo.Header.Width"] = ParseWidth(await content.GetAsync("logo.header.width", "global"), 321);
        ViewData["Logo.Footer.Path"]  = OrDefault(await content.GetAsync("logo.footer.path",  "global"), "/images/logo_sub.png");
        ViewData["Logo.Footer.Width"] = ParseWidth(await content.GetAsync("logo.footer.width", "global"), 321);
        ViewData["Logo.Mark.Path"]    = OrDefault(await content.GetAsync("logo.mark.path",    "global"), "/images/logo-mark.png");
        ViewData["Logo.Mark.Width"]   = ParseWidth(await content.GetAsync("logo.mark.width",  "global"), 259);

        await next();
    }

    protected string Lang => ViewData["Lang"] as string ?? "vi";

    private static string OrDefault(string value, string defaultValue)
        => string.IsNullOrWhiteSpace(value) ? defaultValue : value;

    private static int ParseWidth(string value, int defaultWidth)
        => int.TryParse(value, out var w) && w > 0 ? w : defaultWidth;
}
