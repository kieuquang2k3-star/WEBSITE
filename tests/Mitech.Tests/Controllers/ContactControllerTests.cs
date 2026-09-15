using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class ContactControllerTests
{
    private static Mock<ILanguageService> LangMock()
    {
        var m = new Mock<ILanguageService>();
        m.Setup(s => s.GetCurrent(It.IsAny<HttpContext>())).Returns("vi");
        return m;
    }

    private static ContactViewModel ValidVm() => new()
    {
        CompanyName  = "Công ty TNHH Test",
        ContactName  = "Nguyễn Văn A",
        Email        = "test@example.com",
        EmailConfirm = "test@example.com",
        Phone        = "0123456789",
        Message      = "Tin nhắn kiểm tra"
    };

    [Fact]
    public void Get_Index_ReturnsViewWithEmptyModel()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));

        var result = controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ContactViewModel>(view.Model);
        Assert.Equal(string.Empty, vm.CompanyName);
        Assert.Equal(string.Empty, vm.ContactName);
        Assert.Equal(string.Empty, vm.Message);
    }

    [Fact]
    public async Task Post_ValidModel_SavesContactMessage()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));

        await controller.Index(ValidVm());

        var saved = await ctx.ContactMessages.SingleAsync();
        Assert.Equal("Công ty TNHH Test", saved.CompanyName);
        Assert.Equal("Nguyễn Văn A", saved.ContactName);
        Assert.Equal("test@example.com", saved.Email);
        Assert.Equal("0123456789", saved.Phone);
        Assert.Equal("Tin nhắn kiểm tra", saved.Message);
    }

    [Fact]
    public async Task Post_ValidModel_RedirectsToThanks()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));

        var result = await controller.Index(ValidVm());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Thanks", redirect.ActionName);
    }

    [Fact]
    public async Task Post_InvalidModel_ReturnsViewWithSameModel()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));
        controller.ModelState.AddModelError("CompanyName", "Bắt buộc");
        var vm = new ContactViewModel();

        var result = await controller.Index(vm);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
    }

    [Fact]
    public async Task Post_InvalidModel_DoesNotSaveToDatabase()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));
        controller.ModelState.AddModelError("CompanyName", "Bắt buộc");

        await controller.Index(new ContactViewModel());

        Assert.Empty(ctx.ContactMessages);
    }

    [Fact]
    public async Task Post_ValidModel_SetsReceivedAt()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));
        var before = DateTime.UtcNow;

        await controller.Index(ValidVm());

        var saved = await ctx.ContactMessages.SingleAsync();
        Assert.True(saved.ReceivedAt >= before);
    }

    [Fact]
    public async Task Post_ValidModel_SavesFurigana_WhenProvided()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));
        var vm = ValidVm();
        vm.Furigana = "グエン バン エー";

        await controller.Index(vm);

        var saved = await ctx.ContactMessages.SingleAsync();
        Assert.Equal("グエン バン エー", saved.Furigana);
    }

    [Fact]
    public async Task Post_ValidModel_SavesOptionalFields_WhenEmpty()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));
        var vm = ValidVm();
        vm.ZipCode  = string.Empty;
        vm.Address  = string.Empty;
        vm.Furigana = string.Empty;

        await controller.Index(vm);

        var saved = await ctx.ContactMessages.SingleAsync();
        Assert.Equal(string.Empty, saved.ZipCode);
        Assert.Equal(string.Empty, saved.Address);
    }

    [Fact]
    public void Thanks_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactController(ctx, LangMock().Object));

        var result = controller.Thanks();

        Assert.IsType<ViewResult>(result);
    }
}
