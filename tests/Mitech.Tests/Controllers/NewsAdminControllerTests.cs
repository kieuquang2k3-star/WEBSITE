using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class NewsAdminControllerTests
{
    private static async Task<NewsCategory> SeedCategoryAsync(
        Mitech.Web.Data.ApplicationDbContext ctx, string slug = "info")
    {
        var cat = new NewsCategory { Slug = slug, NameJa = "テスト", NameEn = "Test", NameVi = "Test" };
        ctx.NewsCategories.Add(cat);
        await ctx.SaveChangesAsync();
        return cat;
    }

    [Fact]
    public async Task Index_ReturnsArticles_OrderedByPublishedAtDesc()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = await SeedCategoryAsync(ctx);
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Older", CategoryId = cat.Id, PublishedAt = new DateTime(2025, 1, 1) },
            new NewsArticle { TitleJa = "Newer", CategoryId = cat.Id, PublishedAt = new DateTime(2026, 1, 1) });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var articles = Assert.IsAssignableFrom<IEnumerable<NewsArticle>>(view.Model).ToList();
        Assert.Equal("Newer", articles[0].TitleJa);
        Assert.Equal("Older", articles[1].TitleJa);
    }

    [Fact]
    public async Task Create_Get_ReturnsView_WithCategories()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        await SeedCategoryAsync(ctx);

        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        var result = await controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        var categories = view.ViewData["Categories"] as List<NewsCategory>;
        Assert.NotNull(categories);
        Assert.Single(categories);
    }

    [Fact]
    public async Task Create_Post_ValidArticle_SetsCreatedAt_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int catId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var cat = await SeedCategoryAsync(seedCtx);
            catId = cat.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        var before = DateTime.UtcNow.AddSeconds(-1);
        var article = new NewsArticle
        {
            TitleJa = "Test",
            CategoryId = catId,
            PublishedAt = DateTime.Today
        };

        var result = await controller.Create(article);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var saved = await verifyCtx.NewsArticles.FirstAsync();
        Assert.True(saved.CreatedAt >= before);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView_WithCategories()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        await SeedCategoryAsync(ctx);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        controller.ModelState.AddModelError("TitleJa", "Required");

        var result = await controller.Create(new NewsArticle());

        var view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.ViewData["Categories"]);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WithArticle_WhenExists()
    {
        var dbName = Guid.NewGuid().ToString();
        int articleId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var cat = await SeedCategoryAsync(seedCtx);
            var a = new NewsArticle { TitleJa = "Article", CategoryId = cat.Id, PublishedAt = DateTime.Today };
            seedCtx.NewsArticles.Add(a);
            await seedCtx.SaveChangesAsync();
            articleId = a.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));

        var result = await controller.Edit(articleId);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<NewsArticle>(view.Model);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));

        var result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidArticle_UpdatesAndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int articleId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var cat = await SeedCategoryAsync(seedCtx);
            var a = new NewsArticle { TitleJa = "Old", CategoryId = cat.Id, PublishedAt = DateTime.Today };
            seedCtx.NewsArticles.Add(a);
            await seedCtx.SaveChangesAsync();
            articleId = a.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        var updated = new NewsArticle
        {
            Id = articleId, TitleJa = "Updated", CategoryId = 1, PublishedAt = DateTime.Today
        };

        var result = await controller.Edit(articleId, updated);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var saved = await verifyCtx.NewsArticles.FindAsync(articleId);
        Assert.Equal("Updated", saved!.TitleJa);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));

        var result = await controller.Edit(1, new NewsArticle { Id = 2, PublishedAt = DateTime.Today });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView_WithCategories()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        await SeedCategoryAsync(ctx);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));
        controller.ModelState.AddModelError("TitleJa", "Required");

        var result = await controller.Edit(1, new NewsArticle { Id = 1, PublishedAt = DateTime.Today });

        var view = Assert.IsType<ViewResult>(result);
        Assert.NotNull(view.ViewData["Categories"]);
    }

    [Fact]
    public async Task Delete_Post_RemovesArticle_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int articleId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var cat = await SeedCategoryAsync(seedCtx);
            var a = new NewsArticle { TitleJa = "Del", CategoryId = cat.Id, PublishedAt = DateTime.Today };
            seedCtx.NewsArticles.Add(a);
            await seedCtx.SaveChangesAsync();
            articleId = a.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));

        var result = await controller.Delete(articleId);

        Assert.IsType<RedirectToActionResult>(result);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verifyCtx.NewsArticles.Count());
    }

    [Fact]
    public async Task Delete_Post_NonExistentId_StillRedirects()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new NewsAdminController(ctx));

        var result = await controller.Delete(999);

        Assert.IsType<RedirectToActionResult>(result);
    }
}
