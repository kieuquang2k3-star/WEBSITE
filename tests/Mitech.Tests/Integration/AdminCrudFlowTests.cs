using Mitech.Tests.Helpers;
using System.Net;

namespace Mitech.Tests.Integration;

public class AdminCrudFlowTests : IClassFixture<AdminWebFactory>
{
    private readonly AdminWebFactory _factory;

    public AdminCrudFlowTests(AdminWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_Get_Returns200_WhenAuthenticated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ProductsIndex_Get_Returns200_WhenAuthenticated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/products");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ProductCreate_Post_ValidData_RedirectsToIndex()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var getResp = await client.GetAsync("/admin/products/create");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = AdminWebFactory.ExtractAntiForgeryToken(html);

        // The admin layout renders a logout <form> first (action="/admin/Login/Logout"),
        // followed by the page-specific create form. Find the last form's action URL.
        var formActionMatches = System.Text.RegularExpressions.Regex.Matches(
            html, @"<form[^>]+action=""([^""]+)""");
        var postUrl = formActionMatches.Count > 0
            ? formActionMatches[^1].Groups[1].Value
            : "/admin/products/create";

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("TitleJa", "テスト製品"),
            new KeyValuePair<string, string>("TitleEn", "Test Product"),
            new KeyValuePair<string, string>("TitleVi", "Sản phẩm test"),
            new KeyValuePair<string, string>("SpecName", "Body"),
            new KeyValuePair<string, string>("SpecMaterial", "Aluminum"),
            new KeyValuePair<string, string>("SpecDimension", "100mm"),
            new KeyValuePair<string, string>("SortOrder", "99"),
            new KeyValuePair<string, string>("IsActive", "true"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var postResp = await client.PostAsync(postUrl, form);

        Assert.Equal(HttpStatusCode.Found, postResp.StatusCode);
        Assert.Contains("/admin/products", postResp.Headers.Location?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductsIndex_AfterCreate_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/products");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task NewsIndex_Get_Returns200_WhenAuthenticated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/news");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task NewsCreate_Post_ValidData_RedirectsToIndex()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var getResp = await client.GetAsync("/admin/news/create");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = AdminWebFactory.ExtractAntiForgeryToken(html);

        // The admin layout renders a logout <form> first; use the last form's action URL.
        var formActionMatches = System.Text.RegularExpressions.Regex.Matches(
            html, @"<form[^>]+action=""([^""]+)""");
        var postUrl = formActionMatches.Count > 0
            ? formActionMatches[^1].Groups[1].Value
            : "/admin/news/create";

        // SeedData seeds categories — get first one from the create form (category id=1 or first seeded)
        var catMatch = System.Text.RegularExpressions.Regex.Match(
            html, @"<option[^>]+value=""(\d+)""");
        var catId = catMatch.Success ? catMatch.Groups[1].Value : "1";

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("TitleJa", "テストニュース"),
            new KeyValuePair<string, string>("TitleEn", "Test News"),
            new KeyValuePair<string, string>("TitleVi", "Tin tức test"),
            new KeyValuePair<string, string>("BodyJa", "Body content"),
            new KeyValuePair<string, string>("BodyEn", "Body content"),
            new KeyValuePair<string, string>("BodyVi", "Nội dung"),
            new KeyValuePair<string, string>("CategoryId", catId),
            new KeyValuePair<string, string>("PublishedAt", "2026-06-05"),
            new KeyValuePair<string, string>("IsPublished", "true"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var postResp = await client.PostAsync(postUrl, form);

        Assert.Equal(HttpStatusCode.Found, postResp.StatusCode);
    }

    [Fact]
    public async Task ContactIndex_Get_Returns200_WhenAuthenticated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/contact");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task PagesIndex_Get_Returns200_WhenAuthenticated()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resp = await client.GetAsync("/admin/pages");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
