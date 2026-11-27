using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>
/// X01 D57: ranking rules. They move an image up or down and never throw one away. Weights: named 20 (more than
/// all the others together, so every named image ranks above every image that does not name the object),
/// Observation 4, Photographic 1, main image of a release 2, ESA/ESO/NOIRLab 1 (curated pages, full credits).
/// </summary>
public static partial class Ranking
{
    [GeneratedRegex(@"^[a-z]+\d+a$")]
    private static partial Regex MainImage();

    /// <summary>ESA/ESO/NOIRLab: the "a" image of a release (heic1502a); b, c, ... are mostly zooms and charts.</summary>
    public static bool IsMainImage(string id) => MainImage().IsMatch(id);

    public static (int Score, IReadOnlyList<string> Reasons) Rank(ImageCandidate c)
    {
        var score = 0;
        var reasons = new List<string>();
        void Add(bool on, int points, string why) { if (on) { score += points; reasons.Add(why); } }
        Add(c.Named, 20, "named");
        Add(c.Type == "Observation", 4, "Observation");
        Add(c.Type == "Photographic", 1, "Photographic");
        Add(c.Source != "nasa" && IsMainImage(c.SourceId), 2, "main image");
        Add(c.Source != "nasa", 1, "ESA/ESO/NOIRLab");
        return (score, reasons);
    }

    private static readonly string[] SourceOrder = ["esahubble", "esawebb", "eso", "noirlab", "nasa"];

    public static IReadOnlyList<ImageCandidate> Order(IEnumerable<ImageCandidate> candidates) =>
        candidates.Select(c => { var (s, r) = Rank(c); return c with { Score = s, Reasons = r }; })
            .OrderByDescending(c => c.Score)
            .ThenBy(c => Array.IndexOf(SourceOrder, c.Source))
            .ThenBy(c => c.SourceId, StringComparer.Ordinal)
            .ToList();
}
