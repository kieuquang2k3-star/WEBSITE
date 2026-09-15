namespace Mitech.Web.Models;

public class PageContent
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string Lang { get; set; } = "ja";
    public string Value { get; set; } = string.Empty;
}
