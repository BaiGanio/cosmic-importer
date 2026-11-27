using System.Net;
using System.Text;
using System.Text.Json;
using BGAPI.Cosmic.Importer.Rules;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 stage 3 to 4: a local review page (D52, D57). Tab "Kept": the ranked images the rules kept;
/// untick to drop. Tab "Dropped": what a rule threw away, and why; tick to bring back. Save writes
/// docs/picks.json: per slug, the human "drop" and "keep" (brought back) lists. The final set
/// of an object = its kept images - drop + keep, so a new ranking run keeps the human choices. Listens on http://localhost:5199/ only, until Ctrl+C. No tests (D24).
/// Run: dotnet run --project src -- review
/// </summary>
public static class ReviewServer
{
    private const string Prefix = "http://localhost:5199/";
    private const int MaxBody = 5_000_000;

    public sealed record Pick(string Source, string Id);
    public sealed record Choices(List<Pick> Drop, List<Pick> Keep);
    public sealed record PicksFile(string SavedOn, Dictionary<string, Choices> Objects);
    private sealed record SaveBody(Dictionary<string, Choices>? Objects);

    public static async Task<int> RunAsync(string importer, string docs)
    {
        var html = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Review", "review.html"));
        var candidatesFile = Path.Combine(importer, "out", "image-candidates.json");
        var picksFile = Path.Combine(docs, "picks.json");
        var read = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var candidates = JsonSerializer.Deserialize<BuildCandidates.CandidatesFile>(await File.ReadAllTextAsync(candidatesFile), read)!;
        var kept = candidates.Objects.ToDictionary(o => o.Slug, o => o.Candidates.Select(c => (c.Source, c.SourceId)).ToHashSet());
        var dropped = candidates.Objects.ToDictionary(o => o.Slug, o => o.Dropped.Select(c => (c.Source, c.SourceId)).ToHashSet());

        using var listener = new HttpListener();
        listener.Prefixes.Add(Prefix);
        listener.Start();
        Console.WriteLine($"Review page: {Prefix}  (Ctrl+C to stop). Picks: {picksFile}");
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); listener.Stop(); };

        while (!stop.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); }
            catch (Exception) when (stop.IsCancellationRequested) { break; }
            catch (HttpListenerException) { break; }
            try { await HandleAsync(ctx); }
            catch (Exception e) { await SendAsync(ctx, 500, "text/plain", e.Message); }
        }
        return 0;

        async Task HandleAsync(HttpListenerContext ctx)
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            if (ctx.Request.HttpMethod == "GET" && path == "/")
                await SendAsync(ctx, 200, "text/html; charset=utf-8", html);
            else if (ctx.Request.HttpMethod == "GET" && path == "/data")
            {
                var picks = File.Exists(picksFile)
                    ? JsonSerializer.Deserialize<PicksFile>(await File.ReadAllTextAsync(picksFile), read)!.Objects
                    : new Dictionary<string, Choices>();
                await SendAsync(ctx, 200, "application/json",
                    JsonSerializer.Serialize(new { objects = candidates.Objects, picks }, BuildCandidates.Json));
            }
            else if (ctx.Request.HttpMethod == "POST" && path == "/save")
            {
                if (ctx.Request.ContentLength64 > MaxBody) { await SendAsync(ctx, 413, "text/plain", "too big"); return; }
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var body = JsonSerializer.Deserialize<SaveBody>(await reader.ReadToEndAsync(), read);
                var clean = new Dictionary<string, Choices>();
                foreach (var (slug, choices) in body?.Objects ?? [])
                {
                    if (!kept.TryGetValue(slug, out var k) || !dropped.TryGetValue(slug, out var d))
                    { await SendAsync(ctx, 400, "text/plain", $"unknown object {slug}"); return; }
                    var badDrop = (choices.Drop ?? []).FirstOrDefault(p => !k.Contains((p.Source, p.Id)));
                    var badKeep = (choices.Keep ?? []).FirstOrDefault(p => !d.Contains((p.Source, p.Id)));
                    if (badDrop is not null || badKeep is not null)
                    {
                        var bad = badDrop ?? badKeep!;
                        await SendAsync(ctx, 400, "text/plain", $"unknown image {bad.Source} {bad.Id} for {slug}");
                        return;
                    }
                    var c = new Choices((choices.Drop ?? []).Distinct().ToList(), (choices.Keep ?? []).Distinct().ToList());
                    if (c.Drop.Count + c.Keep.Count > 0) clean[slug] = c;
                }
                var file = new PicksFile(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), clean);
                await File.WriteAllTextAsync(picksFile, JsonSerializer.Serialize(file, BuildCandidates.Json) + "\n");
                await SendAsync(ctx, 200, "text/plain",
                    $"{clean.Sum(p => p.Value.Drop.Count)} dropped by you, {clean.Sum(p => p.Value.Keep.Count)} brought back");
                Console.WriteLine($"Saved {picksFile}");
            }
            else await SendAsync(ctx, 404, "text/plain", "not found");
        }
    }

    private static async Task SendAsync(HttpListenerContext ctx, int status, string type, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = type;
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
        ctx.Response.Close();
    }
}
