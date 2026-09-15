using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mitech.Web.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace Mitech.Tests.Helpers;

public class AdminWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // EF Core 9 uses IDbContextOptionsConfiguration<TContext> internally.
            // Remove ALL service descriptors related to ApplicationDbContext and its options
            // to prevent multiple database provider conflicts (e.g. Sqlite + InMemory).
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Any(a => a == typeof(ApplicationDbContext))) ||
                    (d.ServiceType.FullName != null &&
                     d.ServiceType.FullName.Contains("DbContextOptionsConfiguration") &&
                     d.ServiceType.FullName.Contains("ApplicationDbContext")))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateAnonymousClient();

        var getResp = await client.GetAsync("/admin/login");
        var html = await getResp.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "admin@mitec.co.jp"),
            new KeyValuePair<string, string>("password", "Admin@123456"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var loginResp = await client.PostAsync("/admin/login", form);
        if (loginResp.StatusCode != HttpStatusCode.Found)
            throw new InvalidOperationException(
                $"Test login failed. Status: {loginResp.StatusCode}. " +
                "Ensure SeedData has run and admin@mitec.co.jp / Admin@123456 are seeded.");

        return client;
    }

    private static readonly Regex AntiForgeryTokenRegex = new(
        @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""",
        RegexOptions.Compiled);

    public static string ExtractAntiForgeryToken(string html)
    {
        var match = AntiForgeryTokenRegex.Match(html);
        if (!match.Success)
            throw new InvalidOperationException(
                "Anti-forgery token not found in HTML. Check that the form renders correctly.");
        return match.Groups[1].Value;
    }
}
