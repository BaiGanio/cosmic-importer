using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 stage 3: builds out/image-candidates.json and docs/images-report.md from the
/// cache only. Asks the web nothing (D45). Allowed hosts: docs/image-hosts.json (must match the brief and BGAPI Cosmic:ImageHosts).
/// Also writes out/other-objects.json (D59) and applies out/agent-results.json when it exists (D58).
/// Run: dotnet run --project src -- build-candidates [slug,slug,...]
/// </summary>
public static class BuildCandidates
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly Dictionary<string, string> Sites = new()
    {
        ["esahubble"] = "https://esahubble.org",
        ["esawebb"] = "https://esawebb.org",
        ["eso"] = "https://www.eso.org",
        ["noirlab"] = "https://noirlab.edu",
    };

    public sealed record ObjectCandidates(string Slug, string? Name, IReadOnlyList<ImageCandidate> Candidates, IReadOnlyList<DroppedImage> Dropped);
    public sealed record CandidatesFile(IReadOnlyList<ObjectCandidates> Objects);

    public static int Run(string root, string importer, string docs, string[] args)
    {
        var allowed = AllowedHosts(root);
        var objects = SearchMeasure.Load(importer, args.Length > 0 ? args[0].Split(',') : SearchMeasure.Sample);
        var downloads = Path.Combine(importer, "downloads");
        var result = new List<ObjectCandidates>();
        var refused = new List<DroppedImage>();
        var notOnDisk = new List<string>();
        var otherFinds = new List<OtherObjects.Find>();
        var agentFile = Path.Combine(importer, "out", "agent-results.json");
        var agentResults = File.Exists(agentFile)
            ? JsonSerializer.Deserialize<List<AgentResult>>(File.ReadAllText(agentFile), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
            : [];
        if (agentResults.Count > 0) Console.WriteLine($"Agent results applied: {agentResults.Count} ({agentResults.Count(r => !r.Keep)} drops)");

        foreach (var o in objects)
        {
            var terms = SearchForms.For(o).Select(f => f.Term).ToList();
            var list = new List<ImageCandidate>();
            var dropped = new List<DroppedImage>();
            foreach (var source in SearchMeasure.ImageSources)
            {
                var hits = new Dictionary<string, SearchHit>(StringComparer.Ordinal);
                var termOf = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var form in SearchForms.ForSource(o, source.Name))
                {
                    var key = Uri.EscapeDataString(form.Term);
                    for (var p = 1; p <= SearchMeasure.MaxPages; p++)
                    {
                        var file = Path.Combine(downloads, source.Name, "search", key, $"page-{p}.{source.Ext}");
                        if (!File.Exists(file)) { if (p == 1) notOnDisk.Add($"{source.Name} \"{form.Term}\""); break; }
                        var page = source.IsNasa ? SearchParse.Nasa(File.ReadAllText(file)) : SearchParse.Djangoplicity(File.ReadAllText(file));
                        foreach (var h in page.Hits) if (hits.TryAdd(h.Id, h)) termOf[h.Id] = form.Term;
                        if (page.Hits.Count == 0 || p * page.Hits.Count >= page.Total) break;
                    }
                }
                foreach (var hit in hits.Values)
                {
                    var id = Uri.EscapeDataString(hit.Id);
                    ImageCandidates.Result r;
                    if (source.IsNasa)
                    {
                        var f = Path.Combine(downloads, "nasa", "metadata", id + ".json");
                        r = ImageCandidates.FromNasa(o.Slug, hit, File.Exists(f) ? SourcePages.NasaMetadata(File.ReadAllText(f)) : null, terms, allowed);
                    }
                    else
                    {
                        var f = Path.Combine(downloads, source.Name, "pages", id + ".html");
                        var page = File.Exists(f) ? SourcePages.Djangoplicity(File.ReadAllText(f)) : null;
                        r = ImageCandidates.FromDjangoplicity(o.Slug, source.Name, Sites[source.Name], hit, page, terms, allowed);
                        if (page is not null && OtherObjects.IsOther(page.Names, terms))
                            otherFinds.Add(new OtherObjects.Find(source.Name, hit.Id, hit.Title, page.Names, page.Credit,
                                Sites[source.Name] + hit.PagePath, r.Candidate is not null ? "kept" : r.Dropped!.Reason, o.Slug, termOf[hit.Id]));
                    }
                    if (r.Candidate is not null) list.Add(r.Candidate);
                    else dropped.Add(r.Dropped!);
                }
            }
            var (kept, agentDropped) = AgentApply.Apply(Ranking.Order(list), agentResults);
            dropped.AddRange(agentDropped);
            refused.AddRange(dropped);
            result.Add(new ObjectCandidates(o.Slug, o.Name, kept, dropped));
        }

        var outFile = Path.Combine(importer, "out", "image-candidates.json");
        File.WriteAllText(outFile, JsonSerializer.Serialize(new CandidatesFile(result), Json) + "\n");
        var others = OtherObjects.Merge(otherFinds);
        var othersFile = Path.Combine(importer, "out", "other-objects.json");
        File.WriteAllText(othersFile, JsonSerializer.Serialize(new { note = "X01 D59: images whose page names only other objects. Names as the page writes them.", entries = others }, Json) + "\n");
        Console.WriteLine($"Other objects: {others.Count} images; written: {othersFile}");
        var report = Path.Combine(docs, "images-report.md");
        File.WriteAllText(report, Report(result, refused, notOnDisk));
        Console.WriteLine($"Candidates: {result.Sum(r => r.Candidates.Count)} for {result.Count} objects; refused: {refused.Count}");
        Console.WriteLine($"Written: {outFile}");
        Console.WriteLine($"Written: {report}");
        return 0;
    }

    internal static string[] AllowedHosts(string root)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "image-hosts.json")));
        return doc.RootElement.EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    private static string Report(List<ObjectCandidates> result, List<DroppedImage> refused, List<string> notOnDisk)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Cosmic: image candidates report (X01 step 9, stage 3)");
        sb.AppendLine();
        sb.AppendLine("Written by `dotnet run --project src -- build-candidates`. Do not edit by hand.");
        sb.AppendLine($"Run on {DateTime.UtcNow:yyyy-MM-dd}, from the cache only. Candidates: `out/image-candidates.json`.");
        sb.AppendLine("\"Named\": the title, the NASA keywords or the page's \"Name:\" field names the object (D46). Kept images are ranked (D57); dropped ones were thrown away by a rule (D51, D55, D56).");
        sb.AppendLine();
        sb.AppendLine("## Candidates per object");
        sb.AppendLine();
        sb.AppendLine("| Object | Name | Kept | Named | nasa | esahubble | esawebb | eso | noirlab | Dropped |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var o in result)
        {
            int N(string s) => o.Candidates.Count(c => c.Source == s);
            sb.AppendLine($"| {o.Slug} | {o.Name} | {o.Candidates.Count} | {o.Candidates.Count(c => c.Named)} | {N("nasa")} | {N("esahubble")} | {N("esawebb")} | {N("eso")} | {N("noirlab")} | {refused.Count(r => r.Slug == o.Slug)} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Objects with no candidate");
        sb.AppendLine();
        var none = result.Where(o => o.Candidates.Count == 0).ToList();
        sb.AppendLine(none.Count == 0 ? "None." : string.Join("\n", none.Select(o => $"- {o.Slug}")));
        sb.AppendLine();
        sb.AppendLine("## Objects with no named candidate");
        sb.AppendLine();
        var noNamed = result.Where(o => !o.Candidates.Any(c => c.Named)).ToList();
        sb.AppendLine(noNamed.Count == 0 ? "None." : string.Join("\n", noNamed.Select(o => $"- {o.Slug}")));
        sb.AppendLine();
        sb.AppendLine("## Dropped, by reason");
        sb.AppendLine();
        sb.AppendLine("| Reason | Count |");
        sb.AppendLine("|---|---|");
        foreach (var g in refused.GroupBy(r => r.By == "agent" ? "agent: " + r.Reason.Split(':')[0] : r.Reason).OrderByDescending(g => g.Count()))
            sb.AppendLine($"| {g.Key} | {g.Count()} |");
        sb.AppendLine();
        sb.AppendLine("## Every dropped image");
        sb.AppendLine();
        sb.AppendLine("| Object | Source | Id | Title | Dropped by: reason |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var r in refused)
            sb.AppendLine($"| {r.Slug} | {r.Source} | {r.SourceId} | {r.Title.Replace("|", "\\|")} | {r.By}: {r.Reason.Replace("|", "\\|")} |");
        if (notOnDisk.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Searches not on disk");
            sb.AppendLine();
            foreach (var s in notOnDisk) sb.AppendLine($"- {s}");
        }
        return sb.ToString();
    }
}
