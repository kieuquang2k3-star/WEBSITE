using System.ComponentModel.DataAnnotations;

namespace Mitech.Web.Models;

public class JobPosition
{
    public int    Id        { get; set; }
    [MaxLength(200)]
    public string Slug      { get; set; } = string.Empty;

    public string TitleVi      { get; set; } = string.Empty;
    public string TitleJa      { get; set; } = string.Empty;
    public string TitleEn      { get; set; } = string.Empty;

    public string ShortDescVi  { get; set; } = string.Empty;
    public string ShortDescJa  { get; set; } = string.Empty;
    public string ShortDescEn  { get; set; } = string.Empty;

    public string DetailVi     { get; set; } = string.Empty;
    public string DetailJa     { get; set; } = string.Empty;
    public string DetailEn     { get; set; } = string.Empty;

    public string SalaryVi     { get; set; } = string.Empty;
    public string SalaryJa     { get; set; } = string.Empty;
    public string SalaryEn     { get; set; } = string.Empty;

    public string VideoUrl     { get; set; } = string.Empty;

    public int    SortOrder    { get; set; }
    public bool   IsActive     { get; set; } = true;
}
