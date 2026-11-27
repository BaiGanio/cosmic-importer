using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 D57 step 3, first test: 10 images of one object (the 5 best kept, 5 dropped with different reasons)
/// go to local Ollama models with their text. The image (its url size) is cached in downloads/&lt;source&gt;/images/,
/// each answer in out/model/&lt;model&gt;/&lt;source&gt;/&lt;id&gt;.json with the prompt's SHA-256, so a second run asks
/// nothing again. Ollama runs on this machine only (http://localhost:11434). No tests (D24).
/// Run: dotnet run --project src -- model-test [hard | slug] [model,model]
/// </summary>
public static class ModelTest
{
    private sealed record Item(string Source, string Id, string Title, string? Type, string? Credit, string? Url, bool RulesKeep, string Rules);
    private sealed record Cached(string Model, string PromptSha256, string Raw, double Seconds);

    /// <summary>
    /// "hard": for messier-42 and messier-45, the 3 best-ranked NASA images (no type) and the 2 best-ranked
    /// images that do not name the object: the cases where the rules can be wrong. Otherwise: one object,
    /// its 5 best kept and 5 dropped images.
    /// </summary>
    public static async Task<int> RunAsync(string importer, string[] args)
    {
        var hard = args.Length > 0 && args[0] == "hard";
        var models = args.Length > 1 ? args[1].Split(',') : ["gemma4:26b"];
        var read = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var file = JsonSerializer.Deserialize<BuildCandidates.CandidatesFile>(
            File.ReadAllText(Path.Combine(importer, "out", "image-candidates.json")), read)!;
        var slugs = hard ? ["messier-42", "messier-45"] : new[] { args.Length > 0 ? args[0] : "messier-42" };

        var work = new List<(CosmicObject Obj, Item Item)>();
        foreach (var slug in slugs)
        {
            var o = file.Objects.Single(x => x.Slug == slug);
            var obj = SearchMeasure.Load(importer, [slug])[0];
            static Item Kept(ImageCandidate c) => new(c.Source, c.SourceId, c.Title, c.Type, c.Credit, c.Url, true,
                $"kept, score {c.Score} ({string.Join(", ", c.Reasons)})");
            IEnumerable<Item> picked = hard
                ? o.Candidates.Where(c => c.Source == "nasa").Take(3).Select(Kept)
                    .Concat(o.Candidates.Where(c => !c.Named && c.Source != "nasa").Take(2).Select(Kept))
                : o.Candidates.Take(5).Select(Kept)
                    .Concat(o.Dropped.Where(d => d.Url is not null).GroupBy(d => d.Reason)
                        .SelectMany(g => g.Select((d, i) => (d, i))).OrderBy(x => x.i).Take(5)
                        .Select(x => new Item(x.d.Source, x.d.SourceId, x.d.Title, x.d.Type, x.d.Credit, x.d.Url, false, x.d.Reason)));
            work.AddRange(picked.Select(i => (obj, i)));
        }
        var promptsLog = new StringBuilder("# Exactly what each model call gets\n\n");

        var downloads = Path.Combine(importer, "downloads");
        using var fetcher = new CachedFetcher(downloads);
        using var ollama = new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(2) };
        var rows = new List<(string Slug, Item Item, string Model, ModelAnswer? Answer, double Seconds)>();

        foreach (var (obj, item) in work)
        {
            var slug = obj.Slug;
            var ext = Path.GetExtension(new Uri(item.Url!).AbsolutePath);
            var rel = Path.Combine("images", Uri.EscapeDataString(item.Id) + ext);
            await fetcher.GetAsync(item.Source, rel, item.Url!);
            var imagePath = Path.Combine(downloads, item.Source, rel);
            if (!File.Exists(imagePath)) { Console.WriteLine($"no image: {item.Source} {item.Id}"); continue; }
            var image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));

            var (names, description) = Text(downloads, item);
            var prompt = ModelCheck.Prompt(obj.Name ?? slug, obj.Aliases ?? [], item.Source, item.Id, item.Title, item.Type, names, description, item.Credit);
            var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)));
            promptsLog.AppendLine($"## {slug} / {item.Source} {item.Id}\n\nImage: {item.Url} (file {Path.GetRelativePath(importer, imagePath)}, {new FileInfo(imagePath).Length:N0} bytes)\n\nText:\n\n```\n{prompt}\n```\n");

            foreach (var model in models)
            {
                var cacheFile = Path.Combine(importer, "out", "model", model.Replace(':', '_'), item.Source, Uri.EscapeDataString(item.Id) + ".json");
                Cached? cached = File.Exists(cacheFile) ? JsonSerializer.Deserialize<Cached>(File.ReadAllText(cacheFile), read) : null;
                if (cached is null || cached.PromptSha256 != sha)
                {
                    Console.WriteLine($"{model}: {item.Source} {item.Id} ...");
                    var watch = Stopwatch.StartNew();
                    HttpResponseMessage response;
                    try
                    {
                    response = await ollama.PostAsJsonAsync("/api/generate", new
                    {
                        model, prompt, images = new[] { image }, stream = false, format = "json", think = false,
                        options = new { temperature = 0, seed = 1, num_predict = 300, num_ctx = 8192 },
                    });
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine($"{model}: {item.Source} {item.Id}: no answer in 2 minutes");
                        rows.Add((slug, item, model, null, watch.Elapsed.TotalSeconds));
                        continue;
                    }
                    using var _r = response;
                    response.EnsureSuccessStatusCode();
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    cached = new Cached(model, sha, doc.RootElement.GetProperty("response").GetString() ?? "", watch.Elapsed.TotalSeconds);
                    if (ModelCheck.Parse(cached.Raw) is not null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                        File.WriteAllText(cacheFile, JsonSerializer.Serialize(cached, BuildCandidates.Json));
                    }
                    else Console.WriteLine($"{model}: {item.Source} {item.Id}: unreadable answer, not cached: {cached.Raw}");
                }
                rows.Add((slug, item, model, ModelCheck.Parse(cached.Raw), cached.Seconds));
            }
        }

        var sb = new StringBuilder();
        File.WriteAllText(Path.Combine(importer, "out", hard ? "model-prompts-hard.md" : "model-prompts.md"), promptsLog.ToString());
        sb.AppendLine($"# Model test, {(hard ? "hard cases" : slugs[0])}, {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("| Object | Source | Id | Title | Rules | Model | Model says | Score | Reason | Seconds |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var r in rows)
            sb.AppendLine($"| {r.Slug} | {r.Item.Source} | {r.Item.Id} | {r.Item.Title.Replace("|", "/")} | {r.Item.Rules} | {r.Model} | " +
                $"{(r.Answer is null ? "unreadable" : r.Answer.Keep ? "keep" : "drop")} | {r.Answer?.Score} | {r.Answer?.Reason.Replace("|", "/")} | {r.Seconds:0.0} |");
        var report = Path.Combine(importer, "out", hard ? "model-test-hard.md" : "model-test.md");
        File.WriteAllText(report, sb.ToString());
        Console.WriteLine(sb.ToString());
        Console.WriteLine($"Written: {report}");
        return 0;
    }

    private static (IReadOnlyList<string> Names, string? Description) Text(string downloads, Item item)
    {
        var id = Uri.EscapeDataString(item.Id);
        if (item.Source == "nasa")
        {
            var f = Path.Combine(downloads, "nasa", "metadata", id + ".json");
            return ([], File.Exists(f) ? SourcePages.NasaMetadata(File.ReadAllText(f)).Text : null);
        }
        var page = Path.Combine(downloads, item.Source, "pages", id + ".html");
        if (!File.Exists(page)) return ([], null);
        var p = SourcePages.Djangoplicity(File.ReadAllText(page));
        return (p.Names, p.Description);
    }
}
