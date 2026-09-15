using System.Text;
using Mitech.Web.Services;

namespace Mitech.Web.Infrastructure;

/// <summary>
/// Khi bật chế độ bảo trì, mọi truy cập công khai sẽ thấy trang thông báo (HTTP 503).
/// Chỉ admin đã đăng nhập (role "Admin") mới xem được site bình thường.
/// Khu vực /admin và /lang luôn được phép để admin có thể đăng nhập / đổi ngôn ngữ.
/// </summary>
public class MaintenanceMiddleware
{
    private const string EnabledKey = "site.maintenance.enabled";
    private const string MessageKey = "site.maintenance.message";

    private readonly RequestDelegate _next;

    // Cache cờ bật/tắt để không truy vấn DB mỗi request (TTL ngắn). Admin Save sẽ gọi Invalidate().
    private static volatile bool _cachedEnabled;
    private static DateTimeOffset _cachedAt = DateTimeOffset.MinValue;
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(10);
    private static readonly object Lock = new();

    public MaintenanceMiddleware(RequestDelegate next) => _next = next;

    public static void InvalidateCache()
    {
        lock (Lock) { _cachedAt = DateTimeOffset.MinValue; }
    }

    public async Task InvokeAsync(HttpContext context, IPageContentService content, ILanguageService lang)
    {
        var path = context.Request.Path.Value ?? "/";

        // Khu admin (gồm /admin/login) và chuyển ngôn ngữ luôn truy cập được.
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lang", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Admin đã đăng nhập → bỏ qua bảo trì.
        if (context.User?.IsInRole("Admin") == true)
        {
            await _next(context);
            return;
        }

        if (!await IsEnabledAsync(content))
        {
            await _next(context);
            return;
        }

        // Bị chặn → trả trang bảo trì 503.
        var code = lang.GetCurrent(context);
        var message = await content.GetAsync(MessageKey, code);
        if (string.IsNullOrWhiteSpace(message))
            message = DefaultMessage(code);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers["Retry-After"] = "3600";
        await context.Response.WriteAsync(BuildHtml(code, message), Encoding.UTF8);
    }

    private static async Task<bool> IsEnabledAsync(IPageContentService content)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _cachedAt < Ttl) return _cachedEnabled;

        var raw = await content.GetAsync(EnabledKey, "global");
        var enabled = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
        lock (Lock) { _cachedEnabled = enabled; _cachedAt = now; }
        return enabled;
    }

    private static string DefaultMessage(string code) => code switch
    {
        "ja" => "現在、ウェブサイトはメンテナンス中です。しばらくしてから再度アクセスしてください。",
        "en" => "The website is currently under maintenance. Please check back later.",
        _    => "Website đang được bảo trì. Vui lòng quay lại sau."
    };

    private static string BuildHtml(string code, string message)
    {
        var (title, loginLabel) = code switch
        {
            "ja" => ("メンテナンス中", "管理者ログイン"),
            "en" => ("Under maintenance", "Admin login"),
            _    => ("Đang bảo trì", "Đăng nhập quản trị")
        };
        var safeMsg = System.Net.WebUtility.HtmlEncode(message);

        return $$"""
<!DOCTYPE html>
<html lang="{{code}}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<meta name="robots" content="noindex">
<title>{{title}} — MICROTECHNO</title>
<link rel="icon" href="/favicon.png" sizes="32x32">
<style>
  *{box-sizing:border-box;margin:0;padding:0}
  body{min-height:100vh;display:flex;align-items:center;justify-content:center;
       font-family:-apple-system,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif;
       background:#0f1729;color:#e7ecf5;padding:24px;line-height:1.6}
  .box{max-width:560px;width:100%;text-align:center}
  .brand{font-weight:800;letter-spacing:.12em;color:#4d9bff;font-size:.95rem;margin-bottom:28px}
  .icon{font-size:54px;margin-bottom:18px}
  h1{font-size:1.7rem;font-weight:700;margin-bottom:14px}
  p.msg{font-size:1.05rem;color:#aebbd1;margin-bottom:30px;white-space:pre-line}
  .login{display:inline-block;padding:11px 26px;border-radius:8px;background:#2563eb;color:#fff;
         text-decoration:none;font-weight:600;font-size:.95rem;transition:background .15s}
  .login:hover{background:#1d4ed8}
  .langs{margin-top:34px;font-size:.85rem}
  .langs a{color:#7d8aa5;text-decoration:none;padding:0 8px}
  .langs a:hover{color:#fff}
  .langs a.cur{color:#fff;font-weight:700}
</style>
</head>
<body>
  <div class="box">
    <div class="brand">MICROTECHNO VIETNAM</div>
    <div class="icon">🛠️</div>
    <h1>{{title}}</h1>
    <p class="msg">{{safeMsg}}</p>
    <a class="login" href="/admin/login">{{loginLabel}}</a>
    <div class="langs">
      <a href="/lang/vi" class="{{(code == "vi" ? "cur" : "")}}">VI</a>·
      <a href="/lang/ja" class="{{(code == "ja" ? "cur" : "")}}">JA</a>·
      <a href="/lang/en" class="{{(code == "en" ? "cur" : "")}}">EN</a>
    </div>
  </div>
</body>
</html>
""";
    }
}
