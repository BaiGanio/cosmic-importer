using System.Globalization;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>The picked distance, or why the field stays empty.</summary>
public sealed record DistancePick(double? LightYears, DistanceClaim? Claim, string? WhyEmpty);

/// <summary>X01 D28 (which Wikidata value) and D29 (the source's significant digits).</summary>
public static class Distance
{
    public const string LightYear = "Q531";
    public const string Parsec = "Q12129";
    public const string Kiloparsec = "Q11929860";
    public const string Megaparsec = "Q3773454";

    // IAU: 1 pc = 3.261563777 ly.
    private const decimal LightYearsPerParsec = 3.261563777m;

    /// <summary>Digits after the sign and the leading zeros. Trailing zeros of an integer count (300 → 3).</summary>
    public static int SignificantDigits(string number)
    {
        var mantissa = number.Trim().TrimStart('+', '-').Split('e', 'E')[0];
        var digits = mantissa.Replace(".", "", StringComparison.Ordinal).TrimStart('0');
        return Math.Max(1, digits.Length);
    }

    /// <summary>Light-years, with the source's significant digits. A value already in light-years is kept.</summary>
    public static double? ToLightYears(string amount, string unitId)
    {
        var text = amount.Trim().TrimStart('+');
        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return null;

        decimal? factor = unitId switch
        {
            LightYear => 1m,
            Parsec => LightYearsPerParsec,
            Kiloparsec => LightYearsPerParsec * 1_000m,
            Megaparsec => LightYearsPerParsec * 1_000_000m,
            _ => null,
        };
        if (factor is null) return null;
        if (unitId == LightYear) return (double)value;
        return (double)RoundToSignificant(value * factor.Value, SignificantDigits(text));
    }

    public static DistancePick Pick(IReadOnlyList<DistanceClaim> claims)
    {
        var usable = claims
            .Where(c => c.Rank != "deprecated" && ToLightYears(c.Amount, c.UnitId) is not null)
            .ToList();
        if (usable.Count == 0)
            return new(null, null, claims.Count == 0
                ? "Wikidata has no distance"
                : "Wikidata has no usable distance (all deprecated, or an unknown unit)");

        var preferred = usable.Where(c => c.Rank == "preferred").ToList();
        var candidates = preferred.Count > 0 ? preferred : usable;
        if (candidates.Count == 1) return Picked(candidates[0]);

        var published = candidates.Where(c => c.CitesPublishedWork).ToList();
        if (published.Count == 1) return Picked(published[0]);

        return new(null, null, published.Count > 1
            ? $"{published.Count} values cite a published work (D28)"
            : $"{candidates.Count} values, none cites a published work (D28)");
    }

    private static DistancePick Picked(DistanceClaim c) => new(ToLightYears(c.Amount, c.UnitId), c, null);

    private static decimal RoundToSignificant(decimal x, int digits)
    {
        if (x == 0) return 0;
        var abs = Math.Abs(x);
        var exponent = 0; // x = m × 10^exponent, 1 <= m < 10
        while (abs >= 10) { abs /= 10; exponent++; }
        while (abs < 1) { abs *= 10; exponent--; }

        var decimals = digits - 1 - exponent;
        if (decimals >= 0) return Math.Round(x, Math.Min(decimals, 28), MidpointRounding.AwayFromZero);

        decimal scale = 1;
        for (var i = 0; i < -decimals; i++) scale *= 10;
        return Math.Round(x / scale, MidpointRounding.AwayFromZero) * scale;
    }
}
