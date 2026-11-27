using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 D58: the agent. Every image the rules kept (and every image the agent dropped before) goes to the local
/// model with docs/image-rules.md, the object and the page text. The image (url size) is cached in
/// downloads/&lt;source&gt;/images/; each answer in out/agent/&lt;model&gt;/&lt;slug&gt;/&lt;source&gt;/&lt;id&gt;.json with the
/// prompt's SHA-256 (a changed rules file asks again). Results: out/agent-results.json; build-candidates applies them.
/// Ollama on this machine only. No tests (D24).
/// Run: dotnet run --project src -- agent [slug,slug,...] [model]
/// </summary>
public static class AgentRun
{
    private sealed record Cached(string Model, string PromptSha256, string Raw, double Seconds);

    public static async Task<int> RunAsync(string importer, string docs, string[] args)
    {
        var model = args.Length > 1 ? args[1] : "gemma4:26b";
        var read = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rules = await File.ReadAllTextAsync(Path.Combine(docs, "image-rules.md"));
        var ruleIds = AgentCheck.RuleIds(rules);
        var file = JsonSerializer.Deserialize<BuildCandidates.CandidatesFile>(
            File.ReadAllText(Path.Combine(importer, "out", "image-candidates.json")), read)!;
        var slugs = args.Length > 0 && args[0].Length > 0 ? args[0].Split(',').ToHashSet() : null;

        var work = file.Objects.Where(o => slugs is null || slugs.Contains(o.Slug))
            .SelectMany(o => o.Candidates.Select(c => (o.Slug, c.Source, c.SourceId, c.Title, c.Type, c.Credit, c.Url))
                .Concat(o.Dropped.Where(d => d.By == "agent").Select(d => (o.Slug, d.Source, d.SourceId, d.Title, d.Type, d.Credit, d.Url))))
            .ToList();
        var objects = SearchMeasure.Load(importer, work.Select(w => w.Slug).Distinct()).ToDictionary(o => o.Slug);
        Console.WriteLine($"Agent: {work.Count} images, model {model}, rules {string.Join(" ", ruleIds)}");

        var downloads = Path.Combine(importer, "downloads");
        using var fetcher = new CachedFetcher(downloads);
        using var ollama = new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMinutes(2) };
        var results = new List<AgentResult>();
        var noAnswer = new List<string>();
        var all = Stopwatch.StartNew();
        var asked = 0;

        for (var n = 0; n < work.Count; n++)
        {
            var w = work[n];
            var obj = objects[w.Slug];
            if (w.Url is null) { noAnswer.Add($"{w.Slug} {w.Source} {w.SourceId}: no url"); continue; }
            var ext = Path.GetExtension(new Uri(w.Url).AbsolutePath);
            var rel = Path.Combine("images", Uri.EscapeDataString(w.SourceId) + ext);
            await fetcher.GetAsync(w.Source, rel, w.Url);
            var imagePath = Path.Combine(downloads, w.Source, rel);
            if (!File.Exists(imagePath)) { noAnswer.Add($"{w.Slug} {w.Source} {w.SourceId}: image not downloaded"); continue; }

            var (names, description) = PageText(downloads, w.Source, w.SourceId);
            var prompt = AgentCheck.Prompt(rules, obj.Name ?? w.Slug, obj.Aliases ?? [], w.Source, w.SourceId, w.Title, w.Type, names, description, w.Credit);
            var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(prompt)));
            var cacheFile = Path.Combine(importer, "out", "agent", model.Replace(':', '_'), w.Slug, w.Source, Uri.EscapeDataString(w.SourceId) + ".json");
            var cached = File.Exists(cacheFile) ? JsonSerializer.Deserialize<Cached>(File.ReadAllText(cacheFile), read) : null;
            AgentAnswer? answer = cached is not null && cached.PromptSha256 == sha ? AgentCheck.Parse(cached.Raw, ruleIds) : null;

            if (answer is null)
            {
                asked++;
                var watch = Stopwatch.StartNew();
                try
                {
                    var image = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath));
                    using var response = await ollama.PostAsJsonAsync("/api/generate", new
                    {
                        model, prompt, images = new[] { image }, stream = false, format = "json", think = false,
                        options = new { temperature = 0, seed = 1, num_predict = 300, num_ctx = 8192 },
                    });
                    response.EnsureSuccessStatusCode();
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var raw = doc.RootElement.GetProperty("response").GetString() ?? "";
                    answer = AgentCheck.Parse(raw, ruleIds);
                    if (answer is not null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
                        File.WriteAllText(cacheFile, JsonSerializer.Serialize(new Cached(model, sha, raw, watch.Elapsed.TotalSeconds), BuildCandidates.Json));
                    }
                    else noAnswer.Add($"{w.Slug} {w.Source} {w.SourceId}: unreadable: {raw}");
                }
                catch (Exception e) when (e is TaskCanceledException or HttpRequestException)
                {
                    noAnswer.Add($"{w.Slug} {w.Source} {w.SourceId}: {e.GetType().Name}");
                }
            }
            if (answer is not null)
                results.Add(new AgentResult(w.Slug, w.Source, w.SourceId, answer.Keep, answer.Broken, answer.Score, answer.Reason));

            if ((n + 1) % 10 == 0 || n + 1 == work.Count)
            {
                var per = asked > 0 ? all.Elapsed.TotalSeconds / asked : 0;
                var left = TimeSpan.FromSeconds(per * (work.Count - n - 1));
                Console.WriteLine($"{n + 1}/{work.Count}  dropped so far: {results.Count(r => !r.Keep)}  no answer: {noAnswer.Count}  ~{per:0.0} s/image  ~{left:hh\\:mm} left");
                File.WriteAllText(Path.Combine(importer, "out", "agent-results.json"), JsonSerializer.Serialize(results, BuildCandidates.Json));
            }
        }

        File.WriteAllText(Path.Combine(importer, "out", "agent-results.json"), JsonSerializer.Serialize(results, BuildCandidates.Json));
        File.WriteAllLines(Path.Combine(importer, "out", "agent-no-answer.txt"), noAnswer);
        Console.WriteLine($"Done: {results.Count} answers, {results.Count(r => !r.Keep)} dropped, {noAnswer.Count} no answer. Next: build-candidates");
        return 0;
    }

    private static (IReadOnlyList<string> Names, string? Description) PageText(string downloads, string source, string sourceId)
    {
        var id = Uri.EscapeDataString(sourceId);
        if (source == "nasa")
        {
            var f = Path.Combine(downloads, "nasa", "metadata", id + ".json");
            return ([], File.Exists(f) ? SourcePages.NasaMetadata(File.ReadAllText(f)).Text : null);
        }
        var page = Path.Combine(downloads, source, "pages", id + ".html");
        if (!File.Exists(page)) return ([], null);
        var p = SourcePages.Djangoplicity(File.ReadAllText(page));
        return (p.Names, p.Description);
    }
}
