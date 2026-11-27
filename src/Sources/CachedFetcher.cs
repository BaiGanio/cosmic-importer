using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BGAPI.Cosmic.Importer.Sources;

/// <summary>
/// X01 stage 3, APPROACH.md "Rules for every stage": one request at a time, about 1 per second,
/// obeys 429 and Retry-After, generic User-Agent, no personal data. Every answer is cached in
/// downloads/&lt;source&gt;/ (D45), and listed in downloads/&lt;source&gt;/index.tsv. A cached answer is never
/// asked again. Only a 200 with a body is cached (NOIRLab sent empty 200s on 2026-11-24). No tests (D24).
/// </summary>
public sealed class CachedFetcher(string downloads) : IDisposable
{
    private static readonly TimeSpan Gap = TimeSpan.FromMilliseconds(1100);
    private readonly HttpClient _http = NewClient();
    private DateTime _last = DateTime.MinValue;

    public int Asked { get; private set; }
    public int FromCache { get; private set; }
    public List<string> Failures { get; } = [];

    private static HttpClient NewClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; cosmic-importer/0.1)");
        return http;
    }

    public async Task<string?> GetAsync(string source, string relPath, string url)
    {
        var path = Path.Combine(downloads, source, relPath);
        if (File.Exists(path)) { FromCache++; return await File.ReadAllTextAsync(path); }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var wait = _last + Gap - DateTime.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait);
            _last = DateTime.UtcNow;
            Asked++;

            HttpResponseMessage response;
            try { response = await _http.GetAsync(url); }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                Failures.Add($"network error ({e.GetType().Name}) {url}");
                return null;
            }
            using var _ = response;
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retry = response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is { } d ? d - DateTimeOffset.UtcNow : TimeSpan.FromSeconds(60));
                Console.WriteLine($"429 from {source}, waiting {retry.TotalSeconds:0} s");
                await Task.Delay(retry < TimeSpan.Zero ? TimeSpan.Zero : retry);
                continue;
            }
            if (!response.IsSuccessStatusCode)
            {
                Failures.Add($"{(int)response.StatusCode} {url}");
                return null;
            }

            var body = await response.Content.ReadAsByteArrayAsync();
            if (body.Length == 0)
            {
                Failures.Add($"200 with an empty body {url}");
                return null;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path + ".part", body);
            File.Move(path + ".part", path, overwrite: false);
            await AppendIndexAsync(source, relPath, url, body);
            return Encoding.UTF8.GetString(body);
        }
        Failures.Add($"429 three times {url}");
        return null;
    }

    private async Task AppendIndexAsync(string source, string relPath, string url, byte[] body)
    {
        var index = Path.Combine(downloads, source, "index.tsv");
        if (!File.Exists(index)) await File.WriteAllTextAsync(index, "path\turl\tgotOn\tbytes\tsha256\n");
        var sha = Convert.ToHexStringLower(SHA256.HashData(body));
        await File.AppendAllTextAsync(index, $"{relPath.Replace('\\', '/')}\t{url}\t{DateTime.UtcNow:yyyy-MM-dd}\t{body.Length}\t{sha}\n");
    }

    public void Dispose() => _http.Dispose();
}
