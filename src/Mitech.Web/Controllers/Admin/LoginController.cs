using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mitech.Web.Controllers.Admin;

public class LoginController : Controller  // intentionally NOT extending AdminBaseController — login must be public
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginController(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string email, string password, string? returnUrl)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/admin");

        ViewData["Error"] = "Email hoặc mật khẩu không chính xác.";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
