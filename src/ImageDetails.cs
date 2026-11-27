using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 stage 3: for every hit of every D48 search (D46: every hit, not only named ones), fetch the
/// details that hold credit and license: the image page (ESA/Hubble, ESA/Webb, ESO, NOIRLab) or
/// metadata.json (NASA). Cached in downloads/&lt;source&gt;/pages/ and downloads/nasa/metadata/ (D45, D49).
/// Fetch only: nothing is parsed here except the search lists.
/// Run: dotnet run --project src -- fetch-details [slug,slug,...]
/// </summary>
public static class ImageDetails
{
    private static readonly Dictionary<string, string> Hosts = new()
    {
        ["esahubble"] = "https://esahubble.org",
        ["esawebb"] = "https://esawebb.org",
        ["eso"] = "https://www.eso.org",
        ["noirlab"] = "https://noirlab.edu",
    };

    public static async Task<int> RunAsync(string importer, string[] args)
    {
        var objects = SearchMeasure.Load(importer, args.Length > 0 ? args[0].Split(',') : SearchMeasure.Sample);
        using var fetcher = new CachedFetcher(Path.Combine(importer, "downloads"));
        foreach (var source in SearchMeasure.ImageSources)
        {
            var hits = new Dictionary<string, SearchHit>(StringComparer.Ordinal);
            foreach (var o in objects)
            {
                var forms = SearchForms.ForSource(o, source.Name);
                foreach (var form in forms)
                    foreach (var hit in await HitsAsync(fetcher, source, form))
                        hits.TryAdd(hit.Id, hit);
            }
            Console.WriteLine($"{source.Name}: {hits.Count} different hits");
            var n = 0;
            foreach (var hit in hits.Values)
            {
                n++;
                if (n % 25 == 0) Console.WriteLine($"{source.Name}: {n} of {hits.Count}");
                var id = Uri.EscapeDataString(hit.Id);
                if (source.IsNasa)
                    await fetcher.GetAsync("nasa", Path.Combine("metadata", id + ".json"),
                        $"https://images-assets.nasa.gov/image/{id}/metadata.json");
                else if (hit.PagePath is { } pagePath && pagePath.StartsWith('/'))
                    await fetcher.GetAsync(source.Name, Path.Combine("pages", id + ".html"), Hosts[source.Name] + pagePath);
                else
                    fetcher.Failures.Add($"{source.Name} {hit.Id}: no page path in the search list");
            }
        }
        Console.WriteLine($"Asked the web: {fetcher.Asked}; from cache: {fetcher.FromCache}; failures: {fetcher.Failures.Count}");
        foreach (var f in fetcher.Failures) Console.WriteLine($"  {f}");
        return fetcher.Failures.Count == 0 ? 0 : 1;
    }

    /// <summary>The hits of one search, from the cache (or the web, if a page is missing), at most MaxPages pages.</summary>
    private static async Task<List<SearchHit>> HitsAsync(CachedFetcher fetcher, SearchMeasure.ImageSource source, SearchForm form)
    {
        var key = Uri.EscapeDataString(form.Term);
        var hits = new List<SearchHit>();
        for (var p = 1; p <= SearchMeasure.MaxPages; p++)
        {
            var url = source.IsNasa
                ? $"{source.Base}?q={key}&media_type=image" + (p > 1 ? $"&page={p}" : "")
                : p == 1 ? $"{source.Base}?search={key}" : $"{source.Base}page/{p}/?search={key}";
            var body = await fetcher.GetAsync(source.Name, Path.Combine("search", key, $"page-{p}.{source.Ext}"), url);
            if (body is null) break;
            var page = source.IsNasa ? SearchParse.Nasa(body) : SearchParse.Djangoplicity(body);
            hits.AddRange(page.Hits);
            if (page.Hits.Count == 0 || hits.Count >= page.Total) break;
        }
        return hits;
    }
}
