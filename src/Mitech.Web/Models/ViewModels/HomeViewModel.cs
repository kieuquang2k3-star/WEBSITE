namespace Mitech.Web.Models.ViewModels;

public class HomeViewModel
{
    public List<NewsArticle> LatestNews { get; set; } = [];
    public Dictionary<string, string> Content { get; set; } = [];

    public string Get(string key, string fallback = "") =>
        Content.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val) ? val : fallback;
}
