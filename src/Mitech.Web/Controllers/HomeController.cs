using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class HomeController : BasePublicController
{
    private readonly ApplicationDbContext _db;
    private readonly IPageContentService _content;

    public HomeController(ApplicationDbContext db, ILanguageService langService, IPageContentService content)
        : base(langService)
    {
        _db = db;
        _content = content;
    }

    public async Task<IActionResult> Index()
    {
        var lang = Lang;

        var news = await _db.NewsArticles
            .Include(a => a.Category)
            .Where(a => a.IsPublished)
            .OrderByDescending(a => a.PublishedAt)
            .Take(5)
            .ToListAsync();

        var content = await _content.GetPageAsync("home", lang);

        ViewData["BnrProducts"]  = await _content.GetAsync("home.banner.img.products",  "global");
        ViewData["BnrAbout"]     = await _content.GetAsync("home.banner.img.about",     "global");
        ViewData["BnrCompany"]   = await _content.GetAsync("home.banner.img.company",   "global");
        ViewData["BnrLocations"] = await _content.GetAsync("home.banner.img.locations", "global");

        ViewData["StrengthImg1"] = await _content.GetAsync("home.strength1.img", "global");
        ViewData["StrengthImg2"] = await _content.GetAsync("home.strength2.img", "global");
        ViewData["StrengthImg3"] = await _content.GetAsync("home.strength3.img", "global");

        return View(new HomeViewModel { LatestNews = news, Content = content });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
