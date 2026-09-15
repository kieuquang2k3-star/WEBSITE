using Mitech.Tests.Helpers;
using System.Net;

namespace Mitech.Tests.Integration;

public class ClientPagesTests : IClassFixture<AdminWebFactory>
{
    private readonly AdminWebFactory _factory;

    public ClientPagesTests(AdminWebFactory factory) => _factory = factory;

    public static IEnumerable<object[]> PublicRoutes =>
        new[]
        {
            new object[] { "/" },
            new object[] { "/san-pham" },
            new object[] { "/tin-tuc" },
            new object[] { "/lien-he" },
            new object[] { "/ve-chung-toi" },
            new object[] { "/cong-ty" },
            new object[] { "/cong-ty/en" },
            new object[] { "/cong-ty/vi" },
            new object[] { "/dia-diem" },
            new object[] { "/tuyen-dung" },
            new object[] { "/chinh-sach-mua-hang" },
            new object[] { "/ke-hoach-hanh-dong" },
            new object[] { "/chinh-sach-bao-mat" },
            new object[] { "/bao-mat-thong-tin" },
            new object[] { "/so-do-trang" },
        };

    [Theory]
    [MemberData(nameof(PublicRoutes))]
    public async Task Get_PublicPage_Returns200(string url)
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewsCategory_SeededSlug_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc/danh-muc/information");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewsCategory_UnknownSlug_Returns404()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc/danh-muc/khong-ton-tai");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewsDetail_SeededId_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewsDetail_UnknownId_Returns404()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_NewsList_PageParam_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc?page=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_JobDetail_SeededSlug_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tuyen-dung/van-hanh-may-cnc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_JobDetail_UnknownSlug_Returns404()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tuyen-dung/khong-ton-tai");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("vi")]
    [InlineData("ja")]
    [InlineData("en")]
    public async Task Get_LangSwitch_ValidCode_Redirects(string lang)
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/lang/{lang}");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public async Task Get_LangSwitch_InvalidCode_RedirectsToHome()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/lang/xx");
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Equal("/", location);
    }

    [Fact]
    public async Task Get_ContactPage_ContainsAntiForgeryToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/lien-he");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task Get_HomePage_ContainsSeededArticleLink()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Verify the seeded article link appears in the news section
        Assert.Contains("/tin-tuc/1", html);
    }

    [Fact]
    public async Task Get_NewsCategory_EventSlug_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tin-tuc/danh-muc/event");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_SecondJobDetail_SeededSlug_Returns200()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tuyen-dung/kiem-tra-ngoai-quan-vis");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ProductsPage_ContainsSeededProductImage()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/san-pham");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Verify seeded product image path appears on the page
        Assert.Contains("img_product_02-2.jpg", html);
    }

    [Fact]
    public async Task Get_TuyenDungPage_ContainsSeededJobSlug()
    {
        var client = _factory.CreateAnonymousClient();
        var response = await client.GetAsync("/tuyen-dung");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        // Verify seeded job slug appears in links on the recruitment page
        Assert.Contains("van-hanh-may-cnc", html);
    }
}
