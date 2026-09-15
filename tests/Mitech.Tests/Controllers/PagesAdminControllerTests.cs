using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class PagesAdminControllerTests
{
    [Fact]
    public async Task Index_CallsGetAllAsync_AndReturnsView()
    {
        var contents = new List<PageContent>
        {
            new() { PageKey = "home.title", Lang = "ja", Value = "テスト" }
        };
        var serviceMock = new Mock<IPageContentService>();
        serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(contents.AsReadOnly());

        var controller = ControllerTestHelper.Create(new PagesAdminController(serviceMock.Object));
        var result = await controller.Index();

        serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(contents, view.Model);
    }

    [Fact]
    public async Task Update_Post_CallsSetAsync_WithCorrectArgs_AndRedirects()
    {
        var serviceMock = new Mock<IPageContentService>();
        serviceMock.Setup(s => s.SetAsync("home.title", "ja", "新タイトル"))
            .Returns(Task.CompletedTask);

        var controller = ControllerTestHelper.Create(new PagesAdminController(serviceMock.Object));
        var result = await controller.Update("home.title", "ja", "新タイトル");

        serviceMock.Verify(s => s.SetAsync("home.title", "ja", "新タイトル"), Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
