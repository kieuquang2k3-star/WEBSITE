using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class CompanyController : BasePublicController
{
    public CompanyController(ILanguageService langService) : base(langService) { }

    public IActionResult Index() => View();

    public IActionResult En()
    {
        ViewData["Lang"] = "en";
        return View();
    }

    public IActionResult Vi()
    {
        ViewData["Lang"] = "vi";
        return View();
    }
}
