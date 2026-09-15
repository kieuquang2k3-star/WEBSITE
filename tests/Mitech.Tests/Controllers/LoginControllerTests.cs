using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Moq;

namespace Mitech.Tests.Controllers;

public class LoginControllerTests
{
    private static (Mock<SignInManager<IdentityUser>> signInMock, LoginController controller) Setup()
    {
        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = new Mock<SignInManager<IdentityUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        var controller = ControllerTestHelper.Create(new LoginController(signInManager.Object));
        return (signInManager, controller);
    }

    [Fact]
    public void Index_Get_ReturnsView()
    {
        var (_, controller) = Setup();
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Post_ValidCredentials_RedirectsToAdmin()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync("test@test.com", "pass", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Index("test@test.com", "pass", null);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/admin", redirect.Url);
    }

    [Fact]
    public async Task Index_Post_ValidCredentials_WithReturnUrl_RedirectsToReturnUrl()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Index("a@b.com", "pw", "/admin/products");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/admin/products", redirect.Url);
    }

    [Fact]
    public async Task Index_Post_InvalidCredentials_ReturnsViewWithError()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await controller.Index("a@b.com", "wrong", null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.ViewData["Error"]);
    }

    [Fact]
    public async Task Logout_Post_SignsOutAndRedirectsHome()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await controller.Logout();

        signInMock.Verify(m => m.SignOutAsync(), Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }
}
