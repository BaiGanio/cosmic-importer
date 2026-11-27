using System.Text.Json;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>What the agent said about one image for one object. Broken: rule ids from image-rules.md.</summary>
public sealed record AgentAnswer(bool Keep, IReadOnlyList<string> Broken, int Score, string Reason);

/// <summary>One agent answer, stored in out/agent-results.json.</summary>
public sealed record AgentResult(string Slug, string Source, string SourceId, bool Keep, IReadOnlyList<string> Broken, int Score, string Reason);

/// <summary>
/// X01 D58: the agent. It gets Lyuben's rules file (docs/image-rules.md) as written, the
/// object, and the page text, plus the image, and answers with the rule ids the image breaks.
/// </summary>
public static partial class AgentCheck
{
    public static string Prompt(string rules, string objectName, IReadOnlyList<string> aliases, string source, string id,
        string title, string? type, IReadOnlyList<string> pageNames, string? description, string? credit)
    {
        var desc = description is null ? "(none)"
            : description.Length > ModelCheck.MaxDescription ? description[..ModelCheck.MaxDescription] + "…" : description;
        return $"""
            You check images for a public astronomy website. It shows only real photographs of sky objects.
            These are the rules, written by the site owner:

            {rules.Trim()}

            The object: {objectName}{(aliases.Count > 0 ? $" ({string.Join(", ", aliases)})" : "")}.
            The image comes from {source}, id {id}. What its page says:
            Title: {title}
            Type: {type ?? "(none)"}
            Names on the page: {(pageNames.Count > 0 ? string.Join(", ", pageNames) : "(none)")}
            Description: {desc}
            Credit: {credit ?? "(none)"}

            Look at the image and read the text. Apply the rules. Answer with JSON only, no other words, in the format the rules give.
            """;
    }

    [GeneratedRegex(@"^- (R\d+):", RegexOptions.Multiline)]
    private static partial Regex RuleLine();

    public static IReadOnlyList<string> RuleIds(string rules) =>
        RuleLine().Matches(rules).Select(m => m.Groups[1].Value).ToList();

    [GeneratedRegex(@"\{[\s\S]*\}")]
    private static partial Regex JsonObject();

    /// <summary>Null when the answer is not JSON, has no "keep", or names a rule the file does not have.</summary>
    public static AgentAnswer? Parse(string text, IReadOnlyList<string> ruleIds)
    {
        var m = JsonObject().Match(text);
        if (!m.Success) return null;
        try
        {
            using var doc = JsonDocument.Parse(m.Value);
            var r = doc.RootElement;
            if (!r.TryGetProperty("keep", out var keep) || keep.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            var broken = r.TryGetProperty("broken", out var b) && b.ValueKind == JsonValueKind.Array
                ? b.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Distinct().ToList()
                : [];
            if (broken.Any(id => !ruleIds.Contains(id))) return null;
            var score = r.TryGetProperty("score", out var s) && s.TryGetDouble(out var d) ? (int)Math.Round(Math.Clamp(d, 0, 10)) : 0;
            var reason = r.TryGetProperty("reason", out var why) && why.ValueKind == JsonValueKind.String ? why.GetString()! : "";
            return new AgentAnswer(keep.GetBoolean() && broken.Count == 0, broken, score, reason);
        }
        catch (JsonException) { return null; }
    }
}

/// <summary>X01 D58: an image the agent drops moves from Kept to Dropped ("agent", its rules and reason).</summary>
public static class AgentApply
{
    public static (IReadOnlyList<ImageCandidate> Kept, IReadOnlyList<DroppedImage> Dropped) Apply(
        IEnumerable<ImageCandidate> candidates, IEnumerable<AgentResult> results)
    {
        var drops = results.Where(r => !r.Keep).ToDictionary(r => (r.Slug, r.Source, r.SourceId));
        var kept = new List<ImageCandidate>();
        var dropped = new List<DroppedImage>();
        foreach (var c in candidates)
        {
            if (drops.TryGetValue((c.Slug, c.Source, c.SourceId), out var r))
                dropped.Add(new DroppedImage(c.Slug, c.Source, c.SourceId, c.Title, c.Type, "agent",
                    $"{string.Join(", ", r.Broken)}: {r.Reason}", c.Credit, c.SourcePage, c.Sizes, c.Url, c.ThumbUrl));
            else kept.Add(c);
        }
        return (kept, dropped);
    }
}
