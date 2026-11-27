namespace BGAPI.Cosmic.Importer;

/// <summary>One object in the brief's shape, without images (X01 D25).</summary>
public sealed record CosmicObject
{
    public required string Slug { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<string>? Aliases { get; init; }
    public string? Category { get; init; }
    public string? Subcategory { get; init; }
    public string? Constellation { get; init; }
    public string? Ra { get; init; }
    public string? Dec { get; init; }
    public double? DistanceLy { get; init; }
    public double? Magnitude { get; init; }
    public string? Discovered { get; init; }
    public IReadOnlyList<Fact>? Facts { get; init; }
    public IReadOnlyList<Source>? Sources { get; init; }
}

public sealed record Fact(string Key, string Label, string Value, string? Unit);

public sealed record Source(string Catalog, string CatalogId, string License, string Url);

/// <summary>One OpenNGC row, the columns stage 2 reads. Values as the file writes them.</summary>
public sealed record OpenNgcRow
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Ra { get; init; }
    public string? Dec { get; init; }
    public string? Const { get; init; }
    public string? MajAx { get; init; }
    public string? MinAx { get; init; }
    public string? VMag { get; init; }
    public string? Hubble { get; init; }
    public string? RadVel { get; init; }
    public int? Messier { get; init; }
    public IReadOnlyList<string> CommonNames { get; init; } = [];
}

/// <summary>One Wikidata entity, the claims stage 2 reads.</summary>
public sealed record WikidataEntity
{
    public required string Id { get; init; }
    public string? Label { get; init; }
    public int? Messier { get; init; }
    public IReadOnlyList<string> CatalogCodes { get; init; } = [];
    public IReadOnlyList<DistanceClaim> Distances { get; init; } = [];
    public WikidataTime? Discovered { get; init; }
    public double? RaDeg { get; init; }
    public double? DecDeg { get; init; }
    public IReadOnlyList<double> Magnitudes { get; init; } = [];
    public IReadOnlyList<string> DiscovererIds { get; init; } = [];
}

/// <summary>One P2583 claim. <paramref name="CitesPublishedWork"/>: a reference has "stated in" (P248).</summary>
public sealed record DistanceClaim(string Amount, string UnitId, string Rank, bool CitesPublishedWork);

public sealed record WikidataTime(string Time, int Precision, string CalendarId);
