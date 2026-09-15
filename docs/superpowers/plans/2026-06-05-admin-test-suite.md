# Admin Test Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write unit tests for all 6 admin controllers and integration tests covering auth, login flow, and CRUD HTTP responses.

**Architecture:** Unit tests call controller action methods directly using InMemory DB + Moq mocks; integration tests spin up the full ASP.NET Core pipeline with `WebApplicationFactory<Program>` and an InMemory DB, sending real HTTP requests.

**Tech Stack:** xUnit, Moq, EF Core InMemory, Microsoft.AspNetCore.Mvc.Testing, ASP.NET Core 9

---

## File Map

| Action | File |
|---|---|
| Modify | `tests/Mitech.Tests/Mitech.Tests.csproj` |
| Modify | `src/Mitech.Web/Program.cs` |
| Modify | `src/Mitech.Web/Data/SeedData.cs` |
| Create | `tests/Mitech.Tests/Helpers/ControllerTestHelper.cs` |
| Create | `tests/Mitech.Tests/Helpers/AdminWebFactory.cs` |
| Create | `tests/Mitech.Tests/Controllers/LoginControllerTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/DashboardControllerTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/ProductsAdminControllerTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/NewsAdminControllerTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/ContactAdminControllerTests.cs` |
| Create | `tests/Mitech.Tests/Controllers/PagesAdminControllerTests.cs` |
| Create | `tests/Mitech.Tests/Integration/AdminAuthTests.cs` |
| Create | `tests/Mitech.Tests/Integration/AdminLoginFlowTests.cs` |
| Create | `tests/Mitech.Tests/Integration/AdminCrudFlowTests.cs` |

---

### Task 1: Setup — packages, Program.cs, SeedData.cs

**Files:**
- Modify: `tests/Mitech.Tests/Mitech.Tests.csproj`
- Modify: `src/Mitech.Web/Program.cs`
- Modify: `src/Mitech.Web/Data/SeedData.cs`

- [ ] **Step 1: Add Moq and Microsoft.AspNetCore.Mvc.Testing packages**

Edit `tests/Mitech.Tests/Mitech.Tests.csproj` — add to the `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.5" />
```

Full csproj after edit:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.5" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.5" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Mitech.Web\Mitech.Web.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Expose Program class and fix HTTPS redirect**

`src/Mitech.Web/Program.cs` — two changes:
1. Move `app.UseHttpsRedirection()` inside the `!IsDevelopment()` block so test environment (which uses Development) doesn't redirect.
2. Add `public partial class Program { }` at the end so `WebApplicationFactory<Program>` can reference it.

Replace the relevant section:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
```
→
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

And add at the very end of `Program.cs` (after `app.Run();`):
```csharp
public partial class Program { }
```

- [ ] **Step 3: Fix SeedData to handle InMemory DB**

`src/Mitech.Web/Data/SeedData.cs` — replace the DB initialization block at the top of `InitializeAsync`:

```csharp
// OLD:
var isSqlite = db.Database.ProviderName?.Contains("Sqlite") == true;
if (isSqlite)
    await db.Database.EnsureCreatedAsync();
else
    await db.Database.MigrateAsync();
```
→
```csharp
// NEW:
var providerName = db.Database.ProviderName ?? string.Empty;
if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase)
    || providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    await db.Database.EnsureCreatedAsync();
else
    await db.Database.MigrateAsync();
```

- [ ] **Step 4: Restore packages**

```
cd tests/Mitech.Tests
dotnet restore
```

Expected: `Restore succeeded.`

- [ ] **Step 5: Verify build**

```
dotnet build tests/Mitech.Tests
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add tests/Mitech.Tests/Mitech.Tests.csproj src/Mitech.Web/Program.cs src/Mitech.Web/Data/SeedData.cs
git commit -m "chore(tests): add Moq + Mvc.Testing packages, fix Program/SeedData for test environment"
```

---

### Task 2: Test Helpers

**Files:**
- Create: `tests/Mitech.Tests/Helpers/ControllerTestHelper.cs`
- Create: `tests/Mitech.Tests/Helpers/AdminWebFactory.cs`

- [ ] **Step 1: Create ControllerTestHelper**

`tests/Mitech.Tests/Helpers/ControllerTestHelper.cs`:
```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Moq;

namespace Mitech.Tests.Helpers;

public static class ControllerTestHelper
{
    public static T Create<T>(T controller) where T : Controller
    {
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    public static ApplicationDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
```

- [ ] **Step 2: Create AdminWebFactory**

`tests/Mitech.Tests/Helpers/AdminWebFactory.cs`:
```csharp
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
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) services.Remove(descriptor);

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
        Assert.Equal(HttpStatusCode.Found, loginResp.StatusCode);

        return client;
    }

    public static string ExtractAntiForgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"<input[^>]+name=""__RequestVerificationToken""[^>]+value=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
```

- [ ] **Step 3: Build to verify helpers compile**

```
dotnet build tests/Mitech.Tests
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add tests/Mitech.Tests/Helpers/
git commit -m "test(helpers): add ControllerTestHelper and AdminWebFactory"
```

---

### Task 3: LoginController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/LoginControllerTests.cs`

- [ ] **Step 1: Create LoginControllerTests.cs**

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Moq;

namespace Mitech.Tests.Controllers;

public class LoginControllerTests
{
    private static (Mock<SignInManager<IdentityUser>> signInMock, LoginController controller) Setup()
    {
        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        var signInManager = new Mock<SignInManager<IdentityUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        var controller = ControllerTestHelper.Create(new LoginController(signInManager.Object));
        return (signInManager, controller);
    }

    [Fact]
    public void Index_Get_ReturnsView()
    {
        var (_, controller) = Setup();
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_Post_ValidCredentials_RedirectsToAdmin()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync("test@test.com", "pass", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Index("test@test.com", "pass", null);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/admin", redirect.Url);
    }

    [Fact]
    public async Task Index_Post_ValidCredentials_WithReturnUrl_RedirectsToReturnUrl()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await controller.Index("a@b.com", "pw", "/admin/products");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/admin/products", redirect.Url);
    }

    [Fact]
    public async Task Index_Post_InvalidCredentials_ReturnsViewWithError()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await controller.Index("a@b.com", "wrong", null);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.ViewData["Error"]);
    }

    [Fact]
    public async Task Logout_Post_SignsOutAndRedirectsHome()
    {
        var (signInMock, controller) = Setup();
        signInMock.Setup(m => m.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await controller.Logout();

        signInMock.Verify(m => m.SignOutAsync(), Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }
}
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~LoginControllerTests" -v normal
```

Expected: `5 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Controllers/LoginControllerTests.cs
git commit -m "test(admin): LoginController unit tests (5 cases)"
```

---

### Task 4: DashboardController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/DashboardControllerTests.cs`

- [ ] **Step 1: Create DashboardControllerTests.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class DashboardControllerTests
{
    [Fact]
    public async Task Index_ReturnsView_WithZeroCounts_WhenDbEmpty()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new DashboardController(ctx));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(0, view.ViewData["ProductCount"]);
        Assert.Equal(0, view.ViewData["NewsCount"]);
        Assert.Equal(0, view.ViewData["MessageCount"]);
        Assert.Equal(0, view.ViewData["UnreadCount"]);
    }

    [Fact]
    public async Task Index_ReturnsCorrectCounts()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.Products.AddRange(
            new Product { TitleJa = "A" },
            new Product { TitleJa = "B" });
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com" },
            new ContactMessage { Email = "b@b.com" },
            new ContactMessage { Email = "c@c.com" });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new DashboardController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(2, view.ViewData["ProductCount"]);
        Assert.Equal(0, view.ViewData["NewsCount"]);
        Assert.Equal(3, view.ViewData["MessageCount"]);
    }

    [Fact]
    public async Task Index_UnreadCount_OnlyCountsUnreadMessages()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com", IsRead = false },
            new ContactMessage { Email = "b@b.com", IsRead = true },
            new ContactMessage { Email = "c@c.com", IsRead = false });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new DashboardController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(3, view.ViewData["MessageCount"]);
        Assert.Equal(2, view.ViewData["UnreadCount"]);
    }
}
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~DashboardControllerTests" -v normal
```

Expected: `3 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Controllers/DashboardControllerTests.cs
git commit -m "test(admin): DashboardController unit tests (3 cases)"
```

---

### Task 5: ProductsAdminController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/ProductsAdminControllerTests.cs`

- [ ] **Step 1: Create ProductsAdminControllerTests.cs**

```csharp
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
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~ProductsAdminControllerTests" -v normal
```

Expected: `11 passed, 0 failed`

- [ ] **Step 4: Commit**

```bash
git add tests/Mitech.Tests/Controllers/ProductsAdminControllerTests.cs
git commit -m "test(admin): ProductsAdminController unit tests (11 cases)"
```

---

### Task 6: NewsAdminController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/NewsAdminControllerTests.cs`

- [ ] **Step 1: Create NewsAdminControllerTests.cs**

```csharp
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
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~NewsAdminControllerTests" -v normal
```

Expected: `11 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Controllers/NewsAdminControllerTests.cs
git commit -m "test(admin): NewsAdminController unit tests (11 cases)"
```

---

### Task 7: ContactAdminController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/ContactAdminControllerTests.cs`

- [ ] **Step 1: Create ContactAdminControllerTests.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;

namespace Mitech.Tests.Controllers;

public class ContactAdminControllerTests
{
    [Fact]
    public async Task Index_ReturnsMessages_OrderedByReceivedAtDesc()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.AddRange(
            new ContactMessage { Email = "a@a.com", ReceivedAt = new DateTime(2025, 1, 1) },
            new ContactMessage { Email = "b@b.com", ReceivedAt = new DateTime(2026, 1, 1) });
        await ctx.SaveChangesAsync();

        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var messages = Assert.IsAssignableFrom<IEnumerable<ContactMessage>>(view.Model).ToList();
        Assert.Equal("b@b.com", messages[0].Email);
    }

    [Fact]
    public async Task Detail_ReturnsView_WithMessage_WhenExists()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        ctx.ContactMessages.Add(new ContactMessage { Email = "x@x.com", IsRead = false });
        await ctx.SaveChangesAsync();
        var id = ctx.ContactMessages.First().Id;

        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        var result = await controller.Detail(id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ContactMessage>(view.Model);
    }

    [Fact]
    public async Task Detail_ReturnsNotFound_WhenMessageMissing()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Detail(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Detail_MarksUnreadMessage_AsRead()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "u@u.com", IsRead = false };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        await controller.Detail(msgId);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var msg = await verifyCtx.ContactMessages.FindAsync(msgId);
        Assert.True(msg!.IsRead);
    }

    [Fact]
    public async Task Detail_AlreadyReadMessage_DoesNotChangeDbState()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "r@r.com", IsRead = true };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));
        await controller.Detail(msgId);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var msg = await verifyCtx.ContactMessages.FindAsync(msgId);
        Assert.True(msg!.IsRead);
        Assert.Equal(1, verifyCtx.ContactMessages.Count());
    }

    [Fact]
    public async Task Delete_Post_RemovesMessage_AndRedirects()
    {
        var dbName = Guid.NewGuid().ToString();
        int msgId;
        using (var seedCtx = ControllerTestHelper.CreateInMemoryContext(dbName))
        {
            var m = new ContactMessage { Email = "d@d.com" };
            seedCtx.ContactMessages.Add(m);
            await seedCtx.SaveChangesAsync();
            msgId = m.Id;
        }

        using var ctx = ControllerTestHelper.CreateInMemoryContext(dbName);
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Delete(msgId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        using var verifyCtx = ControllerTestHelper.CreateInMemoryContext(dbName);
        Assert.Equal(0, verifyCtx.ContactMessages.Count());
    }

    [Fact]
    public async Task Delete_Post_NonExistentId_StillRedirects()
    {
        using var ctx = ControllerTestHelper.CreateInMemoryContext();
        var controller = ControllerTestHelper.Create(new ContactAdminController(ctx));

        var result = await controller.Delete(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~ContactAdminControllerTests" -v normal
```

Expected: `7 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Controllers/ContactAdminControllerTests.cs
git commit -m "test(admin): ContactAdminController unit tests (7 cases)"
```

---

### Task 8: PagesAdminController Unit Tests

**Files:**
- Create: `tests/Mitech.Tests/Controllers/PagesAdminControllerTests.cs`

- [ ] **Step 1: Create PagesAdminControllerTests.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Mitech.Tests.Helpers;
using Mitech.Web.Controllers.Admin;
using Mitech.Web.Models;
using Mitech.Web.Services;
using Moq;

namespace Mitech.Tests.Controllers;

public class PagesAdminControllerTests
{
    [Fact]
    public async Task Index_CallsGetAllAsync_AndReturnsView()
    {
        var contents = new List<PageContent>
        {
            new() { PageKey = "home.title", Lang = "ja", Value = "テスト" }
        };
        var serviceMock = new Mock<IPageContentService>();
        serviceMock.Setup(s => s.GetAllAsync())
            .ReturnsAsync(contents.AsReadOnly());

        var controller = ControllerTestHelper.Create(new PagesAdminController(serviceMock.Object));
        var result = await controller.Index();

        serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal(contents, view.Model);
    }

    [Fact]
    public async Task Update_Post_CallsSetAsync_WithCorrectArgs_AndRedirects()
    {
        var serviceMock = new Mock<IPageContentService>();
        serviceMock.Setup(s => s.SetAsync("home.title", "ja", "新タイトル"))
            .Returns(Task.CompletedTask);

        var controller = ControllerTestHelper.Create(new PagesAdminController(serviceMock.Object));
        var result = await controller.Update("home.title", "ja", "新タイトル");

        serviceMock.Verify(s => s.SetAsync("home.title", "ja", "新タイトル"), Times.Once);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }
}
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~PagesAdminControllerTests" -v normal
```

Expected: `2 passed, 0 failed`

- [ ] **Step 3: Run all unit tests to verify nothing broken**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~Mitech.Tests.Controllers|FullyQualifiedName~Mitech.Tests.Services" -v normal
```

Expected: `39 passed, 0 failed` (5 + 3 + 11 + 11 + 7 + 2 from controllers, 5 from PageContentServiceTests)

- [ ] **Step 4: Commit**

```bash
git add tests/Mitech.Tests/Controllers/PagesAdminControllerTests.cs
git commit -m "test(admin): PagesAdminController unit tests (2 cases)"
```

---

### Task 9: Integration — Admin Auth Redirect Tests

**Files:**
- Create: `tests/Mitech.Tests/Integration/AdminAuthTests.cs`

- [ ] **Step 1: Create AdminAuthTests.cs**

```csharp
using Mitech.Tests.Helpers;
using System.Net;

namespace Mitech.Tests.Integration;

public class AdminAuthTests : IClassFixture<AdminWebFactory>
{
    private readonly AdminWebFactory _factory;

    public AdminAuthTests(AdminWebFactory factory) => _factory = factory;

    private static IEnumerable<object[]> ProtectedRoutes =>
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
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~AdminAuthTests" -v normal
```

Expected: `8 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Integration/AdminAuthTests.cs
git commit -m "test(integration): admin auth redirect tests (8 cases)"
```

---

### Task 10: Integration — Login Flow Tests

**Files:**
- Create: `tests/Mitech.Tests/Integration/AdminLoginFlowTests.cs`

- [ ] **Step 1: Create AdminLoginFlowTests.cs**

```csharp
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
```

- [ ] **Step 2: Run tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~AdminLoginFlowTests" -v normal
```

Expected: `3 passed, 0 failed`

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Integration/AdminLoginFlowTests.cs
git commit -m "test(integration): admin login flow tests (3 cases)"
```

---

### Task 11: Integration — Admin CRUD Flow Tests

**Files:**
- Create: `tests/Mitech.Tests/Integration/AdminCrudFlowTests.cs`

- [ ] **Step 1: Create AdminCrudFlowTests.cs**

```csharp
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
        var token = AdminWebFactory.ExtractAntiForgeryToken(
            await getResp.Content.ReadAsStringAsync());

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

        var postResp = await client.PostAsync("/admin/products/create", form);

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
        var token = AdminWebFactory.ExtractAntiForgeryToken(
            await getResp.Content.ReadAsStringAsync());

        // SeedData seeds categories — get first one from the create form (category id=1 or first seeded)
        var html = await getResp.Content.ReadAsStringAsync();
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

        var postResp = await client.PostAsync("/admin/news/create", form);

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
```

- [ ] **Step 2: Run integration tests**

```
dotnet test tests/Mitech.Tests --filter "FullyQualifiedName~Mitech.Tests.Integration" -v normal
```

Expected: `19 passed, 0 failed` (8 auth + 3 login + 8 CRUD)

- [ ] **Step 3: Commit**

```bash
git add tests/Mitech.Tests/Integration/AdminCrudFlowTests.cs
git commit -m "test(integration): admin CRUD flow tests (8 cases)"
```

---

### Task 12: Final Verification — Run All Tests

- [ ] **Step 1: Run full test suite**

```
dotnet test tests/Mitech.Tests -v normal
```

Expected: All tests pass. Summary should include:
- 5 × LoginController
- 3 × Dashboard
- 11 × Products
- 11 × News
- 7 × Contact
- 2 × Pages
- 5 × PageContentService (existing)
- 8 × AdminAuth (integration)
- 3 × AdminLogin (integration)
- 8 × AdminCrud (integration)

**Total: 63 passed, 0 failed**

- [ ] **Step 2: Final commit**

```bash
git add -A
git commit -m "test(admin): complete admin test suite - 63 tests passing"
```
