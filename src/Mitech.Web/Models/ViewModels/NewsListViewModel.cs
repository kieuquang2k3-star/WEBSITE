namespace Mitech.Web.Models.ViewModels;

public class NewsListViewModel
{
    public List<NewsArticle> Articles { get; set; } = [];
    public NewsCategory? CurrentCategory { get; set; }
    public NewsSidebarViewModel Sidebar { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
