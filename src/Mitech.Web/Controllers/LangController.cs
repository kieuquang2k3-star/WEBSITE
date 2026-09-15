using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class LangController : Controller
{
    private static readonly string[] Supported = ["vi", "ja", "en"];

    public IActionResult Switch(string code, string? returnUrl)
    {
        if (Supported.Contains(code))
            LanguageService.SetCookie(Response, code);

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
