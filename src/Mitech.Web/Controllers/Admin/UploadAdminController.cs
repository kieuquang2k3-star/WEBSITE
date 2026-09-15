using Microsoft.AspNetCore.Mvc;

namespace Mitech.Web.Controllers.Admin;

public class UploadAdminController : AdminBaseController
{
    private readonly IWebHostEnvironment _env;

    public UploadAdminController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost, IgnoreAntiforgeryToken]
    public async Task<IActionResult> Image(IFormFile upload)
    {
        if (upload is null || upload.Length == 0)
            return BadRequest(new { error = new { message = "Không có file." } });

        var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext))
            return BadRequest(new { error = new { message = "Chỉ chấp nhận jpg, png, gif, webp." } });

        var yearMonth = DateTime.Now.ToString("yyyy/MM");
        var folder = Path.Combine(_env.WebRootPath, "uploads", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{ext}";
        await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await upload.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/{yearMonth}/{fileName}" });
    }

    [HttpPost, IgnoreAntiforgeryToken]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Video(IFormFile upload)
    {
        if (upload is null || upload.Length == 0)
            return BadRequest(new { error = "Không có file." });

        var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
        if (!new[] { ".mp4", ".webm", ".mov", ".avi" }.Contains(ext))
            return BadRequest(new { error = "Chỉ chấp nhận mp4, webm, mov, avi." });

        var folder = Path.Combine(_env.WebRootPath, "uploads", "videos");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}{ext}";
        await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await upload.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/videos/{fileName}" });
    }
}
