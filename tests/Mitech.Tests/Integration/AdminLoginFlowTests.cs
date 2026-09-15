using Mitech.Tests.Helpers;
using System.Net;

namespace Mitech.Tests.Integration;

public class AdminLoginFlowTests : IClassFixture<AdminWebFactory>
{
    private readonly AdminWebFactory _factory;

    public AdminLoginFlowTests(AdminWebFactory factory) => _factory = factory;

    [Fact]
    public async Task PostLogin_ValidCredentials_RedirectsToAdmin()
    {
        var client = _factory.CreateAnonymousClient();

        var getResp = await client.GetAsync("/admin/login");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = AdminWebFactory.ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "admin@mitec.co.jp"),
            new KeyValuePair<string, string>("password", "Admin@123456"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var loginResp = await client.PostAsync("/admin/login", form);

        Assert.Equal(HttpStatusCode.Found, loginResp.StatusCode);
        var location = loginResp.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/admin", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostLogin_InvalidCredentials_Returns200WithError()
    {
        var client = _factory.CreateAnonymousClient();

        var getResp = await client.GetAsync("/admin/login");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = AdminWebFactory.ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "wrong@email.com"),
            new KeyValuePair<string, string>("password", "wrongpassword"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var loginResp = await client.PostAsync("/admin/login", form);

        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        var body = await loginResp.Content.ReadAsStringAsync();
        Assert.Contains("không chính xác", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostLogout_WhenAuthenticated_RedirectsToHome()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var getResp = await client.GetAsync("/admin");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = AdminWebFactory.ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var logoutResp = await client.PostAsync("/admin/login/logout", form);

        Assert.Equal(HttpStatusCode.Found, logoutResp.StatusCode);
        var location = logoutResp.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/", location);
    }
}
