using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models;

namespace Mitech.Web.Controllers.Admin;

public class NewsAdminController : AdminBaseController
{
    private readonly ApplicationDbContext _db;

    public NewsAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var articles = await _db.NewsArticles
            .Include(a => a.Category)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
        return View(articles);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Categories"] = await _db.NewsCategories.ToListAsync();
        return View(new NewsArticle { PublishedAt = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewsArticle article)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Categories"] = await _db.NewsCategories.ToListAsync();
            return View(article);
        }
        article.CreatedAt = DateTime.UtcNow;
        _db.NewsArticles.Add(article);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article is null) return NotFound();
        ViewData["Categories"] = await _db.NewsCategories.ToListAsync();
        return View(article);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NewsArticle article)
    {
        if (id != article.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewData["Categories"] = await _db.NewsCategories.ToListAsync();
            return View(article);
        }
        _db.Update(article);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var article = await _db.NewsArticles.FindAsync(id);
        if (article is not null)
        {
            _db.NewsArticles.Remove(article);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
