namespace Mitech.Web.Models.ViewModels;

public class NewsSidebarViewModel
{
    public List<NewsArticle> LatestArticles { get; set; } = [];
    public List<NewsCategory> Categories { get; set; } = [];
    public List<(int Year, int Month, int Count)> ArchiveMonths { get; set; } = [];
}
