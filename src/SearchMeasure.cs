using System.Text;
using System.Text.Json;
using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 stage 3, measurement for D48: which search forms work on which source. For each sample object,
/// each form and each source: total hits, hits read (at most MaxPages pages), hits whose list text names
/// the object (D46: NASA title + keywords; ESA/ESO/NOIRLab list title only, the image page is not read
/// yet), and named hits that no other form of the object found.
/// Run: dotnet run --project src -- measure-search [slug,slug,...]
/// </summary>
public static class SearchMeasure
{
    public const int MaxPages = 5;

    internal static readonly string[] Sample =
        ["messier-31", "messier-42", "messier-13", "messier-45", "messier-102", "messier-40"];

    internal sealed record ImageSource(string Name, bool IsNasa, string Base, string Ext);

    internal static readonly ImageSource[] ImageSources =
    [
        new("nasa", true, "https://images-api.nasa.gov/search", "json"),
        new("esahubble", false, "https://esahubble.org/images/", "html"),
        new("esawebb", false, "https://esawebb.org/images/", "html"),
        new("eso", false, "https://www.eso.org/public/images/", "html"),
        new("noirlab", false, "https://noirlab.edu/public/images/", "html"),
    ];

    internal sealed record Row(string Source, string Slug, SearchForm Form, int Total, int Read, HashSet<string> Named, int Pages);

    public static async Task<int> RunAsync(string importer, string docs, string[] args)
    {
        var slugs = args.Length > 0 ? args[0].Split(',') : Sample;
        var all = JsonSerializer.Deserialize<ObjectsFile>(
            File.ReadAllText(Path.Combine(importer, "out", "messier.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Objects;
        var objects = slugs.Select(s => all.SingleOrDefault(o => o.Slug == s)
            ?? throw new InvalidOperationException($"Not in messier.json: {s}")).ToList();

        using var fetcher = new CachedFetcher(Path.Combine(importer, "downloads"));
        var rows = new List<Row>();
        foreach (var o in objects)
        {
            var forms = SearchForms.For(o);
            var terms = forms.Select(f => f.Term).ToList();
            foreach (var source in ImageSources)
                foreach (var form in forms)
                {
                    Console.WriteLine($"{source.Name} {o.Slug} \"{form.Term}\"");
                    rows.Add(await SearchAsync(fetcher, source, o.Slug, form, terms));
                }
        }

        var file = Path.Combine(docs, "search-measure.md");
        File.WriteAllText(file, Build(rows, objects.Select(o => o.Slug).ToList(), fetcher));
        Console.WriteLine($"Asked the web: {fetcher.Asked}; from cache: {fetcher.FromCache}; failures: {fetcher.Failures.Count}");
        Console.WriteLine($"Written: {file}");
        return fetcher.Failures.Count == 0 ? 0 : 1;
    }

    internal static async Task<Row> SearchAsync(CachedFetcher fetcher, ImageSource source, string slug, SearchForm form, List<string> terms)
    {
        var key = Uri.EscapeDataString(form.Term);
        int total = 0, read = 0, pages = 0;
        var named = new HashSet<string>(StringComparer.Ordinal);
        for (var p = 1; p <= MaxPages; p++)
        {
            var url = source.IsNasa
                ? $"{source.Base}?q={key}&media_type=image" + (p > 1 ? $"&page={p}" : "")
                : p == 1 ? $"{source.Base}?search={key}" : $"{source.Base}page/{p}/?search={key}";
            var body = await fetcher.GetAsync(source.Name, Path.Combine("search", key, $"page-{p}.{source.Ext}"), url);
            if (body is null) break;
            var page = source.IsNasa ? SearchParse.Nasa(body) : SearchParse.Djangoplicity(body);
            pages = p;
            total = page.Total;
            read += page.Hits.Count;
            foreach (var hit in page.Hits)
                if (Pairing.Names(hit.Title, terms) || hit.Keywords.Any(k => Pairing.Names(k, terms)))
                    named.Add(hit.Id);
            if (page.Hits.Count == 0 || read >= total) break;
        }
        return new Row(source.Name, slug, form, total, read, named, pages);
    }

    private static string Build(List<Row> rows, List<string> slugs, CachedFetcher fetcher)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Cosmic: search forms measured (X01 step 9, stage 3, for D48)");
        sb.AppendLine();
        sb.AppendLine("Written by `dotnet run --project src -- measure-search`. Do not edit by hand.");
        sb.AppendLine($"Run on {DateTime.UtcNow:yyyy-MM-dd}. Objects: {string.Join(", ", slugs)}. At most {MaxPages} pages per search.");
        sb.AppendLine("\"Named\": the list text names the object (D46). NASA: title and keywords. ESA/ESO/NOIRLab: list title only, the image page is not read yet.");
        sb.AppendLine("\"Only this form\": named hits that no other form of the same object found on that source.");
        sb.AppendLine();
        sb.AppendLine("## Summary per source and form kind");
        sb.AppendLine();
        sb.AppendLine("| Source | Form kind | Searches | Total hits | Read | Named | Only this form |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var g in rows.GroupBy(r => (r.Source, r.Form.Kind)))
            sb.AppendLine($"| {g.Key.Source} | {g.Key.Kind} | {g.Count()} | {g.Sum(r => r.Total)} | {g.Sum(r => r.Read)} | {g.Sum(r => r.Named.Count)} | {g.Sum(r => OnlyThis(r, rows))} |");
        sb.AppendLine();
        sb.AppendLine("## Named images per source and object (all forms together)");
        sb.AppendLine();
        sb.AppendLine("| Source | " + string.Join(" | ", slugs) + " |");
        sb.AppendLine("|---|" + string.Concat(slugs.Select(_ => "---|")));
        foreach (var s in rows.Select(r => r.Source).Distinct())
            sb.AppendLine($"| {s} | " + string.Join(" | ", slugs.Select(slug =>
                rows.Where(r => r.Source == s && r.Slug == slug).SelectMany(r => r.Named).Distinct().Count())) + " |");
        sb.AppendLine();
        sb.AppendLine("## Every search");
        sb.AppendLine();
        sb.AppendLine("| Source | Object | Term | Kind | Total hits | Read | Named | Only this form |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in rows)
            sb.AppendLine($"| {r.Source} | {r.Slug} | {r.Form.Term} | {r.Form.Kind} | {r.Total} | {r.Read} | {r.Named.Count} | {OnlyThis(r, rows)} |");
        sb.AppendLine();
        sb.AppendLine($"Asked the web: {fetcher.Asked}. From cache: {fetcher.FromCache}.");
        if (fetcher.Failures.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Failures");
            sb.AppendLine();
            foreach (var f in fetcher.Failures) sb.AppendLine($"- {f}");
        }
        return sb.ToString();
    }

    private static int OnlyThis(Row r, List<Row> rows) =>
        r.Named.Count(id => !rows.Any(o => o != r && o.Source == r.Source && o.Slug == r.Slug && o.Named.Contains(id)));

    internal sealed record ObjectsFile(List<CosmicObject> Objects);

    internal static List<CosmicObject> Load(string importer, IEnumerable<string> slugs)
    {
        var all = JsonSerializer.Deserialize<ObjectsFile>(
            File.ReadAllText(Path.Combine(importer, "out", "messier.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!.Objects;
        return slugs.Select(s => all.SingleOrDefault(o => o.Slug == s)
            ?? throw new InvalidOperationException($"Not in messier.json: {s}")).ToList();
    }
}
