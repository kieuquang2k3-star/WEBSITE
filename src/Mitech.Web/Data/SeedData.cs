using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mitech.Web.Models;

namespace Mitech.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        // For SQLite dev: EnsureCreated avoids WAL-mode migration lock issues
        // For SQL Server prod: MigrateAsync applies pending migrations
        var providerName = db.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase)
            || providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        await SeedAdminAsync(services, config);
        await SeedNewsCategoriesAsync(db);
        await SeedProductsAsync(db);
        await EnsureTransmissionCatalogAsync(db);
        await SeedNewsArticlesAsync(db);
        await EnsureEventCategoryRemovedAsync(db);
        await SeedHomeContentAsync(db);
        await EnsureStrengthContentAsync(db);
        await SeedSiteSettingsAsync(db);
        await SeedFooterContentAsync(db);
        await SeedNavContentAsync(db);
        await EnsureNavEntriesAsync(db);
        await EnsureFooterBusinessAsync(db);
        await EnsureFooterTelDashesAsync(db);
        await SeedJobPositionsAsync(db);
        await EnsureVisualInspectionJobAsync(db);
        await SeedRecruitContentAsync(db);
    }

    private static async Task SeedAdminAsync(IServiceProvider services, IConfiguration config)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var email = config["AdminSeed:Email"] ?? "admin@mitec.co.jp";
        var password = config["AdminSeed:Password"] ?? "Admin@123456";

        if (await userManager.FindByEmailAsync(email) is null)
        {
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

    private static async Task SeedNewsCategoriesAsync(ApplicationDbContext db)
    {
        if (await db.NewsCategories.AnyAsync()) return;

        db.NewsCategories.AddRange(
            new NewsCategory { Slug = "information", NameJa = "お知らせ", NameEn = "Information", NameVi = "Thông báo" },
            new NewsCategory { Slug = "technology", NameJa = "技術更新", NameEn = "Technology", NameVi = "Công nghệ" }
        );
        await db.SaveChangesAsync();
    }

    // Nội dung 3 thẻ "Thế mạnh" trên trang chủ. lineN: 1 = tiêu đề, 2-3 = đoạn mô tả.
    private const string StrengthContentVersion = "2";

    private static readonly (string key, string lang, string value)[] StrengthContent =
    [
        ("home.strength1.line1", "ja", "お客様から高く評価される「信頼のモノづくり」"),
        ("home.strength1.line2", "ja", "品質・コスト・納期（QCD）の総合力が高く評価され、数々の顧客表彰を受賞。"),
        ("home.strength1.line3", "ja", "「安心して任せられるサプライヤー」として、お客様との強固な信頼関係を築いています。"),
        ("home.strength1.line1", "vi", "Năng lực sản xuất đáng tin cậy được khách hàng đánh giá cao"),
        ("home.strength1.line2", "vi", "Năng lực tổng thể về Chất lượng – Chi phí – Giao hàng (QCD) được khách hàng đánh giá cao và đã nhận được nhiều giải thưởng, bằng khen từ khách hàng."),
        ("home.strength1.line3", "vi", "Với vai trò là nhà cung cấp đáng tin cậy, chúng tôi xây dựng và duy trì mối quan hệ hợp tác bền vững, vững chắc với khách hàng."),
        ("home.strength1.line1", "en", "Trusted manufacturing, highly valued by our customers"),
        ("home.strength1.line2", "en", "Our overall strength in Quality, Cost and Delivery (QCD) is highly regarded, earning numerous customer awards and commendations."),
        ("home.strength1.line3", "en", "As a supplier our customers can entrust with confidence, we build and sustain strong, lasting partnerships."),

        ("home.strength2.line1", "ja", "小径精密部品における圧倒的な加工技術"),
        ("home.strength2.line2", "ja", "難削材や複雑形状、高精度・高難度加工にも対応できる高度な加工技術を保有。"),
        ("home.strength2.line3", "ja", "小径精密部品に求められる厳しい公差や品質要求にも、豊富な実績とノウハウで柔軟に対応します。"),
        ("home.strength2.line1", "vi", "Công nghệ gia công vượt trội đối với các chi tiết chính xác đường kính nhỏ"),
        ("home.strength2.line2", "vi", "Sở hữu công nghệ gia công tiên tiến, có khả năng đáp ứng các yêu cầu gia công vật liệu khó cắt, hình dạng phức tạp cũng như các chi tiết có độ chính xác và độ khó cao."),
        ("home.strength2.line3", "vi", "Với kinh nghiệm thực tiễn phong phú cùng bí quyết kỹ thuật tích lũy, chúng tôi đáp ứng linh hoạt các yêu cầu khắt khe về dung sai và chất lượng của các chi tiết chính xác đường kính nhỏ."),
        ("home.strength2.line1", "en", "Outstanding machining technology for small-diameter precision parts"),
        ("home.strength2.line2", "en", "We hold advanced machining technology capable of handling difficult-to-cut materials, complex geometries, and work demanding high precision and high difficulty."),
        ("home.strength2.line3", "en", "Backed by extensive experience and accumulated know-how, we flexibly meet the stringent tolerance and quality requirements of small-diameter precision parts."),

        ("home.strength3.line1", "ja", "一貫生産体制が実現する高品質・短納期・低コスト"),
        ("home.strength3.line2", "ja", "材料調達から切削加工、表面処理、組立・検査までの一貫生産体制により、品質のばらつきを最小化。"),
        ("home.strength3.line3", "ja", "工程間ロスや納期遅延、コスト増加のリスクを低減し、高付加価値製品を安定して供給します。"),
        ("home.strength3.line1", "vi", "Hệ thống sản xuất khép kín mang lại chất lượng cao – giao hàng nhanh – chi phí tối ưu"),
        ("home.strength3.line2", "vi", "Nhờ hệ thống sản xuất đồng bộ từ khâu mua nguyên vật liệu, gia công cắt gọt, xử lý bề mặt đến lắp ráp và kiểm tra, chúng tôi giảm thiểu tối đa sự biến động về chất lượng."),
        ("home.strength3.line3", "vi", "Đồng thời, giảm thiểu tổn thất giữa các công đoạn, hạn chế rủi ro chậm tiến độ và gia tăng chi phí, qua đó cung cấp ổn định các sản phẩm có giá trị gia tăng cao."),
        ("home.strength3.line1", "en", "Integrated production delivering high quality, short lead times and optimized cost"),
        ("home.strength3.line2", "en", "Our fully integrated production — from material procurement through cutting, surface treatment, assembly and inspection — minimizes variation in quality."),
        ("home.strength3.line3", "en", "It also reduces inter-process losses and the risk of delivery delays and cost increases, enabling a stable supply of high value-added products."),
    ];

    // Ghi nội dung "Thế mạnh" phiên bản hiện tại vào DB một lần duy nhất.
    // Cờ home.strength.contentVersion đảm bảo không ghi đè chỉnh sửa sau này của admin.
    private static async Task EnsureStrengthContentAsync(ApplicationDbContext db)
    {
        const string markerKey = "home.strength.contentVersion";

        var marker = await db.PageContents.FirstOrDefaultAsync(p => p.PageKey == markerKey && p.Lang == "global");
        if (marker?.Value == StrengthContentVersion) return;

        foreach (var (key, lang, value) in StrengthContent)
        {
            var existing = await db.PageContents.FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == lang);
            if (existing is null)
                db.PageContents.Add(new PageContent { PageKey = key, Lang = lang, Value = value });
            else
                existing.Value = value;
        }

        if (marker is null)
            db.PageContents.Add(new PageContent { PageKey = markerKey, Lang = "global", Value = StrengthContentVersion });
        else
            marker.Value = StrengthContentVersion;

        await db.SaveChangesAsync();
    }

    // Removes the legacy "event" (展示会出展) category from existing databases.
    // Idempotent: no-op once removed. Reassigns any articles still in it to "information".
    private static async Task EnsureEventCategoryRemovedAsync(ApplicationDbContext db)
    {
        var eventCat = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "event");
        if (eventCat is null) return;

        var fallback = await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug == "information")
                       ?? await db.NewsCategories.FirstOrDefaultAsync(c => c.Slug != "event");
        if (fallback is not null)
        {
            var orphaned = await db.NewsArticles.Where(a => a.CategoryId == eventCat.Id).ToListAsync();
            foreach (var a in orphaned) a.CategoryId = fallback.Id;
        }

        db.NewsCategories.Remove(eventCat);
        await db.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        db.Products.AddRange(
            new Product { TitleJa = "ブロック部品", TitleEn = "Block Parts", TitleVi = "Linh kiện khối",
                SpecName = "ボディ", SpecNameEn = "Body", SpecNameVi = "Thân",
                SpecMaterial = "アルミ6000系", SpecMaterialEn = "Aluminum 6000 series", SpecMaterialVi = "Nhôm hợp kim 6000",
                SpecDimension = "全長70mm", SpecDimensionEn = "Total length 70mm", SpecDimensionVi = "Chiều dài 70mm",
                ImagePath = "/uploads/2026/04/img_product_02-2.jpg", SortOrder = 1 },
            new Product { TitleJa = "燃焼電池部品", TitleEn = "Fuel Cell Parts", TitleVi = "Linh kiện pin nhiên liệu",
                SpecName = "ボディ", SpecNameEn = "Body", SpecNameVi = "Thân",
                SpecMaterial = "電磁SUS", SpecMaterialEn = "Electromagnetic SUS", SpecMaterialVi = "Thép không gỉ điện từ",
                SpecDimension = "外径φ32", SpecDimensionEn = "Outer dia. φ32", SpecDimensionVi = "Đường kính ngoài φ32",
                ImagePath = "/uploads/2026/04/img_product_03-1.jpg", SortOrder = 2 },
            new Product { TitleJa = "トランスミッション部品", TitleEn = "Transmission Parts", TitleVi = "Linh kiện hộp số",
                SpecName = "バルブASSY（圧入）", SpecNameEn = "Valve ASSY (press-fit)", SpecNameVi = "Cụm van (ép nguội)",
                SpecMaterial = "アルミ6000系", SpecMaterialEn = "Aluminum 6000 series", SpecMaterialVi = "Nhôm hợp kim 6000",
                SpecDimension = "外径φ13", SpecDimensionEn = "Outer dia. φ13", SpecDimensionVi = "Đường kính ngoài φ13",
                ImagePath = "/uploads/2026/04/img_product_04-1.jpg", SortOrder = 3 },
            new Product { TitleJa = "トランスミッション部品", TitleEn = "Transmission Parts", TitleVi = "Linh kiện hộp số",
                SpecName = "シャフト", SpecNameEn = "Shaft", SpecNameVi = "Trục",
                SpecMaterial = "鉄材", SpecMaterialEn = "Iron", SpecMaterialVi = "Vật liệu sắt",
                SpecDimension = "外径φ18", SpecDimensionEn = "Outer dia. φ18", SpecDimensionVi = "Đường kính ngoài φ18",
                ImagePath = "/uploads/2026/04/img_product_05.jpg", SortOrder = 4 },
            new Product { TitleJa = "トランスミッション部品", TitleEn = "Transmission Parts", TitleVi = "Linh kiện hộp số",
                SpecName = "シャフト", SpecNameEn = "Shaft", SpecNameVi = "Trục",
                SpecMaterial = "スチール", SpecMaterialEn = "Steel", SpecMaterialVi = "Thép",
                SpecDimension = "外径φ20", SpecDimensionEn = "Outer dia. φ20", SpecDimensionVi = "Đường kính ngoài φ20",
                ImagePath = "/uploads/2026/04/img_product_06.jpg", SortOrder = 5 },
            new Product { TitleJa = "AT部品", TitleEn = "AT Parts", TitleVi = "Linh kiện AT",
                SpecName = "バルブ", SpecNameEn = "Valve", SpecNameVi = "Van",
                SpecMaterial = "アルミ6000系", SpecMaterialEn = "Aluminum 6000 series", SpecMaterialVi = "Nhôm hợp kim 6000",
                SpecDimension = "外径φ14", SpecDimensionEn = "Outer dia. φ14", SpecDimensionVi = "Đường kính ngoài φ14",
                ImagePath = "/uploads/2026/04/img_product_07.jpg", SortOrder = 6 },
            new Product { TitleJa = "エンジン部品", TitleEn = "Engine Parts", TitleVi = "Linh kiện động cơ",
                SpecName = "アペックスシール", SpecNameEn = "Apex Seal", SpecNameVi = "Con dấu đỉnh",
                SpecMaterial = "特殊鉄材", SpecMaterialEn = "Special iron", SpecMaterialVi = "Thép đặc biệt",
                SpecDimension = "全長85mm", SpecDimensionEn = "Total length 85mm", SpecDimensionVi = "Chiều dài 85mm",
                ImagePath = "/uploads/2026/04/img_product_10-1.jpg", SortOrder = 7 },
            new Product { TitleJa = "精密部品", TitleEn = "Precision Parts", TitleVi = "Linh kiện chính xác",
                SpecName = "ピン", SpecNameEn = "Pin", SpecNameVi = "Chốt",
                SpecMaterial = "SUS304", SpecMaterialEn = "SUS304", SpecMaterialVi = "SUS304",
                SpecDimension = "外径φ4", SpecDimensionEn = "Outer dia. φ4", SpecDimensionVi = "Đường kính ngoài φ4",
                ImagePath = "/uploads/2026/04/img_product_12.jpg", SortOrder = 8 }
        );
        await db.SaveChangesAsync();
    }

    // Thay toàn bộ danh mục sản phẩm bằng bộ 15 linh kiện hộp số (トランスミッション部品)
    // trong "製品紹介.pdf" do khách hàng cung cấp. Chạy một lần duy nhất (đánh dấu bằng
    // site.products.catalogVersion) — không ghi đè nếu admin đã chỉnh sửa danh mục sau đó,
    // vì marker chỉ được set khi migration này tự thực hiện.
    private const string TransmissionCatalogVersion = "1";

    private static async Task EnsureTransmissionCatalogAsync(ApplicationDbContext db)
    {
        const string markerKey = "site.products.catalogVersion";

        var marker = await db.PageContents.FirstOrDefaultAsync(p => p.PageKey == markerKey && p.Lang == "global");
        if (marker?.Value == TransmissionCatalogVersion) return;

        const string ja = "トランスミッション部品";
        const string en = "Transmission Parts";
        const string vi = "Linh kiện hộp số";

        var items = new (string NameJa, string NameEn, string NameVi,
                          string MatJa, string MatEn, string MatVi,
                          string Dim, string DimEn, string DimVi,
                          string Img)[]
        {
            ("プレッシャー・レギュレータ バルブ", "Pressure Regulator Valve", "Van điều áp",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ14", "Outer dia. φ14", "Đường kính ngoài φ14", "product-transmission-01.jpg"),
            ("ソレノイド レギュレーシング バルブ", "Solenoid Regulating Valve", "Van điều tiết điện từ",
             "鋼材", "Steel", "Thép",
             "外径φ10", "Outer dia. φ10", "Đường kính ngoài φ10", "product-transmission-02.jpg"),
            ("プラグ", "Plug", "Nút bịt",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ5", "Outer dia. φ5", "Đường kính ngoài φ5", "product-transmission-03.jpg"),
            ("ストッパープラグ", "Stopper Plug", "Nút bịt chặn",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ10", "Outer dia. φ10", "Đường kính ngoài φ10", "product-transmission-04.jpg"),
            ("ストッパープラグ", "Stopper Plug", "Nút bịt chặn",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ10", "Outer dia. φ10", "Đường kính ngoài φ10", "product-transmission-05.jpg"),
            ("プラグ", "Plug", "Nút bịt",
             "アルミ2000系", "Aluminum 2000 series", "Nhôm hợp kim 2000",
             "外径φ5", "Outer dia. φ5", "Đường kính ngoài φ5", "product-transmission-06.jpg"),
            ("プラグ", "Plug", "Nút bịt",
             "アルミ2000系", "Aluminum 2000 series", "Nhôm hợp kim 2000",
             "外径φ5", "Outer dia. φ5", "Đường kính ngoài φ5", "product-transmission-07.jpg"),
            ("アキュームレータ ピストン", "Accumulator Piston", "Piston tích áp",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ23", "Outer dia. φ23", "Đường kính ngoài φ23", "product-transmission-08.jpg"),
            ("スプールバルブ・パイプ・カット", "Spool Valve (Pipe-cut type)", "Van con trượt (dạng ống, cắt)",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ12", "Outer dia. φ12", "Đường kính ngoài φ12", "product-transmission-09.jpg"),
            ("プールバルブ・カット", "Spool Valve (Cut type)", "Van con trượt (dạng cắt)",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ12", "Outer dia. φ12", "Đường kính ngoài φ12", "product-transmission-10.jpg"),
            ("シャフト・バルブ：オイルポンプ", "Shaft Valve: Oil Pump", "Van trục: Bơm dầu",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ10", "Outer dia. φ10", "Đường kính ngoài φ10", "product-transmission-11.jpg"),
            ("アキュームレータ　ピストン", "Accumulator Piston", "Piston tích áp",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ16", "Outer dia. φ16", "Đường kính ngoài φ16", "product-transmission-12.jpg"),
            ("アキュームレータ　ピストン", "Accumulator Piston", "Piston tích áp",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ16", "Outer dia. φ16", "Đường kính ngoài φ16", "product-transmission-13.jpg"),
            ("アキュームレータ　ピストン", "Accumulator Piston", "Piston tích áp",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ16", "Outer dia. φ16", "Đường kính ngoài φ16", "product-transmission-14.jpg"),
            ("シフトバルブ：ロータリバース", "Shift Valve: Rotary Reverse", "Van chuyển số: Đảo chiều quay",
             "アルミ6000系", "Aluminum 6000 series", "Nhôm hợp kim 6000",
             "外径φ12", "Outer dia. φ12", "Đường kính ngoài φ12", "product-transmission-15.jpg"),
        };

        db.Products.RemoveRange(db.Products);

        var sort = 1;
        foreach (var it in items)
        {
            db.Products.Add(new Product
            {
                TitleJa = ja, TitleEn = en, TitleVi = vi,
                SpecName = it.NameJa, SpecNameEn = it.NameEn, SpecNameVi = it.NameVi,
                SpecMaterial = it.MatJa, SpecMaterialEn = it.MatEn, SpecMaterialVi = it.MatVi,
                SpecDimension = it.Dim, SpecDimensionEn = it.DimEn, SpecDimensionVi = it.DimVi,
                ImagePath = $"/uploads/2026/08/{it.Img}",
                SortOrder = sort++,
                IsActive = true
            });
        }

        if (marker is null)
            db.PageContents.Add(new PageContent { PageKey = markerKey, Lang = "global", Value = TransmissionCatalogVersion });
        else
            marker.Value = TransmissionCatalogVersion;

        await db.SaveChangesAsync();
    }

    private static async Task SeedHomeContentAsync(ApplicationDbContext db)
    {
        if (await db.PageContents.AnyAsync(p => p.PageKey.StartsWith("home."))) return;

        var entries = new (string key, string lang, string value)[]
        {
            ("home.intro.main1", "ja", "「精密」への"),
            ("home.intro.main1", "vi", "Không ngừng"),
            ("home.intro.main1", "en", "Relentless Challenge"),
            ("home.intro.main2", "ja", "あくなき挑戦"),
            ("home.intro.main2", "vi", "thách thức sự chính xác"),
            ("home.intro.main2", "en", "for \"Precision\""),
            ("home.intro.sub1",  "ja", "人と技術によるハイクオリティーの追求。"),
            ("home.intro.sub1",  "vi", "Theo đuổi chất lượng cao nhờ con người và công nghệ."),
            ("home.intro.sub1",  "en", "Pursuing high quality through people and technology."),
            ("home.intro.sub2",  "ja", "マイクロテクノは、確かな精度と高度な技術で"),
            ("home.intro.sub2",  "vi", "Micro Techno mang đến chất lượng và sự tin cậy"),
            ("home.intro.sub2",  "en", "Micro Techno delivers quality and peace of mind"),
            ("home.intro.sub3",  "ja", "品質と安心を届けます。"),
            ("home.intro.sub3",  "vi", "với độ chính xác đáng tin và kỹ thuật tiên tiến."),
            ("home.intro.sub3",  "en", "with reliable precision and advanced technology."),
            ("home.strength.title",   "ja", "私たちの強み"),
            ("home.strength.title",   "vi", "Thế mạnh của chúng tôi"),
            ("home.strength.title",   "en", "Our Strengths"),
            // home.strengthN.lineN do EnsureStrengthContentAsync quản lý (xem StrengthContent).
            ("home.banner.products",  "ja", "製品情報"),
            ("home.banner.products",  "vi", "Sản phẩm"),
            ("home.banner.products",  "en", "Products"),
            ("home.banner.about",     "ja", "マイクロテクノベトナムについて"),
            ("home.banner.about",     "vi", "Về Micro Techno"),
            ("home.banner.about",     "en", "About Us"),
            ("home.banner.company",   "ja", "会社概要"),
            ("home.banner.company",   "vi", "Giới thiệu công ty"),
            ("home.banner.company",   "en", "Company"),
            ("home.banner.locations", "ja", "拠点情報"),
            ("home.banner.locations", "vi", "Thông tin địa điểm"),
            ("home.banner.locations", "en", "Locations"),
            ("home.news.heading",     "ja", "新着情報"),
            ("home.news.heading",     "vi", "Tin tức"),
            ("home.news.heading",     "en", "News"),
            ("home.news.more",        "ja", "新着情報の一覧を見る"),
            ("home.news.more",        "vi", "Xem tất cả tin tức"),
            ("home.news.more",        "en", "View all news"),
        };

        db.PageContents.AddRange(entries.Select(e =>
            new PageContent { PageKey = e.key, Lang = e.lang, Value = e.value }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedSiteSettingsAsync(ApplicationDbContext db)
    {
        if (!await db.PageContents.AnyAsync(p => p.PageKey == "site.favicon.path"))
        {
            db.PageContents.Add(new PageContent
            {
                PageKey = "site.favicon.path",
                Lang = "global",
                Value = "/favicon.png"
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedNavContentAsync(ApplicationDbContext db)
    {
        if (await db.PageContents.AnyAsync(p => p.PageKey.StartsWith("nav."))) return;

        var entries = new (string key, string lang, string value)[]
        {
            ("nav.top",       "vi", "Trang chủ"),
            ("nav.top",       "ja", "トップページ"),
            ("nav.top",       "en", "Top"),
            ("nav.products",  "vi", "Sản phẩm"),
            ("nav.products",  "ja", "製品情報"),
            ("nav.products",  "en", "Products"),
            ("nav.about",     "vi", "Về chúng tôi"),
            ("nav.about",     "ja", "マイクロテクノについて"),
            ("nav.about",     "en", "About us"),
            ("nav.company",   "vi", "Giới thiệu"),
            ("nav.company",   "ja", "会社概要"),
            ("nav.company",   "en", "Company"),
            ("nav.locations", "vi", "Địa điểm"),
            ("nav.locations", "ja", "拠点情報"),
            ("nav.locations", "en", "Locations"),
            ("nav.news",      "vi", "Tin tức"),
            ("nav.news",      "ja", "新着情報"),
            ("nav.news",      "en", "News"),
            ("nav.contact",   "vi", "Liên hệ"),
            ("nav.contact",   "ja", "お問い合わせ"),
            ("nav.contact",   "en", "Contact"),
            ("nav.recruit",   "vi", "Tuyển dụng"),
            ("nav.recruit",   "ja", "採用情報"),
            ("nav.recruit",   "en", "Recruitment"),
        };

        db.PageContents.AddRange(entries.Select(e =>
            new PageContent { PageKey = e.key, Lang = e.lang, Value = e.value }));
        await db.SaveChangesAsync();
    }

    private static async Task EnsureNavEntriesAsync(ApplicationDbContext db)
    {
        var required = new (string key, string lang, string value)[]
        {
            ("nav.recruit", "ja", "採用情報"),
            ("nav.recruit", "en", "Recruitment"),
        };
        bool changed = false;
        foreach (var (key, lang, value) in required)
        {
            if (!await db.PageContents.AnyAsync(p => p.PageKey == key && p.Lang == lang))
            {
                db.PageContents.Add(new PageContent { PageKey = key, Lang = lang, Value = value });
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
    }

    private static async Task SeedFooterContentAsync(ApplicationDbContext db)
    {
        if (await db.PageContents.AnyAsync(p => p.PageKey.StartsWith("footer."))) return;

        var entries = new (string key, string lang, string value)[]
        {
            // Global
            ("footer.company.tel",   "global", "022-1397-4544"),
            ("footer.hq.address",    "global", "〒739-2124 広島県東広島市高屋町郷676番地"),
            ("footer.hq.tel",        "global", "082-434-1155"),
            ("footer.hq.fax",        "global", "082-434-1164"),
            ("footer.instagram.url", "global", "https://www.instagram.com/mitec_recruit"),

            // Tiếng Việt
            ("footer.company.name",    "vi", "CÔNG TY TNHH MICROTECHNO VIỆT NAM"),
            ("footer.company.address", "vi", "Lô đất số J3,4 (RF-12), Khu công nghiệp Thăng Long II,\nPhường Đường Hào, Tỉnh Hưng Yên, Việt Nam"),
            ("footer.hq.name",         "vi", "CÔNG TY CỔ PHẦN MICROTECHNO"),
            ("footer.business.1",      "vi", "Sản xuất, gia công phụ tùng và bộ phận phụ trợ cho xe ô tô."),
            ("footer.business.2",      "vi", "Van điều tiết dầu, van tăng áp dầu, nắp chặn và cuộn lõi."),
            ("footer.business.3",      "vi", "Linh kiện điều khiển thủy lực AT/CVT"),
            ("footer.business.4",      "vi", "Linh kiện đường ống nhiên liệu"),
            ("footer.copyright",       "vi", "Gia công cơ khí chính xác linh kiện ô tô — MICROTECHNO Co., Ltd."),

            // 日本語
            ("footer.company.name",    "ja", "マイクロテクノベトナム有限会社"),
            ("footer.company.address", "ja", "タンロンII工業団地 J3,4区画 (RF-12)\nハオ通り, フンイェン省, ベトナム"),
            ("footer.hq.name",         "ja", "マイクロテクノ株式会社"),
            ("footer.business.1",      "ja", "自動車部品および補助部品の製造・加工"),
            ("footer.business.2",      "ja", "オイルコントロールバルブ、加圧バルブ、ストッパーキャップ、コイルコア"),
            ("footer.business.3",      "ja", "AT/CVT油圧制御部品"),
            ("footer.business.4",      "ja", "フューエルレール部品等の製造"),
            ("footer.copyright",       "ja", "自動車部品の精密切削・研削技術ならマイクロテクノ株式会社"),

            // English
            ("footer.company.name",    "en", "MICROTECHNO VIETNAM CO.,LTD"),
            ("footer.company.address", "en", "Plot J3,4 (RF-12), Thang Long Industrial Park II,\nDuong Hao Ward, Hung Yen Province, Vietnam"),
            ("footer.hq.name",         "en", "Microtechno Co., Ltd"),
            ("footer.business.1",      "en", "Manufacturing and machining of automotive parts and auxiliary components."),
            ("footer.business.2",      "en", "Oil control valves, oil pressure boost valves, stopper caps and coil cores."),
            ("footer.business.3",      "en", "AT/CVT hydraulic control parts"),
            ("footer.business.4",      "en", "Fuel rail parts manufacturing"),
            ("footer.copyright",       "en", "Precision machining for automotive parts — MICROTECHNO Co., Ltd."),
        };

        db.PageContents.AddRange(entries.Select(e =>
            new PageContent { PageKey = e.key, Lang = e.lang, Value = e.value }));
        await db.SaveChangesAsync();
    }

    private static async Task EnsureFooterBusinessAsync(ApplicationDbContext db)
    {
        var correct = new (string key, string lang, string value)[]
        {
            ("footer.business.1", "vi", "Sản xuất, gia công phụ tùng và bộ phận phụ trợ cho xe ô tô."),
            ("footer.business.2", "vi", "Van điều tiết dầu, van tăng áp dầu, nắp chặn và cuộn lõi."),
            ("footer.business.3", "vi", "Linh kiện điều khiển thủy lực AT/CVT"),
            ("footer.business.4", "vi", "Linh kiện đường ống nhiên liệu"),
            ("footer.business.1", "ja", "自動車部品および補助部品の製造・加工"),
            ("footer.business.2", "ja", "オイルコントロールバルブ、加圧バルブ、ストッパーキャップ、コイルコア"),
            ("footer.business.3", "ja", "AT/CVT油圧制御部品"),
            ("footer.business.4", "ja", "フューエルレール部品等の製造"),
            ("footer.business.1", "en", "Manufacturing and machining of automotive parts and auxiliary components."),
            ("footer.business.2", "en", "Oil control valves, oil pressure boost valves, stopper caps and coil cores."),
            ("footer.business.3", "en", "AT/CVT hydraulic control parts"),
            ("footer.business.4", "en", "Fuel rail parts manufacturing"),
        };

        bool changed = false;
        foreach (var (key, lang, value) in correct)
        {
            var existing = await db.PageContents.FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == lang);
            if (existing is null)
            {
                db.PageContents.Add(new PageContent { PageKey = key, Lang = lang, Value = value });
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
    }

    // Số TEL trong footer trước đây lưu không có dấu gạch ngang, khác định dạng của FAX
    // ("082-434-1164"). Chỉ sửa khi giá trị vẫn đúng bằng chuỗi cũ, để không ghi đè
    // chỉnh sửa khác mà admin đã lưu qua trang quản trị.
    private static async Task EnsureFooterTelDashesAsync(ApplicationDbContext db)
    {
        var fixes = new (string key, string oldValue, string newValue)[]
        {
            ("footer.company.tel", "02213974544", "022-1397-4544"),
            ("footer.hq.tel",      "0824341155",  "082-434-1155"),
        };

        bool changed = false;
        foreach (var (key, oldValue, newValue) in fixes)
        {
            var existing = await db.PageContents.FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == "global");
            if (existing is not null && existing.Value == oldValue)
            {
                existing.Value = newValue;
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync();
    }

    // Nội dung banner tuyển dụng, 3 ô thống kê lương và khối "Thời gian làm việc"
    // trên trang /tuyen-dung — chỉnh được qua admin (RecruitmentAdminController).
    private static async Task SeedRecruitContentAsync(ApplicationDbContext db)
    {
        if (await db.PageContents.AnyAsync(p => p.PageKey.StartsWith("recruit."))) return;

        var entries = new (string key, string lang, string value)[]
        {
            ("recruit.hero.tag",   "vi", "Công ty 100% vốn Nhật Bản &nbsp;·&nbsp; KCN Thăng Long II, Phường Dương Hào, Hưng Yên"),
            ("recruit.hero.tag",   "ja", "日系100%出資企業　·　タンロンII工業団地 J3,4区画（フンイェン省）"),
            ("recruit.hero.tag",   "en", "100% Japanese-owned company · Thang Long Industrial Park II, Hung Yen Province"),
            ("recruit.hero.title", "vi", "TUYỂN GẤP 100 NAM NỮ CÔNG NHÂN"),
            ("recruit.hero.title", "ja", "男女作業員　100名　急募"),
            ("recruit.hero.title", "en", "URGENTLY HIRING 100 WORKERS"),
            ("recruit.hero.desc",  "vi", "Sản xuất linh kiện ô tô &nbsp;·&nbsp; Mazda – Toyota – Honda"),
            ("recruit.hero.desc",  "ja", "自動車部品製造　·　Mazda – Toyota – Honda"),
            ("recruit.hero.desc",  "en", "Automotive parts manufacturing · Mazda – Toyota – Honda"),
            ("recruit.hero.income.label", "vi", "Thu nhập"),
            ("recruit.hero.income.label", "ja", "月収"),
            ("recruit.hero.income.label", "en", "Monthly income"),
            ("recruit.hero.income.value", "vi", "8 – 15 triệu VNĐ/tháng"),
            ("recruit.hero.income.value", "ja", "800万〜1,500万 VND/月"),
            ("recruit.hero.income.value", "en", "8 – 15 million VND/month"),
            ("recruit.hero.contact", "vi", "Liên hệ ngay: <strong>0221 397 4544</strong>&nbsp;&nbsp;|&nbsp;&nbsp;Phỏng vấn: Thứ 2–Thứ 6 &amp; Thứ 7 cách tuần · 8:30–16:00"),
            ("recruit.hero.contact", "ja", "お問い合わせ：<strong>0221 397 4544</strong><br>面接：月〜金・隔週土曜　8:30〜16:00"),
            ("recruit.hero.contact", "en", "Contact us: <strong>0221 397 4544</strong>&nbsp;&nbsp;|&nbsp;&nbsp;Interviews: Mon–Fri &amp; every other Sat · 8:30–16:00"),

            ("recruit.stat1.label", "vi", "Lương cơ bản"),
            ("recruit.stat1.label", "ja", "基本給"),
            ("recruit.stat1.label", "en", "Base salary"),
            ("recruit.stat1.value", "vi", "6.070.000 đ"),
            ("recruit.stat1.value", "ja", "6,070,000 VND"),
            ("recruit.stat1.value", "en", "6,070,000 VND"),
            ("recruit.stat2.label", "vi", "Phụ cấp cố định"),
            ("recruit.stat2.label", "ja", "固定手当"),
            ("recruit.stat2.label", "en", "Fixed allowances"),
            ("recruit.stat2.value", "vi", "1.200.000 đ"),
            ("recruit.stat2.value", "ja", "1,200,000 VND"),
            ("recruit.stat2.value", "en", "1,200,000 VND"),
            ("recruit.stat3.label", "vi", "Thưởng Tết 2025–2026"),
            ("recruit.stat3.label", "ja", "旧正月ボーナス 2025〜2026"),
            ("recruit.stat3.label", "en", "Lunar New Year bonus 2025–2026"),
            ("recruit.stat3.value", "vi", "2,2 tháng lương"),
            ("recruit.stat3.value", "ja", "給与2.2か月分"),
            ("recruit.stat3.value", "en", "2.2 months' salary"),

            ("recruit.schedule.heading", "vi", "Thời gian làm việc"),
            ("recruit.schedule.heading", "ja", "勤務時間"),
            ("recruit.schedule.heading", "en", "Working Hours"),
            ("recruit.schedule.note", "vi", "Trung bình 22,3 công/tháng &nbsp;·&nbsp; 8 tiếng/ngày &nbsp;·&nbsp; Nghỉ 4 Chủ nhật + 2 Thứ 7/tháng"),
            ("recruit.schedule.note", "ja", "平均 22.3日/月　·　8時間/日　·　休日：日曜4日＋土曜2日/月"),
            ("recruit.schedule.note", "en", "Average 22.3 days/month · 8 hours/day · Days off: 4 Sundays + 2 Saturdays/month"),
            ("recruit.schedule.admin.label", "vi", "Hành chính:"),
            ("recruit.schedule.admin.label", "ja", "行政（一般）："),
            ("recruit.schedule.admin.label", "en", "Office:"),
            ("recruit.schedule.admin.value", "vi", "08:15 – 17:00"),
            ("recruit.schedule.admin.value", "ja", "08:15〜17:00"),
            ("recruit.schedule.admin.value", "en", "08:15 – 17:00"),
            ("recruit.schedule.day.label", "vi", "Hành chính ngày:"),
            ("recruit.schedule.day.label", "ja", "日勤（行政）："),
            ("recruit.schedule.day.label", "en", "Day shift (office):"),
            ("recruit.schedule.day.value", "vi", "08:15 – 17:00"),
            ("recruit.schedule.day.value", "ja", "08:15〜17:00"),
            ("recruit.schedule.day.value", "en", "08:15 – 17:00"),
            ("recruit.schedule.night.label", "vi", "Hành chính đêm:"),
            ("recruit.schedule.night.label", "ja", "夜勤（行政）："),
            ("recruit.schedule.night.label", "en", "Night shift (office):"),
            ("recruit.schedule.night.value", "vi", "20:15 – 05:00"),
            ("recruit.schedule.night.value", "ja", "20:15〜05:00"),
            ("recruit.schedule.night.value", "en", "20:15 – 05:00"),
            ("recruit.schedule.prod.label", "vi", "Ca sản xuất:"),
            ("recruit.schedule.prod.label", "ja", "生産シフト："),
            ("recruit.schedule.prod.label", "en", "Production shifts:"),
            ("recruit.schedule.shift1", "vi", "Ca 1: 06:00 – 14:00"),
            ("recruit.schedule.shift1", "ja", "シフト1：06:00〜14:00"),
            ("recruit.schedule.shift1", "en", "Shift 1: 06:00 – 14:00"),
            ("recruit.schedule.shift2", "vi", "Ca 2: 14:00 – 22:00"),
            ("recruit.schedule.shift2", "ja", "シフト2：14:00〜22:00"),
            ("recruit.schedule.shift2", "en", "Shift 2: 14:00 – 22:00"),
            ("recruit.schedule.shift3", "vi", "Ca 3: 22:00 – 06:00"),
            ("recruit.schedule.shift3", "ja", "シフト3：22:00〜06:00"),
            ("recruit.schedule.shift3", "en", "Shift 3: 22:00 – 06:00"),
            ("recruit.schedule.fixed.label", "vi", "Ca cố định:"),
            ("recruit.schedule.fixed.label", "ja", "固定シフト："),
            ("recruit.schedule.fixed.label", "en", "Fixed shift:"),
            ("recruit.schedule.fixed.value", "vi", "06:00 – 14:40"),
            ("recruit.schedule.fixed.value", "ja", "06:00〜14:40"),
            ("recruit.schedule.fixed.value", "en", "06:00 – 14:40"),
            ("recruit.schedule.fixed.note", "vi", "(Không đổi ca theo nguyện vọng)"),
            ("recruit.schedule.fixed.note", "ja", "（希望によりシフト変更なし）"),
            ("recruit.schedule.fixed.note", "en", "(No rotation upon request)"),
        };

        db.PageContents.AddRange(entries.Select(e =>
            new PageContent { PageKey = e.key, Lang = e.lang, Value = e.value }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedNewsArticlesAsync(ApplicationDbContext db)
    {
        if (await db.NewsArticles.AnyAsync()) return;

        var infoCategory = await db.NewsCategories.FirstAsync(c => c.Slug == "information");

        db.NewsArticles.Add(new NewsArticle
        {
            TitleJa = "ホームページをリニューアルしました",
            TitleEn = "Website Renewal",
            TitleVi = "Cập nhật trang web",
            BodyJa = "<p>このたび、マイクロテクノ株式会社のホームページをリニューアルいたしました。</p><p>今後ともよろしくお願いいたします。</p>",
            BodyEn = "<p>We have renewed our company website.</p><p>Thank you for your continued support.</p>",
            BodyVi = "<p>Chúng tôi đã cập nhật trang web công ty.</p>",
            CategoryId = infoCategory.Id,
            PublishedAt = new DateTime(2026, 4, 27),
            IsPublished = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedJobPositionsAsync(ApplicationDbContext db)
    {
        if (await db.JobPositions.AnyAsync()) return;

        db.JobPositions.AddRange(
            new JobPosition
            {
                Slug       = "van-hanh-may-cnc",
                TitleVi    = "Công nhân vận hành máy CNC",
                TitleJa    = "CNCオペレーター",
                TitleEn    = "CNC Machine Operator",
                ShortDescVi = "Vận hành máy bán tự động (tiện, mài...). Không làm việc theo dây chuyền.",
                ShortDescJa = "半自動機械（旋盤・研削など）を操作します。ライン作業はありません。",
                ShortDescEn = "Operates semi-automatic machines (turning, grinding, etc.). No assembly-line work.",
                DetailVi   = "<p>Vận hành máy bán tự động bao gồm tiện, mài và các loại máy CNC khác. Không làm việc theo dây chuyền sản xuất. Được đào tạo từ đầu, không yêu cầu kinh nghiệm.</p>",
                DetailJa   = "<p>半自動機械（旋盤・研削など）を操作します。ライン作業はありません。未経験でも研修から始められます。</p>",
                DetailEn   = "<p>Operates semi-automatic machines including lathes, grinders, and other CNC equipment. No assembly-line work required. Training provided from scratch.</p>",
                SalaryVi   = "8 – 15 triệu VNĐ/tháng",
                SalaryJa   = "月収 8〜15百万 VND",
                SalaryEn   = "8 – 15 million VND/month",
                SortOrder  = 1,
                IsActive   = true
            },
            new JobPosition
            {
                Slug       = "kiem-tra-ngoai-quan-vis",
                TitleVi    = "Công nhân kiểm tra ngoại quan (VIS)",
                TitleJa    = "外観検査員（VIS）",
                TitleEn    = "Visual Inspection Operator (VIS)",
                ShortDescVi = "Kiểm tra sản phẩm nhỏ trong phòng điều hòa. Công việc nhẹ nhàng, sạch sẽ.",
                ShortDescJa = "空調完備の室内にて小型部品を目視検査。軽作業・クリーンな環境です。",
                ShortDescEn = "Inspects small components in an air-conditioned room. Light, clean work environment.",
                DetailVi   = "<p>Kiểm tra ngoại quan sản phẩm nhỏ bằng mắt thường trong phòng có điều hòa. Công việc nhẹ nhàng, sạch sẽ, phù hợp cả nam và nữ. Không yêu cầu kinh nghiệm.</p>",
                DetailJa   = "<p>空調完備の室内にて小型部品を目視検査。軽作業・クリーンな環境で、男女問わず活躍できます。未経験でもOKです。</p>",
                DetailEn   = "<p>Visually inspects small components in an air-conditioned room. Light, clean work environment suitable for both men and women. No experience required.</p>",
                SalaryVi   = "8 – 15 triệu VNĐ/tháng",
                SalaryJa   = "月収 8〜15百万 VND",
                SalaryEn   = "8 – 15 million VND/month",
                SortOrder  = 2,
                IsActive   = true
            }
        );
        await db.SaveChangesAsync();
    }

    // Thêm vị trí tuyển dụng "Công đoạn ngoại quan" (VISUAL) — bổ sung ô còn trống
    // trong lưới 6 vị trí trên trang /tuyen-dung. Idempotent theo Slug.
    private static async Task EnsureVisualInspectionJobAsync(ApplicationDbContext db)
    {
        const string slug = "cong-doan-ngoai-quan";
        if (await db.JobPositions.AnyAsync(j => j.Slug == slug)) return;

        var nextOrder = await db.JobPositions.AnyAsync()
            ? await db.JobPositions.MaxAsync(j => j.SortOrder) + 1
            : 1;

        db.JobPositions.Add(new JobPosition
        {
            Slug        = slug,
            TitleVi     = "Công đoạn ngoại quan",
            TitleJa     = "Visual（外観検査）",
            TitleEn     = "Visual inspection process",
            ShortDescVi = "Sử dụng kính phóng đại kiểm tra tình trạng bên ngoài của sản phẩm",
            ShortDescJa = "拡大鏡を使用して製品の外観状態を検査する。",
            ShortDescEn = "Uses a magnifying glass to inspect the external condition of products.",
            DetailVi    = "<figure class=\"table\"><table><tbody><tr><td>Sử dụng kính phóng đại kiểm tra tình trạng bên ngoài của sản phẩm</td></tr><tr><td>Sử dụng thiết bị kiểm tra chuyên dụng để kiểm tra thông số sản phẩm</td></tr><tr><td>Vệ sinh khu vực làm việc hàng ngày</td></tr></tbody></table></figure>",
            DetailJa    = "<figure class=\"table\"><table><tbody><tr><td>拡大鏡を使用して製品の外観状態を検査する。</td></tr><tr><td>専用検査機器を使用して製品の寸法・規格を確認する。</td></tr><tr><td>作業エリアの日常清掃を行う。</td></tr></tbody></table></figure>",
            DetailEn    = "<figure class=\"table\"><table><tbody><tr><td>Uses a magnifying glass to inspect the external condition of products.</td></tr><tr><td>Uses specialized inspection equipment to check product dimensions and specifications.</td></tr><tr><td>Performs daily cleaning of the work area.</td></tr></tbody></table></figure>",
            SortOrder   = nextOrder,
            IsActive    = true
        });
        await db.SaveChangesAsync();
    }
}
