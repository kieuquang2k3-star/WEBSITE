using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers;
using Mitech.Web.Models;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class ProductsControllerTests
{
    private static Mock<ILanguageService> LangMock()
    {
        var m = new Mock<ILanguageService>();
        m.Setup(s => s.GetCurrent(It.IsAny<HttpContext>())).Returns("vi");
        return m;
    }

    [Fact]
    public async Task Index_ReturnsOnlyActiveProducts()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "Active", IsActive = true, SortOrder = 1 },
            new Product { TitleJa = "Inactive", IsActive = false, SortOrder = 2 }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ProductsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ProductsViewModel>(view.Model);
        Assert.Single(vm.Products);
        Assert.Equal("Active", vm.Products[0].TitleJa);
    }

    [Fact]
    public async Task Index_ProductsOrderedBySortOrder()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "Second", IsActive = true, SortOrder = 2 },
            new Product { TitleJa = "First", IsActive = true, SortOrder = 1 },
            new Product { TitleJa = "Third", IsActive = true, SortOrder = 3 }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ProductsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ProductsViewModel>(view.Model);
        Assert.Equal("First", vm.Products[0].TitleJa);
        Assert.Equal("Second", vm.Products[1].TitleJa);
        Assert.Equal("Third", vm.Products[2].TitleJa);
    }

    [Fact]
    public async Task Index_EmptyDatabase_ReturnsEmptyList()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();

        var controller = ControllerTestHelper.Create(new ProductsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ProductsViewModel>(view.Model);
        Assert.Empty(vm.Products);
    }

    [Fact]
    public async Task Index_AllInactive_ReturnsEmptyList()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "A", IsActive = false, SortOrder = 1 },
            new Product { TitleJa = "B", IsActive = false, SortOrder = 2 }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ProductsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ProductsViewModel>(view.Model);
        Assert.Empty(vm.Products);
    }

    [Fact]
    public async Task Index_MultilingualFields_AreReturned()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.Add(new Product
        {
            TitleJa = "部品", TitleEn = "Part", TitleVi = "Linh kiện",
            SpecMaterial = "鉄", SpecMaterialEn = "Iron", SpecMaterialVi = "Sắt",
            IsActive = true, SortOrder = 1
        });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ProductsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<ProductsViewModel>(view.Model);
        var product = vm.Products[0];
        Assert.Equal("部品", product.TitleJa);
        Assert.Equal("Part", product.TitleEn);
        Assert.Equal("Linh kiện", product.TitleVi);
    }
}
