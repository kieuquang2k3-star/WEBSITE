namespace Mitech.Web.Models;

public class Product
{
    public int Id { get; set; }
    public string TitleJa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleVi { get; set; } = string.Empty;
    public string SpecName { get; set; } = string.Empty;
    public string SpecNameEn { get; set; } = string.Empty;
    public string SpecNameVi { get; set; } = string.Empty;
    public string SpecMaterial { get; set; } = string.Empty;
    public string SpecMaterialEn { get; set; } = string.Empty;
    public string SpecMaterialVi { get; set; } = string.Empty;
    public string SpecDimension { get; set; } = string.Empty;
    public string SpecDimensionEn { get; set; } = string.Empty;
    public string SpecDimensionVi { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
