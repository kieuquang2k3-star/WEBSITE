using Mitech.Web.Models;

namespace Mitech.Web.Extensions;

public static class LocalizationExtensions
{
    public static string GetTitle(this NewsArticle a, string lang) => lang switch
    {
        "en" => Fallback(a.TitleEn, a.TitleVi, a.TitleJa),
        "ja" => Fallback(a.TitleJa, a.TitleVi, a.TitleEn),
        _ => Fallback(a.TitleVi, a.TitleJa, a.TitleEn)
    };

    public static string GetBody(this NewsArticle a, string lang) => lang switch
    {
        "en" => Fallback(a.BodyEn, a.BodyVi, a.BodyJa),
        "ja" => Fallback(a.BodyJa, a.BodyVi, a.BodyEn),
        _ => Fallback(a.BodyVi, a.BodyJa, a.BodyEn)
    };

    public static string GetTitle(this Product p, string lang) => lang switch
    {
        "en" => Fallback(p.TitleEn, p.TitleVi, p.TitleJa),
        "ja" => Fallback(p.TitleJa, p.TitleVi, p.TitleEn),
        _ => Fallback(p.TitleVi, p.TitleJa, p.TitleEn)
    };

    public static string GetSpecName(this Product p, string lang) => lang switch
    {
        "en" => Fallback(p.SpecNameEn, p.SpecName),
        "vi" => Fallback(p.SpecNameVi, p.SpecName),
        _ => p.SpecName
    };

    public static string GetSpecMaterial(this Product p, string lang) => lang switch
    {
        "en" => Fallback(p.SpecMaterialEn, p.SpecMaterial),
        "vi" => Fallback(p.SpecMaterialVi, p.SpecMaterial),
        _ => p.SpecMaterial
    };

    public static string GetSpecDimension(this Product p, string lang) => lang switch
    {
        "en" => Fallback(p.SpecDimensionEn, p.SpecDimension),
        "vi" => Fallback(p.SpecDimensionVi, p.SpecDimension),
        _ => p.SpecDimension
    };

    public static string GetName(this NewsCategory c, string lang) => lang switch
    {
        "en" => Fallback(c.NameEn, c.NameVi, c.NameJa),
        "ja" => Fallback(c.NameJa, c.NameVi, c.NameEn),
        _ => Fallback(c.NameVi, c.NameJa, c.NameEn)
    };

    private static string Fallback(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
}
