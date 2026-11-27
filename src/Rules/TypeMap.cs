namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>X01 D31 (category, clear types only) and D32 (subcategory).</summary>
public static class TypeMap
{
    public static string? Category(string type) => type switch
    {
        "G" => "galaxy",
        "GCl" or "OCl" => "cluster",
        "PN" or "SNR" or "Neb" or "HII" or "RfN" or "EmN" => "nebula",
        _ => null,
    };

    public static string? Subcategory(string type, string? hubble) => type switch
    {
        "G" => Galaxy(hubble),
        "GCl" => "globular",
        "OCl" => "open",
        "PN" => "planetary",
        "SNR" => "supernova remnant",
        "HII" or "EmN" => "emission",
        "RfN" => "reflection",
        _ => null,
    };

    private static string? Galaxy(string? hubble)
    {
        if (string.IsNullOrWhiteSpace(hubble) || hubble.Contains('?')) return null;
        string? word =
            hubble.StartsWith('E') ? "elliptical"
            : hubble.StartsWith("S0", StringComparison.Ordinal) ? "lenticular"
            : hubble.StartsWith("SAB", StringComparison.Ordinal) || hubble.StartsWith("SB", StringComparison.Ordinal) ? "barred spiral"
            : hubble.StartsWith('S') ? "spiral"
            : null;
        return word is null ? null : $"{word} ({hubble})";
    }
}
