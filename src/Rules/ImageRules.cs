using System.Net;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>X01 stage 3: host check (D9), credit text (D53), real pictures only (D51).</summary>
public static partial class ImageRules
{
    /// <summary>D9: https only, the host equals one on the list (no case), no subdomain.</summary>
    public static bool HostAllowed(string? url, IEnumerable<string> allowed) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && allowed.Any(h => string.Equals(h, uri.Host, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
    [GeneratedRegex(@"<br\s*/?>|</p>\s*<p[^>]*>", RegexOptions.IgnoreCase)] private static partial Regex LineBreak();
    [GeneratedRegex(@"<[^>]+>")] private static partial Regex Tag();

    /// <summary>
    /// D53: the credit as the page shows it. Words unchanged; HTML tags go, HTML codes become their
    /// characters, a line break (br, or a new paragraph) stays a line break, other white space is one
    /// space (as a browser shows it).
    /// </summary>
    public static string CleanCredit(string html)
    {
        var text = Spaces().Replace(html, " ");
        text = LineBreak().Replace(text, "\n");
        text = WebUtility.HtmlDecode(Tag().Replace(text, ""));
        return string.Join("\n", text.Split('\n')
            .Select(l => Spaces().Replace(l, " ").Trim())
            .Where(l => l.Length > 0));
    }

    private static readonly HashSet<string> NotReal = new(StringComparer.Ordinal) { "Artwork", "Simulation", "Chart", "Collage" };

    /// <summary>D51, D56: the ESA/ESO/NOIRLab image type says it is not a real picture (Collage: panels, labels).</summary>
    public static bool NotARealPicture(string? type) => type is not null && NotReal.Contains(type);

    [GeneratedRegex(@"\b(artist|illustration|concept)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex NotRealWords();

    /// <summary>D51: NASA has no type; its text says "artist", "illustration" or "concept".</summary>
    public static bool NasaTextSaysNotReal(string? text) => text is not null && NotRealWords().IsMatch(text);
}
