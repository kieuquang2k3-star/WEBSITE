namespace Mitech.Web.Services;

public interface ILanguageService
{
    string GetCurrent(HttpContext context);
    IReadOnlyList<(string Code, string Label, string NativeLabel)> SupportedLanguages { get; }
}
