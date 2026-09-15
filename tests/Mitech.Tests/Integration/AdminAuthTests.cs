using Mitech.Tests.Helpers;
using System.Net;

namespace Mitech.Tests.Integration;

public class AdminAuthTests : IClassFixture<AdminWebFactory>
{
    private readonly AdminWebFactory _factory;

    public AdminAuthTests(AdminWebFactory factory) => _factory = factory;

    public static IEnumerable<object[]> ProtectedRoutes =>
        new[]
        {
            new object[] { "/admin" },
            new object[] { "/admin/dashboard" },
            new object[] { "/admin/products" },
            new object[] { "/admin/products/create" },
            new object[] { "/admin/news" },
            new object[] { "/admin/news/create" },
            new object[] { "/admin/pages" },
            new object[] { "/admin/contact" }
        };

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Get_ProtectedRoute_WithoutAuth_RedirectsToLogin(string route)
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("/admin/login", location, StringComparison.OrdinalIgnoreCase);
    }
}
