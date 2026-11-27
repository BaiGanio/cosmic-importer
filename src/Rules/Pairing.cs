using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>
/// X01 D46: a hit "names" the object when its text holds one of the object's terms as a whole word,
/// ignoring only spaces and case ("M31" = "m 31"; "M310" and "NGC 2244" are not "M31" and "NGC 224").
/// </summary>
public static partial class Pairing
{
    [GeneratedRegex(@"\p{L}+|\d+|[^\s\p{L}\d]")]
    private static partial Regex Chunk();

    public static bool Names(string? text, IEnumerable<string> terms) =>
        !string.IsNullOrEmpty(text) && terms.Any(t => PatternFor(t).IsMatch(text));

    private static Regex PatternFor(string term)
    {
        var body = string.Join(@"\s*", Chunk().Matches(term).Select(m => Regex.Escape(m.Value)));
        return new Regex($@"(?<![\p{{L}}\d]){body}(?![\p{{L}}\d])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
