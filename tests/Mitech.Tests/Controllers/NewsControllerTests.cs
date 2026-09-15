using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers;
using Mitech.Web.Models;
using Mitech.Web.Models.ViewModels;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class NewsControllerTests
{
    private static Mock<ILanguageService> LangMock(string lang = "vi")
    {
        var m = new Mock<ILanguageService>();
        m.Setup(s => s.GetCurrent(It.IsAny<HttpContext>())).Returns(lang);
        return m;
    }

    private static async Task<(Mitech.Web.Data.ApplicationDbContext ctx, NewsCategory cat)>
        SeedAsync(string dbName, int count = 3)
    {
        var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        await ctx.SaveChangesAsync();
        for (int i = 0; i < count; i++)
            ctx.NewsArticles.Add(new NewsArticle
            {
                TitleJa = $"Article {i}",
                TitleVi = $"Bài {i}",
                CategoryId = cat.Id,
                PublishedAt = DateTime.UtcNow.AddDays(-i),
                IsPublished = true
            });
        await ctx.SaveChangesAsync();
        return (ctx, cat);
    }

    [Fact]
    public async Task Index_ExcludesDraftArticles()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Published", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = true },
            new NewsArticle { TitleJa = "Draft", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = false }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Single(vm.Articles);
        Assert.Equal("Published", vm.Articles[0].TitleJa);
    }

    [Fact]
    public async Task Index_ArticlesOrderedByPublishedAtDesc()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Older", CategoryId = cat.Id, PublishedAt = new DateTime(2025, 1, 1), IsPublished = true },
            new NewsArticle { TitleJa = "Newer", CategoryId = cat.Id, PublishedAt = new DateTime(2026, 1, 1), IsPublished = true }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Equal("Newer", vm.Articles[0].TitleJa);
        Assert.Equal("Older", vm.Articles[1].TitleJa);
    }

    [Fact]
    public async Task Index_Page1_Returns10Articles_WhenMoreThan10Exist()
    {
        var dbName = Guid.NewGuid().ToString();
        var (ctx, _) = await SeedAsync(dbName, 15);

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index(page: 1);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Equal(10, vm.Articles.Count);
        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(2, vm.TotalPages);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Index_Page2_ReturnsRemainingArticles()
    {
        var dbName = Guid.NewGuid().ToString();
        var (ctx, _) = await SeedAsync(dbName, 15);

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index(page: 2);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Equal(5, vm.Articles.Count);
        Assert.Equal(2, vm.CurrentPage);

        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task Index_EmptyDatabase_ReturnsEmptyList()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Empty(vm.Articles);
        Assert.Equal(0, vm.TotalPages);
    }

    [Fact]
    public async Task Category_UnknownSlug_ReturnsNotFound()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Category("khong-ton-tai");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Category_ValidSlug_FiltersArticlesByCategory()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat1 = new NewsCategory { Slug = "tech", NameJa = "技術", NameEn = "Tech", NameVi = "Công nghệ" };
        var cat2 = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.AddRange(cat1, cat2);
        await ctx.SaveChangesAsync();
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Tech Article", CategoryId = cat1.Id, PublishedAt = DateTime.UtcNow, IsPublished = true },
            new NewsArticle { TitleJa = "Info Article", CategoryId = cat2.Id, PublishedAt = DateTime.UtcNow, IsPublished = true }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Category("tech");

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Single(vm.Articles);
        Assert.Equal("Tech Article", vm.Articles[0].TitleJa);
    }

    [Fact]
    public async Task Category_ExcludesDraftArticles()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        await ctx.SaveChangesAsync();
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Published", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = true },
            new NewsArticle { TitleJa = "Draft", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = false }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Category("info");

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Single(vm.Articles);
    }

    [Fact]
    public async Task Detail_UnknownId_ReturnsNotFound()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Detail(9999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_UnpublishedArticle_ReturnsNotFound()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.Add(new NewsArticle
        {
            TitleJa = "Draft", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = false
        });
        await ctx.SaveChangesAsync();

        var article = await ctx.NewsArticles.FirstAsync();
        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Detail(article.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_PublishedArticle_ReturnsViewWithModel()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.Add(new NewsArticle
        {
            TitleJa = "Test Article", TitleVi = "Bài kiểm tra", CategoryId = cat.Id,
            PublishedAt = DateTime.UtcNow, IsPublished = true
        });
        await ctx.SaveChangesAsync();

        var article = await ctx.NewsArticles.FirstAsync();
        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Detail(article.Id);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsDetailViewModel>(view.Model);
        Assert.Equal("Test Article", vm.Article.TitleJa);
        Assert.Equal("Bài kiểm tra", vm.Article.TitleVi);
    }

    [Fact]
    public async Task Detail_Sidebar_PopulatesCategories()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.Add(new NewsArticle
        {
            TitleJa = "Article", CategoryId = cat.Id, PublishedAt = DateTime.UtcNow, IsPublished = true
        });
        await ctx.SaveChangesAsync();

        var article = await ctx.NewsArticles.FirstAsync();
        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Detail(article.Id);

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsDetailViewModel>(view.Model);
        Assert.NotNull(vm.Sidebar);
        Assert.NotEmpty(vm.Sidebar.Categories);
        Assert.NotEmpty(vm.Sidebar.LatestArticles);
    }

    [Fact]
    public async Task Index_Sidebar_ArchiveGroupsByYearMonth()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var cat = new NewsCategory { Slug = "info", NameJa = "情報", NameEn = "Info", NameVi = "Thông tin" };
        ctx.NewsCategories.Add(cat);
        ctx.NewsArticles.AddRange(
            new NewsArticle { TitleJa = "Jan", CategoryId = cat.Id, PublishedAt = new DateTime(2026, 1, 15), IsPublished = true },
            new NewsArticle { TitleJa = "Mar", CategoryId = cat.Id, PublishedAt = new DateTime(2026, 3, 10), IsPublished = true }
        );
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new NewsController(ctx, LangMock().Object));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<NewsListViewModel>(view.Model);
        Assert.Equal(2, vm.Sidebar.ArchiveMonths.Count);
    }
}
