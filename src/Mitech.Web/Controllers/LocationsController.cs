using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class LocationsController : BasePublicController
{
    public LocationsController(ILanguageService langService) : base(langService) { }

    public IActionResult Index() => View();
}
