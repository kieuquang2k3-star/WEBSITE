using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mitech.Web.Data;
using Mitech.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == "Sqlite")
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
});

builder.Services.AddScoped<IPageContentService, PageContentService>();
builder.Services.AddSingleton<ILanguageService, Mitech.Web.Services.LanguageService>();

builder.Services.AddControllersWithViews()
    .AddRazorOptions(o => o.ViewLocationExpanders.Add(new Mitech.Web.Infrastructure.AdminViewLocationExpander()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Maintenance gate — chặn truy cập công khai khi bật bảo trì (admin đã đăng nhập được bỏ qua)
app.UseMiddleware<Mitech.Web.Infrastructure.MaintenanceMiddleware>();

// Language switch
app.MapControllerRoute("lang-switch", "lang/{code}", defaults: new { controller = "Lang", action = "Switch" });

// Static page routes (Vietnamese URLs)
app.MapControllerRoute("purchase",    "chinh-sach-mua-hang", defaults: new { controller = "StaticPages", action = "Purchase" });
app.MapControllerRoute("privacy",     "chinh-sach-bao-mat",  defaults: new { controller = "StaticPages", action = "Privacy" });
app.MapControllerRoute("security",    "bao-mat-thong-tin",   defaults: new { controller = "StaticPages", action = "Security" });
app.MapControllerRoute("tuyen-dung-detail", "tuyen-dung/{slug}", defaults: new { controller = "Recruitment", action = "Detail" });
app.MapControllerRoute("tuyen-dung", "tuyen-dung", defaults: new { controller = "StaticPages", action = "TuyenDung" });
app.MapControllerRoute("ve-chung-toi", "ve-chung-toi",   defaults: new { controller = "About",   action = "Index" });
app.MapControllerRoute("cong-ty-en",   "cong-ty/en",     defaults: new { controller = "Company", action = "En" });
app.MapControllerRoute("cong-ty-vi",   "cong-ty/vi",     defaults: new { controller = "Company", action = "Vi" });
app.MapControllerRoute("cong-ty",      "cong-ty",        defaults: new { controller = "Company", action = "Index" });
app.MapControllerRoute("san-pham",     "san-pham",       defaults: new { controller = "Products",  action = "Index" });
app.MapControllerRoute("dia-diem",     "dia-diem",       defaults: new { controller = "Locations", action = "Index" });
app.MapControllerRoute("lien-he",      "lien-he",        defaults: new { controller = "Contact",   action = "Index" });

// News routes (Vietnamese URLs)
app.MapControllerRoute("tin-tuc-danh-muc", "tin-tuc/danh-muc/{slug}", defaults: new { controller = "News", action = "Category" });
app.MapControllerRoute("tin-tuc-detail",   "tin-tuc/{id:int}",        defaults: new { controller = "News", action = "Detail" });
app.MapControllerRoute("tin-tuc",          "tin-tuc",                 defaults: new { controller = "News", action = "Index" });

// Admin area — named routes for friendly URL segments (maps /admin/products → ProductsAdmin, etc.)
app.MapControllerRoute("admin-products", "admin/products/{action=Index}/{id?}", defaults: new { controller = "ProductsAdmin" });
app.MapControllerRoute("admin-news", "admin/news/{action=Index}/{id?}", defaults: new { controller = "NewsAdmin" });
app.MapControllerRoute("admin-pages", "admin/pages/{action=Index}/{id?}", defaults: new { controller = "PagesAdmin" });
app.MapControllerRoute("admin-contact", "admin/contact/{action=Index}/{id?}", defaults: new { controller = "ContactAdmin" });
app.MapControllerRoute("admin-upload", "admin/upload/{action}", defaults: new { controller = "UploadAdmin" });
app.MapControllerRoute("admin-jobs", "admin/jobs/{action=Index}/{id?}", defaults: new { controller = "JobsAdmin" });
app.MapControllerRoute("admin-homesettings", "admin/homesettings/{action=Index}/{id?}", defaults: new { controller = "HomeSettingsAdmin" });
app.MapControllerRoute("admin-footer", "admin/footer/{action=Index}/{id?}", defaults: new { controller = "FooterAdmin" });
app.MapControllerRoute("admin-nav",    "admin/nav/{action=Index}/{id?}",    defaults: new { controller = "NavAdmin" });
app.MapControllerRoute("admin-maintenance", "admin/maintenance/{action=Index}/{id?}", defaults: new { controller = "MaintenanceAdmin" });

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}");

// Default
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

app.Run();

public partial class Program { }
