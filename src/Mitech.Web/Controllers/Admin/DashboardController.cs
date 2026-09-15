using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;

namespace Mitech.Web.Controllers.Admin;

public class DashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ProductCount"] = await _db.Products.CountAsync();
        ViewData["NewsCount"] = await _db.NewsArticles.CountAsync();
        ViewData["MessageCount"] = await _db.ContactMessages.CountAsync();
        ViewData["UnreadCount"] = await _db.ContactMessages.CountAsync(m => !m.IsRead);
        return View();
    }
}
