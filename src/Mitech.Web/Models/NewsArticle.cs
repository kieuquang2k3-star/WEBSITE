using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Mitech.Web.Models;

public class NewsArticle
{
    public int Id { get; set; }
    public string TitleJa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string BodyJa { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public string BodyVi { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    [ValidateNever]
    public NewsCategory Category { get; set; } = null!;
    public DateTime PublishedAt { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
