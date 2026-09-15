using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_ReturnsView_WithZeroCounts_WhenDbEmpty()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new DashboardController(ctx));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(0, view.ViewData["ProductCount"]);
        Assert.Equal(0, view.ViewData["NewsCount"]);
        Assert.Equal(0, view.ViewData["MessageCount"]);
        Assert.Equal(0, view.ViewData["UnreadCount"]);
    }

    [Fact]
    public async Task Index_ReturnsCorrectCounts()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "A" },
            new Product { TitleJa = "B" });
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com" },
            new ContactMessage { Email = "b@b.com" },
            new ContactMessage { Email = "c@c.com" });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new DashboardController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(2, view.ViewData["ProductCount"]);
        Assert.Equal(0, view.ViewData["NewsCount"]);
        Assert.Equal(3, view.ViewData["MessageCount"]);
    }

    [Fact]
    public async Task Index_UnreadCount_OnlyCountsUnreadMessages()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com", IsRead = false },
            new ContactMessage { Email = "b@b.com", IsRead = true },
            new ContactMessage { Email = "c@c.com", IsRead = false });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new DashboardController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(3, view.ViewData["MessageCount"]);
        Assert.Equal(2, view.ViewData["UnreadCount"]);
    }
}
