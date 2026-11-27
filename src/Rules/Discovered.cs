using System.Globalization;
using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>Wikidata P575 as text, with Wikidata's precision: year, month or day.</summary>
public static partial class Discovered
{
    [GeneratedRegex(@"^([+-])(\d+)-(\d{2})-(\d{2})T")]
    private static partial Regex TimePattern();

    public static string? Format(WikidataTime? time)
    {
        if (time is null) return null;
        var m = TimePattern().Match(time.Time);
        if (!m.Success) return null;

        var year = long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        var era = m.Groups[1].Value == "-" ? " BC" : "";
        return time.Precision switch
        {
            9 => $"{year}{era}",
            10 => $"{year}-{m.Groups[3].Value}{era}",
            11 => $"{year}-{m.Groups[3].Value}-{m.Groups[4].Value}{era}",
            _ => null,
        };
    }
}
