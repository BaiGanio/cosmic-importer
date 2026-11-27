using System.Text.RegularExpressions;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>X01 D27 (name) and D36 (aliases, as the sources write them).</summary>
public static partial class Names
{
    [GeneratedRegex(@"^Messier \d+$")]
    private static partial Regex MessierLabel();

    [GeneratedRegex(@"^(M|NGC|IC) ?\d+$")]
    private static partial Regex MainCode();

    public static bool IsRealName(string? label) =>
        !string.IsNullOrWhiteSpace(label) && !MessierLabel().IsMatch(label);

    public static string Name(int messier, IReadOnlyList<string> commonNames, string? label) =>
        commonNames.Count > 0 ? commonNames[0]
        : IsRealName(label) ? label!
        : $"Messier {messier}";

    public static IReadOnlyList<string> Aliases(
        string name, IReadOnlyList<string> catalogCodes, IReadOnlyList<string> commonNames, string? label) =>
        catalogCodes.Where(c => MainCode().IsMatch(c))
            .Concat(commonNames)
            .Concat(label is null ? [] : [label])
            .Where(a => a != name)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
