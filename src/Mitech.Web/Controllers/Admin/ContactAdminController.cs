using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;

namespace Mitech.Web.Controllers.Admin;

public class ContactAdminController : AdminBaseController
{
    private readonly ApplicationDbContext _db;

    public ContactAdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var messages = await _db.ContactMessages
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync();
        return View(messages);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var message = await _db.ContactMessages.FindAsync(id);
        if (message is null) return NotFound();

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return View(message);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _db.ContactMessages.FindAsync(id);
        if (message is not null)
        {
            _db.ContactMessages.Remove(message);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
