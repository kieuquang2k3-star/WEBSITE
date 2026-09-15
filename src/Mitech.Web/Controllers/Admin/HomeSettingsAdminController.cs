using Microsoft.AspNetCore.Mvc;
using Mitech.Web.Services;

namespace Mitech.Web.Controllers.Admin;

public class HomeSettingsAdminController : AdminBaseController
{
    private const string G = "global";

    private readonly IPageContentService _content;
    private readonly IWebHostEnvironment _env;

    public HomeSettingsAdminController(IPageContentService content, IWebHostEnvironment env)
    {
        _content = content;
        _env = env;
    }

    private static readonly string[] TextLangs = ["vi", "ja", "en"];

    private static readonly string[] TextKeys =
    [
        "home.intro.main1", "home.intro.main2",
        "home.intro.sub1",  "home.intro.sub2", "home.intro.sub3",
        "home.strength.title",
        "home.strength1.line1", "home.strength1.line2", "home.strength1.line3",
        "home.strength2.line1", "home.strength2.line2", "home.strength2.line3",
        "home.strength3.line1", "home.strength3.line2", "home.strength3.line3",
        "home.banner.products", "home.banner.about",
        "home.banner.company",  "home.banner.locations",
        "home.news.heading",    "home.news.more"
    ];

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cài đặt trang web";

        ViewData["VideoDesktop"]  = await _content.GetAsync("home.video.desktop",           G);
        ViewData["VideoMobile"]   = await _content.GetAsync("home.video.mobile",            G);
        ViewData["RecruitVideo"]  = await _content.GetAsync("recruitment.benefits.video",   G);
        ViewData["TripVideo"]       = await _content.GetAsync("recruitment.trip.video",       G);
        ViewData["InsuranceVideo"]  = await _content.GetAsync("recruitment.insurance.video",  G);

        ViewData["BnrProducts"]  = await _content.GetAsync("home.banner.img.products",  G);
        ViewData["BnrAbout"]     = await _content.GetAsync("home.banner.img.about",     G);
        ViewData["BnrCompany"]   = await _content.GetAsync("home.banner.img.company",   G);
        ViewData["BnrLocations"] = await _content.GetAsync("home.banner.img.locations", G);

        ViewData["StrengthImg1"] = await _content.GetAsync("home.strength1.img", G);
        ViewData["StrengthImg2"] = await _content.GetAsync("home.strength2.img", G);
        ViewData["StrengthImg3"] = await _content.GetAsync("home.strength3.img", G);

        ViewData["HeaderLogoPath"]  = await _content.GetAsync("logo.header.path",  G);
        ViewData["HeaderLogoWidth"] = await _content.GetAsync("logo.header.width", G);
        ViewData["FooterLogoPath"]  = await _content.GetAsync("logo.footer.path",  G);
        ViewData["FooterLogoWidth"] = await _content.GetAsync("logo.footer.width", G);
        ViewData["MarkLogoPath"]    = await _content.GetAsync("logo.mark.path",    G);
        ViewData["MarkLogoWidth"]   = await _content.GetAsync("logo.mark.width",   G);
        ViewData["FaviconPath"]     = await _content.GetAsync("site.favicon.path", G);

        var textContent = new Dictionary<string, Dictionary<string, string>>();
        foreach (var lang in TextLangs)
        {
            textContent[lang] = await _content.GetPageAsync("home", lang);
        }
        ViewData["TextContent"] = textContent;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveText(string lang, IFormCollection form)
    {
        if (!TextLangs.Contains(lang))
            return BadRequest();

        foreach (var key in TextKeys)
        {
            if (form.TryGetValue(key, out var val))
                await _content.SetAsync(key, lang, val.ToString().Trim());
        }

        TempData["Success"] = $"Đã lưu nội dung ({lang.ToUpper()}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveRecruitVideo(string? videoUrl, IFormFile? videoFile)
    {
        if (videoFile is { Length: > 0 })
            videoUrl = await SaveFileAsync(videoFile, "video");
        if (!string.IsNullOrWhiteSpace(videoUrl))
            await _content.SetAsync("recruitment.benefits.video", G, videoUrl.Trim());
        TempData["Success"] = "Đã lưu video quyền lợi tuyển dụng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveTripVideo(string? videoUrl, IFormFile? videoFile)
    {
        if (videoFile is { Length: > 0 })
            videoUrl = await SaveFileAsync(videoFile, "video");
        if (!string.IsNullOrWhiteSpace(videoUrl))
            await _content.SetAsync("recruitment.trip.video", G, videoUrl.Trim());
        TempData["Success"] = "Đã lưu video du lịch công ty.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveInsuranceVideo(string? videoUrl, IFormFile? videoFile)
    {
        if (videoFile is { Length: > 0 })
            videoUrl = await SaveFileAsync(videoFile, "video");
        if (!string.IsNullOrWhiteSpace(videoUrl))
            await _content.SetAsync("recruitment.insurance.video", G, videoUrl.Trim());
        TempData["Success"] = "Đã lưu video bảo hiểm tai nạn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveVideo(
        string? desktopUrl, IFormFile? desktopFile,
        string? mobileUrl,  IFormFile? mobileFile)
    {
        if (desktopFile is { Length: > 0 })
            desktopUrl = await SaveFileAsync(desktopFile, "video");
        if (mobileFile is { Length: > 0 })
            mobileUrl = await SaveFileAsync(mobileFile, "video");

        if (!string.IsNullOrWhiteSpace(desktopUrl))
            await _content.SetAsync("home.video.desktop", G, desktopUrl.Trim());
        if (!string.IsNullOrWhiteSpace(mobileUrl))
            await _content.SetAsync("home.video.mobile", G, mobileUrl.Trim());

        TempData["Success"] = "Đã lưu cài đặt video.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveLogo(
        string? headerLogoUrl, IFormFile? headerLogoFile, string? headerLogoWidth,
        string? footerLogoUrl, IFormFile? footerLogoFile, string? footerLogoWidth,
        string? markLogoUrl,   IFormFile? markLogoFile,   string? markLogoWidth)
    {
        if (headerLogoFile is { Length: > 0 })
            headerLogoUrl = await SaveFileAsync(headerLogoFile, "logo");
        if (footerLogoFile is { Length: > 0 })
            footerLogoUrl = await SaveFileAsync(footerLogoFile, "logo");
        if (markLogoFile is { Length: > 0 })
            markLogoUrl = await SaveFileAsync(markLogoFile, "logo");

        if (!string.IsNullOrWhiteSpace(headerLogoUrl))  await _content.SetAsync("logo.header.path",  G, headerLogoUrl.Trim());
        if (!string.IsNullOrWhiteSpace(headerLogoWidth)) await _content.SetAsync("logo.header.width", G, headerLogoWidth.Trim());
        if (!string.IsNullOrWhiteSpace(footerLogoUrl))  await _content.SetAsync("logo.footer.path",  G, footerLogoUrl.Trim());
        if (!string.IsNullOrWhiteSpace(footerLogoWidth)) await _content.SetAsync("logo.footer.width", G, footerLogoWidth.Trim());
        if (!string.IsNullOrWhiteSpace(markLogoUrl))    await _content.SetAsync("logo.mark.path",    G, markLogoUrl.Trim());
        if (!string.IsNullOrWhiteSpace(markLogoWidth))   await _content.SetAsync("logo.mark.width",   G, markLogoWidth.Trim());

        TempData["Success"] = "Đã lưu cài đặt logo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveStrengthImages(
        string? img1Url, IFormFile? img1File,
        string? img2Url, IFormFile? img2File,
        string? img3Url, IFormFile? img3File)
    {
        if (img1File is { Length: > 0 }) img1Url = await SaveFileAsync(img1File, "strength");
        if (img2File is { Length: > 0 }) img2Url = await SaveFileAsync(img2File, "strength");
        if (img3File is { Length: > 0 }) img3Url = await SaveFileAsync(img3File, "strength");

        if (!string.IsNullOrWhiteSpace(img1Url)) await _content.SetAsync("home.strength1.img", G, img1Url.Trim());
        if (!string.IsNullOrWhiteSpace(img2Url)) await _content.SetAsync("home.strength2.img", G, img2Url.Trim());
        if (!string.IsNullOrWhiteSpace(img3Url)) await _content.SetAsync("home.strength3.img", G, img3Url.Trim());

        TempData["Success"] = "Đã lưu ảnh phần Thế mạnh.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> SaveBannerImages(
        string? productsUrl,  IFormFile? productsFile,
        string? aboutUrl,     IFormFile? aboutFile,
        string? companyUrl,   IFormFile? companyFile,
        string? locationsUrl, IFormFile? locationsFile)
    {
        if (productsFile  is { Length: > 0 }) productsUrl  = await SaveFileAsync(productsFile,  "banner");
        if (aboutFile     is { Length: > 0 }) aboutUrl     = await SaveFileAsync(aboutFile,     "banner");
        if (companyFile   is { Length: > 0 }) companyUrl   = await SaveFileAsync(companyFile,   "banner");
        if (locationsFile is { Length: > 0 }) locationsUrl = await SaveFileAsync(locationsFile, "banner");

        if (!string.IsNullOrWhiteSpace(productsUrl))  await _content.SetAsync("home.banner.img.products",  G, productsUrl.Trim());
        if (!string.IsNullOrWhiteSpace(aboutUrl))     await _content.SetAsync("home.banner.img.about",     G, aboutUrl.Trim());
        if (!string.IsNullOrWhiteSpace(companyUrl))   await _content.SetAsync("home.banner.img.company",   G, companyUrl.Trim());
        if (!string.IsNullOrWhiteSpace(locationsUrl)) await _content.SetAsync("home.banner.img.locations", G, locationsUrl.Trim());

        TempData["Success"] = "Đã lưu ảnh banner.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFavicon(string? faviconUrl, IFormFile? faviconFile)
    {
        if (faviconFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(faviconFile.FileName).ToLowerInvariant();
            if (ext is not (".ico" or ".png" or ".svg" or ".jpg" or ".jpeg" or ".webp"))
            {
                TempData["Error"] = "Định dạng file không hợp lệ. Chỉ chấp nhận .ico, .png, .svg, .jpg, .jpeg, .webp.";
                return RedirectToAction(nameof(Index));
            }
            faviconUrl = await SaveFileAsync(faviconFile, "favicon");
        }

        if (!string.IsNullOrWhiteSpace(faviconUrl))
            await _content.SetAsync("site.favicon.path", G, faviconUrl.Trim());

        TempData["Success"] = "Đã lưu favicon.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", subfolder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(dir, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{subfolder}/{fileName}";
    }
}
