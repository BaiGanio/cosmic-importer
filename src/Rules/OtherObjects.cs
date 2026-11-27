namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>
/// X01 D59: an image whose page "Name:" field names only other objects (N83B found by the search "Orion Nebula")
/// is a real image of another object. The list keeps the names as the page writes them, for later sets.
/// </summary>
public static class OtherObjects
{
    public sealed record Find(string Source, string SourceId, string Title, IReadOnlyList<string> NamesOnPage,
        string? Credit, string SourcePage, string RulesSay, string Slug, string SearchTerm);

    public sealed record Entry(IReadOnlyList<string> NamesOnPage, string Source, string SourceId, string Title,
        string? Credit, string SourcePage, string RulesSay, IReadOnlyList<string> FoundWhileSearching);

    public static bool IsOther(IReadOnlyList<string> namesOnPage, IReadOnlyList<string> terms) =>
        namesOnPage.Count > 0 && !namesOnPage.Any(n => Pairing.Names(n, terms));

    public static IReadOnlyList<Entry> Merge(IEnumerable<Find> finds) =>
        finds.GroupBy(f => (f.Source, f.SourceId))
            .Select(g =>
            {
                var f = g.First();
                return new Entry(f.NamesOnPage, f.Source, f.SourceId, f.Title, f.Credit, f.SourcePage, f.RulesSay,
                    g.Select(x => $"{x.Slug}: {x.SearchTerm}").Distinct().ToList());
            })
            .OrderBy(e => e.NamesOnPage[0], StringComparer.Ordinal)
            .ThenBy(e => e.SourceId, StringComparer.Ordinal)
            .ToList();
}
