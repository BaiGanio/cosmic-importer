using System.Globalization;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>X01 D27: one owner per field, no gap filling. D34 facts, D35 slug and sources.</summary>
public static class Merge
{
    public static string Slug(int messier) => $"messier-{messier}";

    public static Source OpenNgcSource(string name) =>
        new("OpenNGC", name, "CC-BY-SA-4.0", "https://github.com/mattiaverga/OpenNGC");

    public static Source WikidataSource(string id) =>
        new("Wikidata", id, "CC0-1.0", $"https://www.wikidata.org/wiki/{id}");

    /// <summary>What OpenNGC alone says (out/openngc.json).</summary>
    public static CosmicObject FromOpenNgc(int messier, OpenNgcRow row) => new()
    {
        Slug = Slug(messier),
        Name = row.CommonNames.Count > 0 ? row.CommonNames[0] : null,
        Aliases = NullIfEmpty(row.CommonNames.Skip(1).ToList()),
        Category = TypeMap.Category(row.Type),
        Subcategory = TypeMap.Subcategory(row.Type, row.Hubble),
        Constellation = Constellations.FullName(row.Const),
        Ra = Sexagesimal.FormatRa(row.Ra),
        Dec = Sexagesimal.FormatDec(row.Dec),
        Magnitude = ParseDouble(row.VMag),
        Facts = NullIfEmpty(Facts(row)),
        Sources = [OpenNgcSource(row.Name)],
    };

    /// <summary>What Wikidata alone says (out/wikidata.json).</summary>
    public static CosmicObject FromWikidata(int messier, WikidataEntity entity) => new()
    {
        Slug = Slug(messier),
        Name = Names.IsRealName(entity.Label) ? entity.Label : null,
        Aliases = NullIfEmpty(Names.Aliases("", entity.CatalogCodes, [], null)),
        DistanceLy = Distance.Pick(entity.Distances).LightYears,
        Discovered = Discovered.Format(entity.Discovered),
        Sources = [WikidataSource(entity.Id)],
    };

    /// <summary>The merged object (out/messier.json), before edge cases.</summary>
    public static CosmicObject Merged(int messier, OpenNgcRow? row, WikidataEntity entity)
    {
        var o = row is null ? null : FromOpenNgc(messier, row);
        var w = FromWikidata(messier, entity);
        IReadOnlyList<string> commonNames = row?.CommonNames ?? [];
        var name = Names.Name(messier, commonNames, entity.Label);

        var sources = new List<Source>();
        if (o?.Sources is not null) sources.AddRange(o.Sources);
        if (w.Sources is not null) sources.AddRange(w.Sources);

        return new()
        {
            Slug = Slug(messier),
            Name = name,
            Aliases = NullIfEmpty(Names.Aliases(name, entity.CatalogCodes, commonNames, entity.Label)),
            Category = o?.Category,
            Subcategory = o?.Subcategory,
            Constellation = o?.Constellation,
            Ra = o?.Ra,
            Dec = o?.Dec,
            Magnitude = o?.Magnitude,
            DistanceLy = w.DistanceLy,
            Discovered = w.Discovered,
            Facts = o?.Facts,
            Sources = sources,
        };
    }

    private static List<Fact> Facts(OpenNgcRow row)
    {
        var facts = new List<Fact>();
        if (!string.IsNullOrWhiteSpace(row.MajAx))
            facts.Add(new("apparentSize", "Apparent size",
                string.IsNullOrWhiteSpace(row.MinAx) ? row.MajAx : $"{row.MajAx} × {row.MinAx}", "arcmin"));
        if (!string.IsNullOrWhiteSpace(row.RadVel))
            facts.Add(new("radialVelocity", "Radial velocity", row.RadVel, "km/s"));
        return facts;
    }

    private static double? ParseDouble(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static IReadOnlyList<T>? NullIfEmpty<T>(IReadOnlyList<T> list) => list.Count == 0 ? null : list;
}
