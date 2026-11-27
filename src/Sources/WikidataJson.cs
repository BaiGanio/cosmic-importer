using System.Globalization;
using System.Text.Json;

namespace BGAPI.Cosmic.Importer.Sources;

/// <summary>Reads one Special:EntityData JSON file. Data only: nothing in it is run.</summary>
public static class WikidataJson
{
    private const string MessierCatalog = "Q14530";

    public static WikidataEntity Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var entity = doc.RootElement.GetProperty("entities").EnumerateObject().First().Value;
        var claims = entity.TryGetProperty("claims", out var cl) ? cl : default;

        List<JsonElement> Claims(string property) =>
            claims.ValueKind == JsonValueKind.Object && claims.TryGetProperty(property, out var list)
                ? list.EnumerateArray().ToList()
                : [];

        var messierCode = Claims("P528")
            .Where(c => Qualifiers(c, "P972").Contains(MessierCatalog))
            .Select(StringValue).OfType<string>().FirstOrDefault();

        return new WikidataEntity
        {
            Id = entity.GetProperty("id").GetString()!,
            Label = entity.TryGetProperty("labels", out var labels) && labels.TryGetProperty("en", out var en)
                ? en.GetProperty("value").GetString()
                : null,
            Messier = messierCode is not null
                && int.TryParse(new string(messierCode.Where(char.IsAsciiDigit).ToArray()), NumberStyles.None, CultureInfo.InvariantCulture, out var m)
                    ? m : null,
            CatalogCodes = Claims("P528").Select(StringValue).OfType<string>().ToList(),
            Distances = Claims("P2583")
                .Where(c => Value(c) is not null)
                .Select(c => new DistanceClaim(
                    Value(c)!.Value.GetProperty("amount").GetString()!,
                    Value(c)!.Value.TryGetProperty("unit", out var u) ? u.GetString()!.Split('/')[^1] : "",
                    c.GetProperty("rank").GetString()!,
                    CitesPublishedWork(c)))
                .ToList(),
            Discovered = Claims("P575")
                .Where(c => c.GetProperty("rank").GetString() != "deprecated" && Value(c) is not null)
                .OrderBy(c => c.GetProperty("rank").GetString() == "preferred" ? 0 : 1)
                .Select(c => Value(c)!.Value)
                .Select(v => new WikidataTime(
                    v.GetProperty("time").GetString()!,
                    v.GetProperty("precision").GetInt32(),
                    v.GetProperty("calendarmodel").GetString()!.Split('/')[^1]))
                .FirstOrDefault(),
            RaDeg = Claims("P6257").Select(Amount).FirstOrDefault(a => a is not null),
            DecDeg = Claims("P6258").Select(Amount).FirstOrDefault(a => a is not null),
            Magnitudes = Claims("P1215").Select(Amount).OfType<double>().ToList(),
            DiscovererIds = Claims("P61")
                .Select(c => Value(c) is { } v && v.TryGetProperty("id", out var id) ? id.GetString() : null)
                .OfType<string>().ToList(),
        };
    }

    private static JsonElement? Value(JsonElement claim) =>
        claim.GetProperty("mainsnak").TryGetProperty("datavalue", out var dv) ? dv.GetProperty("value") : null;

    private static string? StringValue(JsonElement claim) =>
        Value(claim) is { ValueKind: JsonValueKind.String } v ? v.GetString() : null;

    private static double? Amount(JsonElement claim) =>
        Value(claim) is { } v && v.TryGetProperty("amount", out var a)
        && double.TryParse(a.GetString()!.TrimStart('+'), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d : null;

    private static List<string> Qualifiers(JsonElement claim, string property) =>
        claim.TryGetProperty("qualifiers", out var q) && q.TryGetProperty(property, out var list)
            ? list.EnumerateArray()
                .Select(s => s.TryGetProperty("datavalue", out var dv) && dv.GetProperty("value").TryGetProperty("id", out var id) ? id.GetString() : null)
                .OfType<string>().ToList()
            : [];

    private static bool CitesPublishedWork(JsonElement claim) =>
        claim.TryGetProperty("references", out var refs)
        && refs.EnumerateArray().Any(r => r.TryGetProperty("snaks", out var s) && s.TryGetProperty("P248", out _));
}
