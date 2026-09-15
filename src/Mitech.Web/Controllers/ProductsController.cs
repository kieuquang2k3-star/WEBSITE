using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class ProductsController : BasePublicController
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db, ILanguageService langService)
        : base(langService)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new ProductsViewModel
        {
            Products = await _db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync()
        };
        return View(vm);
    }
}
