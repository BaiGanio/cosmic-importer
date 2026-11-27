using System.Text.Json;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>What the model said about one image. Null fields: the model did not say.</summary>
public sealed record ModelAnswer(bool Keep, int Score, string Reason, bool? RealPhoto, bool? ShowsObject, bool? HasLabelsOrPanels);

/// <summary>
/// X01 D57 step 3: one fixed question to a local vision model, with the image and its text, and a fixed
/// JSON answer. The model only ranks and suggests; a human decides (D5).
/// </summary>
public static partial class ModelCheck
{
    public const int MaxDescription = 1000;

    public static string Prompt(string objectName, IReadOnlyList<string> aliases, string source, string id,
        string title, string? type, IReadOnlyList<string> pageNames, string? description, string? credit)
    {
        var desc = description is null ? "(none)"
            : description.Length > MaxDescription ? description[..MaxDescription] + "…" : description;
        return $$"""
            You check images for a public astronomy website. It shows only real photographs of sky objects.
            The object: {{objectName}}{{(aliases.Count > 0 ? $" ({string.Join(", ", aliases)})" : "")}}.
            The image comes from {{source}}, id {{id}}. What its page says:
            Title: {{title}}
            Type: {{type ?? "(none)"}}
            Names on the page: {{(pageNames.Count > 0 ? string.Join(", ", pageNames) : "(none)")}}
            Description: {{desc}}
            Credit: {{credit ?? "(none)"}}

            Look at the image and read the text. Answer with JSON only, no other words:
            {"realPhoto": true or false, "showsObject": true or false, "hasLabelsOrPanels": true or false, "score": 0 to 10, "keep": true or false, "reason": "at most 15 words"}
            realPhoto: a real telescope or camera photograph, not an artist's impression, a drawing, a diagram, a chart or a simulation.
            showsObject: the object above is the main subject of the image.
            hasLabelsOrPanels: the image has text, arrows, scale bars or several panels.
            keep: true only when realPhoto and showsObject are true and hasLabelsOrPanels is false.
            score: how good the image is for the object's page, 10 is best.
            """;
    }

    [GeneratedRegex(@"\{[\s\S]*\}")]
    private static partial Regex JsonObject();

    public static ModelAnswer? Parse(string text)
    {
        var m = JsonObject().Match(text);
        if (!m.Success) return null;
        try
        {
            using var doc = JsonDocument.Parse(m.Value);
            var r = doc.RootElement;
            if (!r.TryGetProperty("keep", out var keep) || keep.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            var score = r.TryGetProperty("score", out var s) && s.TryGetDouble(out var d) ? (int)Math.Round(Math.Clamp(d, 0, 10)) : 0;
            var reason = r.TryGetProperty("reason", out var why) && why.ValueKind == JsonValueKind.String ? why.GetString()! : "";
            return new ModelAnswer(keep.GetBoolean(), score, reason, Bool(r, "realPhoto"), Bool(r, "showsObject"), Bool(r, "hasLabelsOrPanels"));
        }
        catch (JsonException) { return null; }
    }

    private static bool? Bool(JsonElement r, string name) =>
        r.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : null;
}
