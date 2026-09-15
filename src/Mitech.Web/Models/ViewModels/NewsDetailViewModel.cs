namespace Mitech.Web.Models.ViewModels;

public class NewsDetailViewModel
{
    public NewsArticle Article { get; set; } = null!;
    public NewsSidebarViewModel Sidebar { get; set; } = new();
    public NewsArticle? PrevArticle { get; set; }
    public NewsArticle? NextArticle { get; set; }
}
