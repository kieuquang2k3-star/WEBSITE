using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models;
using Mitech.Web.Services;

namespace Mitech.Tests.Services;

public class PageContentServiceTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAsync_ReturnsValue_WhenKeyAndLangMatch()
    {
        using var ctx = CreateInMemoryContext();
        ctx.PageContents.Add(new PageContent { PageKey = "home.title", Lang = "ja", Value = "テスト" });
        await ctx.SaveChangesAsync();

        var service = new PageContentService(ctx);
        var result = await service.GetAsync("home.title", "ja");

        Assert.Equal("テスト", result);
    }

    [Fact]
    public async Task GetAsync_FallsBackToJa_WhenLangNotFound()
    {
        using var ctx = CreateInMemoryContext();
        ctx.PageContents.Add(new PageContent { PageKey = "home.title", Lang = "ja", Value = "日本語" });
        await ctx.SaveChangesAsync();

        var service = new PageContentService(ctx);
        var result = await service.GetAsync("home.title", "en");

        Assert.Equal("日本語", result);
    }

    [Fact]
    public async Task GetAsync_ReturnsEmpty_WhenKeyNotFound()
    {
        using var ctx = CreateInMemoryContext();

        var service = new PageContentService(ctx);
        var result = await service.GetAsync("nonexistent.key", "ja");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task SetAsync_CreatesNewEntry_WhenKeyDoesNotExist()
    {
        using var ctx = CreateInMemoryContext();
        var service = new PageContentService(ctx);

        await service.SetAsync("home.title", "ja", "新タイトル");

        var saved = await ctx.PageContents.FirstOrDefaultAsync(p => p.PageKey == "home.title" && p.Lang == "ja");
        Assert.NotNull(saved);
        Assert.Equal("新タイトル", saved.Value);
    }

    [Fact]
    public async Task SetAsync_UpdatesExistingEntry_WhenKeyExists()
    {
        using var ctx = CreateInMemoryContext();
        ctx.PageContents.Add(new PageContent { PageKey = "home.title", Lang = "ja", Value = "旧タイトル" });
        await ctx.SaveChangesAsync();

        var service = new PageContentService(ctx);
        await service.SetAsync("home.title", "ja", "新タイトル");

        var updated = await ctx.PageContents.FirstAsync(p => p.PageKey == "home.title" && p.Lang == "ja");
        Assert.Equal("新タイトル", updated.Value);
        Assert.Single(ctx.PageContents);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsOnlyKeysWithMatchingPrefix()
    {
        using var ctx = CreateInMemoryContext();
        ctx.PageContents.AddRange(
            new PageContent { PageKey = "home.intro.main1", Lang = "vi", Value = "Không ngừng" },
            new PageContent { PageKey = "home.intro.main2", Lang = "vi", Value = "thách thức" },
            new PageContent { PageKey = "other.key",        Lang = "vi", Value = "other" }
        );
        await ctx.SaveChangesAsync();

        var service = new PageContentService(ctx);
        var result = await service.GetPageAsync("home", "vi");

        Assert.Equal(2, result.Count);
        Assert.Equal("Không ngừng", result["home.intro.main1"]);
        Assert.Equal("thách thức",  result["home.intro.main2"]);
        Assert.DoesNotContain("other.key", result.Keys);
    }

    [Fact]
    public async Task GetPageAsync_FallsBackToJa_ForMissingLangKeys()
    {
        using var ctx = CreateInMemoryContext();
        ctx.PageContents.AddRange(
            new PageContent { PageKey = "home.intro.main1", Lang = "ja", Value = "日本語" },
            new PageContent { PageKey = "home.intro.main1", Lang = "vi", Value = "Tiếng Việt" },
            new PageContent { PageKey = "home.intro.main2", Lang = "ja", Value = "フォールバック" }
        );
        await ctx.SaveChangesAsync();

        var service = new PageContentService(ctx);
        var result = await service.GetPageAsync("home", "vi");

        Assert.Equal("Tiếng Việt",    result["home.intro.main1"]);
        Assert.Equal("フォールバック", result["home.intro.main2"]);
    }
}
