namespace Mitech.Web.Models;

public class NewsCategory
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameJa { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameVi { get; set; } = string.Empty;
    public ICollection<NewsArticle> Articles { get; set; } = [];
}
