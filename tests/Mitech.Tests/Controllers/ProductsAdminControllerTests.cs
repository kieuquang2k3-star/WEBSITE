using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;
using Moq;

namespace Mitech.Tests.Controllers;

public class ProductsAdminControllerTests
{
    private static Mock<IWebHostEnvironment> EnvMock()
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        return mock;
    }

    [Fact]
    public async Task Index_ReturnsProductsSortedBySortOrder()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "B", SortOrder = 2 },
            new Product { TitleJa = "A", SortOrder = 1 },
            new Product { TitleJa = "C", SortOrder = 3 });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(view.Model).ToList();
        Assert.Equal(new[] { "A", "B", "C" }, products.Select(p => p.TitleJa));
    }

    [Fact]
    public void Create_Get_ReturnsView_WithNewProduct()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Product>(view.Model);
    }

    [Fact]
    public async Task Create_Post_ValidProduct_SavesAndRedirectsToIndex()
    {
        var dbName = Guid.NewGuid().ToString();
        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));
        var product = new Product { TitleJa = "New Product", SortOrder = 1 };

        var result = await controller.Create(product, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(1, await verifyCtx.Products.CountAsync());
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));
        controller.ModelState.AddModelError("TitleJa", "Required");

        var result = await controller.Create(new Product(), null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WithProduct_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            seedCtx.Products.Add(new Product { TitleJa = "Existing" });
            await seedCtx.SaveChangesAsync();
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var productId = ctx.Products.First().Id;
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = await controller.Edit(productId);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Product>(view.Model);
        Assert.Equal("Existing", model.TitleJa);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenProductMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidProduct_UpdatesAndRedirectsToIndex()
    {
        var dbName = Guid.NewGuid().ToString();
        int productId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var p = new Product { TitleJa = "Old Name" };
            seedCtx.Products.Add(p);
            await seedCtx.SaveChangesAsync();
            productId = p.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));
        var updated = new Product { Id = productId, TitleJa = "New Name" };

        var result = await controller.Edit(productId, updated, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var saved = await verifyCtx.Products.FindAsync(productId);
        Assert.Equal("New Name", saved!.TitleJa);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = await controller.Edit(1, new Product { Id = 2 }, null);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));
        controller.ModelState.AddModelError("TitleJa", "Required");

        var result = await controller.Edit(1, new Product { Id = 1 }, null);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Delete_Post_RemovesProduct_AndRedirectsToIndex()
    {
        var dbName = Guid.NewGuid().ToString();
        int productId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var p = new Product { TitleJa = "To Delete" };
            seedCtx.Products.Add(p);
            await seedCtx.SaveChangesAsync();
            productId = p.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = await controller.Delete(productId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verifyCtx.Products.Count());
    }

    [Fact]
    public async Task Delete_Post_NonExistentId_StillRedirectsToIndex()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ProductsAdminController(ctx, EnvMock().Object));

        var result = await controller.Delete(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
