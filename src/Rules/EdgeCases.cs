using System.Text.Json;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>One entry of docs/edge-cases.json (X01 D26).</summary>
public sealed record EdgeCase
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string Field { get; init; }
    public JsonElement Value { get; init; }
    public string? Reason { get; init; }
    public required string Status { get; init; }
}

/// <summary>X01 D26: every entry is applied, open ones too.</summary>
public static class EdgeCases
{
    private sealed record EdgeCaseFile(List<EdgeCase> Entries);

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<EdgeCase> Parse(string json) =>
        JsonSerializer.Deserialize<EdgeCaseFile>(json, Options)?.Entries ?? [];

    /// <summary>The OpenNGC row for a Messier number. An "identity" entry names it; else the row with that M number that is not a Dup.</summary>
    public static OpenNgcRow? RowFor(int messier, IReadOnlyList<OpenNgcRow> rows, IReadOnlyList<EdgeCase> cases)
    {
        var identity = cases.FirstOrDefault(c => c.Slug == Merge.Slug(messier) && c.Field == "identity");
        if (identity is not null)
        {
            var name = identity.Value.GetString();
            return rows.FirstOrDefault(r => r.Name == name);
        }
        return rows.FirstOrDefault(r => r.Messier == messier && r.Type != "Dup");
    }

    public static CosmicObject Apply(CosmicObject obj, IReadOnlyList<EdgeCase> cases)
    {
        foreach (var c in cases.Where(c => c.Slug == obj.Slug && c.Field != "identity"))
        {
            obj = c.Field switch
            {
                "discovered" => obj with { Discovered = AsString(c.Value) },
                "category" => obj with { Category = AsString(c.Value) },
                "distanceLy" => obj with { DistanceLy = c.Value.ValueKind == JsonValueKind.Number ? c.Value.GetDouble() : null },
                _ => throw new InvalidOperationException($"{c.Id}: field '{c.Field}' is not supported"),
            };
        }
        return obj;
    }

    private static string? AsString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
