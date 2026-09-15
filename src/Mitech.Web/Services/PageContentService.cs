using Microsoft.EntityFrameworkCore;
using Mitech.Web.Data;
using Mitech.Web.Models;

namespace Mitech.Web.Services;

public class PageContentService : IPageContentService
{
    private readonly ApplicationDbContext _db;

    public PageContentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetAsync(string key, string lang = "ja")
    {
        var entry = await _db.PageContents
            .FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == lang);

        if (entry is not null)
            return entry.Value;

        if (lang != "ja")
        {
            var fallback = await _db.PageContents
                .FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == "ja");
            if (fallback is not null)
                return fallback.Value;
        }

        return string.Empty;
    }

    public async Task SetAsync(string key, string lang, string value)
    {
        var entry = await _db.PageContents
            .FirstOrDefaultAsync(p => p.PageKey == key && p.Lang == lang);

        if (entry is null)
            _db.PageContents.Add(new PageContent { PageKey = key, Lang = lang, Value = value });
        else
            entry.Value = value;

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PageContent>> GetAllAsync()
    {
        return await _db.PageContents
            .OrderBy(p => p.PageKey)
            .ThenBy(p => p.Lang)
            .ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetPageAsync(string prefix, string lang)
    {
        var prefixDot = prefix + ".";

        var all = await _db.PageContents
            .Where(p => p.PageKey.StartsWith(prefixDot) &&
                        (p.Lang == "ja" || p.Lang == "global" || p.Lang == lang))
            .ToListAsync();

        var result = new Dictionary<string, string>();

        // Priority: ja (lowest) < global < lang-specific (highest)
        foreach (var e in all.Where(e => e.Lang == "ja"))     result[e.PageKey] = e.Value;
        foreach (var e in all.Where(e => e.Lang == "global")) result[e.PageKey] = e.Value;
        if (lang != "ja" && lang != "global")
            foreach (var e in all.Where(e => e.Lang == lang)) result[e.PageKey] = e.Value;

        return result;
    }
}
