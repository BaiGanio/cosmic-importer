using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

public enum FormKind { MSpaced, MCompact, MessierN, Ngc, Ic, Name }

public sealed record SearchForm(FormKind Kind, string Term);

/// <summary>
/// X01 D47: every name and alias of the object, as written, plus "M31" and "Messier 31" (D48),
/// which no source writes but which search better on some sources. A search word is never stored.
/// </summary>
public static partial class SearchForms
{
    [GeneratedRegex(@"^messier-(\d+)$")]
    private static partial Regex Slug();

    [GeneratedRegex(@"^M \d+$")] private static partial Regex MSpaced();
    [GeneratedRegex(@"^M\d+$")] private static partial Regex MCompact();
    [GeneratedRegex(@"^Messier \d+$")] private static partial Regex MessierN();
    [GeneratedRegex(@"^NGC ?\d+$")] private static partial Regex Ngc();
    [GeneratedRegex(@"^IC ?\d+$")] private static partial Regex Ic();

    public static IReadOnlyList<SearchForm> For(CosmicObject o)
    {
        var match = Slug().Match(o.Slug);
        if (!match.Success) throw new InvalidOperationException($"Not a Messier slug: {o.Slug}");
        var n = match.Groups[1].Value;
        return new[] { o.Name }.Concat(o.Aliases ?? []).Concat([$"M{n}", $"Messier {n}"])
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Select(t => new SearchForm(Kind(t), t))
            .ToList();
    }

    /// <summary>
    /// X01 D48, measured on 6 objects (search-measure.md): "M 31" is dropped everywhere (NASA: only junk;
    /// the other sources: the same answers as "M31"). NGC is dropped on NASA (almost no hits, none named).
    /// </summary>
    public static IReadOnlyList<SearchForm> ForSource(CosmicObject o, string source) =>
        For(o).Where(f => f.Kind != FormKind.MSpaced && !(source == "nasa" && f.Kind == FormKind.Ngc)).ToList();

    public static FormKind Kind(string term) =>
        MSpaced().IsMatch(term) ? FormKind.MSpaced
        : MCompact().IsMatch(term) ? FormKind.MCompact
        : MessierN().IsMatch(term) ? FormKind.MessierN
        : Ngc().IsMatch(term) ? FormKind.Ngc
        : Ic().IsMatch(term) ? FormKind.Ic
        : FormKind.Name;
}
