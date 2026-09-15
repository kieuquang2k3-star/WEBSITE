namespace Mitech.Web.Services;

public class LanguageService : ILanguageService
{
    private static readonly (string Code, string Label, string NativeLabel)[] Supported =
    [
        ("vi", "Tiếng Việt", "VI"),
        ("ja", "日本語", "JA"),
        ("en", "English", "EN")
    ];

    public IReadOnlyList<(string Code, string Label, string NativeLabel)> SupportedLanguages => Supported;

    public string GetCurrent(HttpContext context)
    {
        var lang = context.Request.Cookies["lang"];
        if (lang != null && Supported.Any(s => s.Code == lang)) return lang;
        return "vi";
    }

    public static void SetCookie(HttpResponse response, string code)
    {
        response.Cookies.Append("lang", code, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });
    }
}
