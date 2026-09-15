using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class AboutController : BasePublicController
{
    public AboutController(ILanguageService langService) : base(langService) { }

    public IActionResult Index() => View();
}
