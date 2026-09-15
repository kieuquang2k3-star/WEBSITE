using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Data;
using Mitech.Web.Models;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers;

public class ContactController : BasePublicController
{
    private readonly ApplicationDbContext _db;

    public ContactController(ApplicationDbContext db, ILanguageService langService)
        : base(langService)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index() => View(new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        _db.ContactMessages.Add(new ContactMessage
        {
            CompanyName = vm.CompanyName,
            ContactName = vm.ContactName,
            Furigana = vm.Furigana,
            Email = vm.Email,
            Phone = vm.Phone,
            ZipCode = vm.ZipCode,
            Address = vm.Address,
            Message = vm.Message,
            ReceivedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Thanks");
    }

    public IActionResult Thanks() => View();
}
