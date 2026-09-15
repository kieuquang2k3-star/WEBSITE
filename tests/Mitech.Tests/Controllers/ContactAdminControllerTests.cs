using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class ContactAdminControllerTests
{
    [Fact]
    public async Task Index_ReturnsMessages_OrderedByReceivedAtDesc()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com", ReceivedAt = new DateTime(2025, 1, 1) },
            new ContactMessage { Email = "b@b.com", ReceivedAt = new DateTime(2026, 1, 1) });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var messages = Assert.IsAssignableFrom<IEnumerable<ContactMessage>>(view.Model).ToList();
        Assert.Equal("b@b.com", messages[0].Email);
    }

    [Fact]
    public async Task Detail_ReturnsView_WithMessage_WhenExists()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.Add(new ContactMessage { Email = "x@x.com", IsRead = false });
        await ctx.SaveChangesAsync();
        var id = ctx.ContactMessages.First().Id;

        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        var result = await controller.Detail(id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ContactMessage>(view.Model);
    }

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenMessageMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Detail(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_MarksUnreadMessage_AsRead()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "u@u.com", IsRead = false };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        await controller.Detail(msgId);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var msg = await verifyCtx.ContactMessages.FindAsync(msgId);
        Assert.True(msg!.IsRead);
    }

    [Fact]
    public async Task Detail_AlreadyReadMessage_DoesNotChangeDbState()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "r@r.com", IsRead = true };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        await controller.Detail(msgId);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var msg = await verifyCtx.ContactMessages.FindAsync(msgId);
        Assert.True(msg!.IsRead);
        Assert.Equal(1, verifyCtx.ContactMessages.Count());
    }

    [Fact]
    public async Task Delete_Post_RemovesMessage_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "d@d.com" };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Delete(msgId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verifyCtx.ContactMessages.Count());
    }

    [Fact]
    public async Task Delete_Post_NonExistentId_StillRedirects()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Delete(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
