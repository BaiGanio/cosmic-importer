using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BGAPI.Cosmic.Importer.Rules;

namespace BGAPI.Cosmic.Importer;

public sealed record ObjectRun(int Messier, OpenNgcRow? Row, WikidataEntity Entity, CosmicObject Final, DistancePick Distance);

/// <summary>docs/report.md (X01 D26, D30): open edge cases, gaps, disagreements.</summary>
public static partial class Report
{
    private const double MaxPositionArcsec = 60;
    private const double MaxMagnitudeDifference = 0.5;

    [GeneratedRegex(@"^(NGC|IC)0*(\d+)$")]
    private static partial Regex OpenNgcId();

    [GeneratedRegex(@"^(NGC|IC) ?0*(\d+)$")]
    private static partial Regex WikidataId();

    public static string Build(IReadOnlyList<ObjectRun> runs, IReadOnlyList<EdgeCase> cases, IReadOnlyList<int> noWikidata, string runDate)
    {
        var written = runs.Where(r => r.Final.Category is not null).ToList();
        var leftOut = runs.Where(r => r.Final.Category is null).ToList();
        var gaps = runs.SelectMany(Gaps).ToList();
        var disagreements = runs.SelectMany(Disagreements).ToList();
        var open = cases.Where(c => c.Status == "open").ToList();
        var withDiscoverer = runs.Count(r => r.Entity.DiscovererIds.Count > 0);

        var sb = new StringBuilder();
        sb.AppendLine("# Cosmic: extract report (X01 step 9, stage 2)");
        sb.AppendLine();
        sb.AppendLine("Written by the importer (`dotnet run --project src`). Do not edit by hand: the next run writes it again (D30).");
        sb.AppendLine($"Run on {runDate}. Sources: OpenNGC `v20260501`, Wikidata entities of 2026-11-24 (`INVENTORY.md`). Edge cases: `edge-cases.json`.");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Objects written to `out/messier.json`: **{written.Count} of 110**");
        sb.AppendLine($"- Left out (no category): {leftOut.Count}");
        if (noWikidata.Count > 0) sb.AppendLine($"- No Wikidata file: {string.Join(", ", noWikidata.Select(m => $"M{m}"))}");
        sb.AppendLine($"- Open edge cases: {open.Count} of {cases.Count}");
        sb.AppendLine($"- Gaps (empty fields): {gaps.Count}");
        sb.AppendLine($"- Disagreements (the owner's value stays, D27): {disagreements.Count}");
        sb.AppendLine($"- Discoverer: no object has it yet. Wikidata gives only Q-ids ({withDiscoverer} of 110 have P61); the names need a stage 1 download (D34)");
        sb.AppendLine("- Not compared: constellation and category. Wikidata gives Q-ids (P59, P31) whose names are not on disk");
        sb.AppendLine();

        sb.AppendLine("## Open edge cases (D26)");
        sb.AppendLine();
        sb.AppendLine("Applied in this run. A human checks each one and sets `verified`.");
        sb.AppendLine();
        sb.AppendLine("| Id | Object | Field | Value used | Reason |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var c in open)
            sb.AppendLine($"| {c.Id} | {c.Slug} | {c.Field} | {Cell(c.Value.ValueKind == JsonValueKind.Null ? "(empty)" : c.Value.ToString())} | {Cell(c.Reason)} |");
        sb.AppendLine();

        if (leftOut.Count > 0)
        {
            sb.AppendLine("## Left out");
            sb.AppendLine();
            sb.AppendLine("| Object | Why |");
            sb.AppendLine("|---|---|");
            foreach (var r in leftOut)
                sb.AppendLine($"| {r.Final.Slug} | {(r.Row is null ? "no OpenNGC row" : $"OpenNGC type `{r.Row.Type}` has no category and no edge case (D31)")} |");
            sb.AppendLine();
        }

        sb.AppendLine("## Gaps");
        sb.AppendLine();
        sb.AppendLine("Empty fields. Next year: check whether a source has the value now (D30).");
        sb.AppendLine();
        sb.AppendLine("| Object | Field | Why |");
        sb.AppendLine("|---|---|---|");
        foreach (var g in gaps) sb.AppendLine($"| {g.Slug} | {g.Field} | {Cell(g.Why)} |");
        sb.AppendLine();

        sb.AppendLine("## Disagreements");
        sb.AppendLine();
        sb.AppendLine($"The owner's value stays (D27). Shown: name differs; position more than {MaxPositionArcsec:0}″ apart; no Wikidata magnitude within {MaxMagnitudeDifference.ToString(CultureInfo.InvariantCulture)} of OpenNGC's V; NGC/IC number differs.");
        sb.AppendLine();
        sb.AppendLine("| Object | Field | Owner | Other |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var d in disagreements) sb.AppendLine($"| {d.Slug} | {d.Field} | {Cell(d.Owner)} | {Cell(d.Other)} |");

        return sb.ToString();
    }

    private static IEnumerable<(string Slug, string Field, string Why)> Gaps(ObjectRun r)
    {
        var o = r.Final;
        var s = o.Slug;
        var row = r.Row;
        if (row is null) yield return (s, "(all OpenNGC fields)", "no OpenNGC row");
        if (row is not null && o.Subcategory is null)
            yield return (s, "subcategory", $"OpenNGC type `{row.Type}`, Hubble `{row.Hubble ?? "-"}`: no mapping (D32)");
        if (row is not null && o.Constellation is null) yield return (s, "constellation", $"OpenNGC code `{row.Const ?? "-"}` unknown");
        if (row is not null && o.Ra is null) yield return (s, "ra", $"OpenNGC RA `{row.Ra ?? "-"}` not readable");
        if (row is not null && o.Dec is null) yield return (s, "dec", $"OpenNGC Dec `{row.Dec ?? "-"}` not readable");
        if (row is not null && o.Magnitude is null) yield return (s, "magnitude", "OpenNGC has no V-Mag (D27: no gap filling)");
        if (o.DistanceLy is null) yield return (s, "distanceLy", r.Distance.WhyEmpty ?? "emptied by an edge case");
        if (o.Discovered is null)
            yield return (s, "discovered", r.Entity.Discovered is null ? "Wikidata has no P575" : $"Wikidata P575 `{r.Entity.Discovered.Time}` precision {r.Entity.Discovered.Precision}: not a year, month or day");
        if (row is not null && string.IsNullOrWhiteSpace(row.MajAx)) yield return (s, "facts.apparentSize", "OpenNGC has no MajAx");
        if (row is not null && string.IsNullOrWhiteSpace(row.RadVel)) yield return (s, "facts.radialVelocity", "OpenNGC has no RadVel");
        if (r.Entity.DiscovererIds.Count == 0) yield return (s, "facts.discoverer", "Wikidata has no P61");
    }

    private static IEnumerable<(string Slug, string Field, string Owner, string Other)> Disagreements(ObjectRun r)
    {
        var row = r.Row;
        var e = r.Entity;
        var s = r.Final.Slug;
        if (row is null) yield break;

        if (Names.IsRealName(e.Label) && e.Label != r.Final.Name && !row.CommonNames.Contains(e.Label!))
            yield return (s, "name", $"OpenNGC: {r.Final.Name}", $"Wikidata: {e.Label}");

        var ra = Sexagesimal.RaDegrees(row.Ra);
        var dec = Sexagesimal.DecDegrees(row.Dec);
        if (ra is not null && dec is not null && e.RaDeg is not null && e.DecDeg is not null)
        {
            var dRa = (e.RaDeg.Value - ra.Value) * Math.Cos(dec.Value * Math.PI / 180) * 3600;
            var dDec = (e.DecDeg.Value - dec.Value) * 3600;
            var apart = Math.Sqrt(dRa * dRa + dDec * dDec);
            if (apart > MaxPositionArcsec)
                yield return (s, "ra/dec", $"OpenNGC: {r.Final.Ra}, {r.Final.Dec}",
                    $"Wikidata: {N(e.RaDeg.Value)}°, {N(e.DecDeg.Value)}° ({apart.ToString("0", CultureInfo.InvariantCulture)}″ apart)");
        }

        if (r.Final.Magnitude is { } v && e.Magnitudes.Count > 0 && e.Magnitudes.All(w => Math.Abs(w - v) > MaxMagnitudeDifference))
            yield return (s, "magnitude", $"OpenNGC V: {N(v)}", $"Wikidata: {string.Join(", ", e.Magnitudes.Select(N))}");

        var id = OpenNgcId().Match(row.Name);
        if (id.Success)
        {
            var same = e.CatalogCodes.Select(c => WikidataId().Match(c)).Where(m => m.Success && m.Groups[1].Value == id.Groups[1].Value).ToList();
            if (same.Count > 0 && same.All(m => m.Groups[2].Value != id.Groups[2].Value))
                yield return (s, $"{id.Groups[1].Value} number", $"OpenNGC row: {row.Name}", $"Wikidata: {string.Join(", ", same.Select(m => m.Value))}");
        }
    }

    private static string N(double d) => d.ToString("0.#####", CultureInfo.InvariantCulture);

    private static string Cell(string? text) => (text ?? "").Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}
