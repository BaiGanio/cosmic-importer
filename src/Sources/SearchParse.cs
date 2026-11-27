using System.Text.Json;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Sources;

/// <summary>One hit of a search list. Values as the source writes them (the title is only read, never stored).</summary>
public sealed record SearchHit(string Id, string Title, IReadOnlyList<string> Keywords, string? ThumbUrl, string? PagePath)
{
    /// <summary>Every image link the list gives (NASA: thumb, small, medium, large, orig; the others: the list thumbnail).</summary>
    public IReadOnlyList<string> Links { get; init; } = [];
}

public sealed record SearchPage(int Total, IReadOnlyList<SearchHit> Hits);

/// <summary>X01 stage 3: the search answers of NASA (JSON) and of ESA/Hubble, ESA/Webb, ESO, NOIRLab (one site software, HTML).</summary>
public static partial class SearchParse
{
    public static SearchPage Nasa(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var c = doc.RootElement.GetProperty("collection");
        var total = c.GetProperty("metadata").GetProperty("total_hits").GetInt32();
        var hits = new List<SearchHit>();
        foreach (var item in c.GetProperty("items").EnumerateArray())
        {
            var data = item.GetProperty("data")[0];
            var keywords = data.TryGetProperty("keywords", out var k) && k.ValueKind == JsonValueKind.Array
                ? k.EnumerateArray().Select(x => x.GetString()).OfType<string>().ToList()
                : [];
            string? thumb = null;
            List<string> all = [];
            if (item.TryGetProperty("links", out var links))
            {
                thumb = links.EnumerateArray()
                    .Where(l => l.TryGetProperty("rel", out var r) && r.GetString() == "preview")
                    .Select(l => l.GetProperty("href").GetString())
                    .FirstOrDefault();
                all = links.EnumerateArray()
                    .Where(l => l.TryGetProperty("render", out var r) && r.GetString() == "image")
                    .Select(l => l.GetProperty("href").GetString()).OfType<string>().ToList();
            }
            hits.Add(new SearchHit(
                data.GetProperty("nasa_id").GetString()!,
                data.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                keywords, thumb, null) { Links = all });
        }
        return new SearchPage(total, hits);
    }

    [GeneratedRegex(@"\{\s*id: '((?:[^'\\]|\\.)*)',\s*title: '((?:[^'\\]|\\.)*)',[^{}]*?src: '((?:[^'\\]|\\.)*)',\s*url: '((?:[^'\\]|\\.)*)'")]
    private static partial Regex Image();

    [GeneratedRegex(@"Showing \d+ to \d+ of (\d+)")]
    private static partial Regex Showing();

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex Escape();

    public static SearchPage Djangoplicity(string html)
    {
        if (html.Contains("No images were found", StringComparison.Ordinal)) return new SearchPage(0, []);
        var hits = Image().Matches(html)
            .Select(m => new SearchHit(Js(m.Groups[1].Value), Js(m.Groups[2].Value), [], Js(m.Groups[3].Value), Js(m.Groups[4].Value)) { Links = [Js(m.Groups[3].Value)] })
            .ToList();
        var showing = Showing().Match(html);
        if (!showing.Success)
            throw new InvalidOperationException("Not a search list and not \"No images were found\"");
        return new SearchPage(int.Parse(showing.Groups[1].Value), hits);
    }

    private static string Js(string s) => Escape().Replace(s, "$1");
}
