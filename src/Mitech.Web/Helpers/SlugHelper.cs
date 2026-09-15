using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mitech.Web.Helpers;

public static class SlugHelper
{
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "vi-tri-" + Guid.NewGuid().ToString()[..8];

        var s = input.ToLowerInvariant()
            .Replace("đ", "d").Replace("ð", "d");

        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = NonAlphanumeric
            .Replace(sb.ToString().Normalize(NormalizationForm.FormC), "-")
            .Trim('-');

        return string.IsNullOrEmpty(result)
            ? "vi-tri-" + Guid.NewGuid().ToString()[..8]
            : result;
    }
}
