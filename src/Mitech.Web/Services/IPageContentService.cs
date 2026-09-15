using Mitech.Web.Models;

namespace Mitech.Web.Services;

public interface IPageContentService
{
    Task<string> GetAsync(string key, string lang = "ja");
    Task SetAsync(string key, string lang, string value);
    Task<IReadOnlyList<PageContent>> GetAllAsync();
    Task<Dictionary<string, string>> GetPageAsync(string prefix, string lang);
}
