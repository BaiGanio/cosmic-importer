using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BGAPI.Cosmic.Importer.Rules;

namespace BGAPI.Cosmic.Importer;

/// <summary>
/// X01 stage 5: sends the objects with images to BGAPI. Builds out/import.json (always), gets a token for
/// the client cosmic-importer (client credentials), and POSTs each batch to /api/cosmic/import. The answers
/// go to out/send-result.json. No tests for the HTTP part (D24); the join is ImportPayload.
/// Run: dotnet run --project src -- send [--dry-run] [--api URL] [--token-url URL]
/// Defaults: --api https://localhost:44364 (the BGAPI launch profile; a localhost dev certificate is accepted), --token-url http://localhost:8080/realms/baiganio-dev/protocol/openid-connect/token.
/// Secret: env COSMIC_IMPORTER_SECRET; on a localhost token URL without it, the dev secret from the brief.
/// </summary>
public static class Send
{
    public static async Task<int> RunAsync(string importer, string docs, string[] args)
    {
        string Arg(string name, string fallback)
        {
            var i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }
        var dryRun = args.Contains("--dry-run");
        var api = Arg("--api", "https://localhost:44364").TrimEnd('/');
        var tokenUrl = Arg("--token-url", "http://localhost:8080/realms/baiganio-dev/protocol/openid-connect/token");

        var read = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var outDir = Path.Combine(importer, "out");
        var facts = JsonSerializer.Deserialize<FactsFile>(await File.ReadAllTextAsync(Path.Combine(outDir, "messier.json")), read)!.Objects;
        var candidates = JsonSerializer.Deserialize<BuildCandidates.CandidatesFile>(
            await File.ReadAllTextAsync(Path.Combine(outDir, "image-candidates.json")), read)!;
        var picksFile = Path.Combine(docs, "picks.json");
        var picks = File.Exists(picksFile)
            ? JsonSerializer.Deserialize<ReviewServer.PicksFile>(await File.ReadAllTextAsync(picksFile), read)!.Objects
            : new Dictionary<string, ReviewServer.Choices>();

        var payload = ImportPayload.Build(facts, candidates, picks);
        var importFile = Path.Combine(outDir, "import.json");
        await File.WriteAllTextAsync(importFile, JsonSerializer.Serialize(new { objects = payload.Objects }, BuildCandidates.Json) + "\n");
        Console.WriteLine($"Objects with images: {payload.Objects.Count}, images: {payload.Objects.Sum(o => o.Images.Count)}; written: {importFile}");
        foreach (var s in payload.Skipped) Console.WriteLine($"  skipped: {s}");
        if (dryRun) { Console.WriteLine("Dry run: nothing sent."); return 0; }
        if (payload.Objects.Count == 0) { Console.WriteLine("Nothing to send."); return 1; }

        var secret = Environment.GetEnvironmentVariable("COSMIC_IMPORTER_SECRET");
        if (secret is null)
        {
            if (!new Uri(tokenUrl).IsLoopback) { Console.Error.WriteLine("Set COSMIC_IMPORTER_SECRET for a token URL that is not localhost."); return 1; }
            secret = "cosmic-importer-secret";
        }

        using var handler = new HttpClientHandler();
        if (new Uri(api).IsLoopback) handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        var tokenAnswer = await http.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials", ["client_id"] = "cosmic-importer", ["client_secret"] = secret,
        }));
        if (!tokenAnswer.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Token: {(int)tokenAnswer.StatusCode} {await tokenAnswer.Content.ReadAsStringAsync()}");
            return 1;
        }
        var token = (await tokenAnswer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var answers = new List<JsonElement>();
        var failed = false;
        foreach (var batch in ImportPayload.Batches(payload.Objects))
        {
            var response = await http.PostAsJsonAsync($"{api}/api/cosmic/import", new { objects = batch }, BuildCandidates.Json);
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Batch of {batch.Count}: {(int)response.StatusCode} {body}");
            if (!response.IsSuccessStatusCode) { failed = true; break; }
            answers.Add(JsonSerializer.Deserialize<JsonElement>(body));
        }
        var resultFile = Path.Combine(outDir, "send-result.json");
        await File.WriteAllTextAsync(resultFile, JsonSerializer.Serialize(new { sentOn = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), api, answers }, BuildCandidates.Json) + "\n");
        Console.WriteLine($"Written: {resultFile}");
        return failed ? 1 : 0;
    }

    private sealed record FactsFile(IReadOnlyList<CosmicObject> Objects);
}
