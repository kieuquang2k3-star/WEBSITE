using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Extensions;
using Mitech.Web.Models;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class NewsController : BasePublicController
{
    private readonly ApplicationDbContext _db;
    private const int PageSize = 10;

    public NewsController(ApplicationDbContext db, ILanguageService langService)
        : base(langService)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        return View(await BuildListViewModel(null, page));
    }

    public async Task<IActionResult> Category(string slug, int page = 1)
    {
        var category = await _db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == slug);
        if (category is null) return NotFound();

        ViewData["Title"] = category.GetName(Lang);
        return View("Index", await BuildListViewModel(category, page));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var article = await _db.NewsArticles
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsPublished);

        if (article is null) return NotFound();

        ViewData["Title"] = article.GetTitle(Lang);

        var prev = await _db.NewsArticles
            .Where(a => a.IsPublished && a.Id < article.Id)
            .OrderByDescending(a => a.Id)
            .FirstOrDefaultAsync();

        var next = await _db.NewsArticles
            .Where(a => a.IsPublished && a.Id > article.Id)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync();

        return View(new NewsDetailViewModel
        {
            Article     = article,
            Sidebar     = await BuildSidebarAsync(),
            PrevArticle = prev,
            NextArticle = next
        });
    }

    private async Task<NewsListViewModel> BuildListViewModel(NewsCategory? category, int page)
    {
        var query = _db.NewsArticles.Include(a => a.Category).Where(a => a.IsPublished);
        if (category is not null)
            query = query.Where(a => a.CategoryId == category.Id);

        var total = await query.CountAsync();
        var articles = await query
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return new NewsListViewModel
        {
            Articles = articles,
            CurrentCategory = category,
            Sidebar = await BuildSidebarAsync(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        };
    }

    private async Task<NewsSidebarViewModel> BuildSidebarAsync()
    {
        var archiveRaw = await _db.NewsArticles
            .Where(a => a.IsPublished)
            .GroupBy(a => new { a.PublishedAt.Year, a.PublishedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync();

        return new NewsSidebarViewModel
        {
            Categories = await _db.NewsCategories.ToListAsync(),
            LatestArticles = await _db.NewsArticles
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.PublishedAt)
                .Take(5)
                .ToListAsync(),
            ArchiveMonths = archiveRaw.Select(x => (x.Year, x.Month, x.Count)).ToList()
        };
    }
}
