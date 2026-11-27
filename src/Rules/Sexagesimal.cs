using System.Globalization;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>X01 D33: OpenNGC's digits are kept; only the symbols change.</summary>
public static partial class Sexagesimal
{
    [GeneratedRegex(@"^(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)$")]
    private static partial Regex RaPattern();

    [GeneratedRegex(@"^([+-])(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)$")]
    private static partial Regex DecPattern();

    public static string? FormatRa(string? ra) =>
        Match(RaPattern(), ra) is { } m ? $"{m.Groups[1].Value}h {m.Groups[2].Value}m {m.Groups[3].Value}s" : null;

    public static string? FormatDec(string? dec) =>
        Match(DecPattern(), dec) is { } m ? $"{m.Groups[1].Value}{m.Groups[2].Value}° {m.Groups[3].Value}′ {m.Groups[4].Value}″" : null;

    /// <summary>For the report only (comparing with Wikidata).</summary>
    public static double? RaDegrees(string? ra) =>
        Match(RaPattern(), ra) is { } m ? 15 * Sum(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value) : null;

    /// <summary>For the report only (comparing with Wikidata).</summary>
    public static double? DecDegrees(string? dec) =>
        Match(DecPattern(), dec) is { } m
            ? (m.Groups[1].Value == "-" ? -1 : 1) * Sum(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value)
            : null;

    private static Match? Match(Regex pattern, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var m = pattern.Match(value.Trim());
        return m.Success ? m : null;
    }

    private static double Sum(string a, string b, string c) =>
        double.Parse(a, CultureInfo.InvariantCulture)
        + double.Parse(b, CultureInfo.InvariantCulture) / 60
        + double.Parse(c, CultureInfo.InvariantCulture) / 3600;
}
