namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>One image in the brief's shape.</summary>
public sealed record ImportImage(string Url, string ThumbUrl, string Credit, string License, string SourcePage);

/// <summary>One object in the brief's shape, with its images: what POST /api/cosmic/import takes (X01 stage 5).</summary>
public sealed record ImportObject
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
    public required IReadOnlyList<ImportImage> Images { get; init; }
    public IReadOnlyList<Source>? Sources { get; init; }
}

/// <summary>
/// X01 stage 5: joins the facts (out/messier.json) with the images (out/image-candidates.json) and the
/// human choices (docs/picks.json). The images of an object = kept - drop + keep (D57),
/// in the kept order, the brought-back ones last. Every image goes in, no limit (D60).
/// An object with no image is not sent (the brief rejects it) and is listed in Skipped.
/// </summary>
public static class ImportPayload
{
    public const int MaxBatch = 1000;

    public sealed record Result(IReadOnlyList<ImportObject> Objects, IReadOnlyList<string> Skipped);

    public static Result Build(IReadOnlyList<CosmicObject> facts, BuildCandidates.CandidatesFile candidates,
        IReadOnlyDictionary<string, ReviewServer.Choices> picks)
    {
        var bySlug = facts.ToDictionary(f => f.Slug, StringComparer.Ordinal);
        var objects = new List<ImportObject>();
        var skipped = new List<string>();
        foreach (var o in candidates.Objects)
        {
            if (!bySlug.TryGetValue(o.Slug, out var f)) { skipped.Add($"{o.Slug}: not in messier.json"); continue; }
            picks.TryGetValue(o.Slug, out var choice);
            var drop = (choice?.Drop ?? []).Select(p => (p.Source, p.Id)).ToHashSet();
            var keep = (choice?.Keep ?? []).Select(p => (p.Source, p.Id)).ToHashSet();

            var images = o.Candidates
                .Where(c => !drop.Contains((c.Source, c.SourceId)) && c.Url is not null && c.ThumbUrl is not null)
                .Select(c => new ImportImage(c.Url!, c.ThumbUrl!, c.Credit, c.License, c.SourcePage))
                .ToList();
            foreach (var d in o.Dropped.Where(d => keep.Contains((d.Source, d.SourceId))))
            {
                // A dropped image carries no license, so a brought-back image cannot be sent yet.
                skipped.Add($"{o.Slug}: brought-back image {d.Source} {d.SourceId} not sent (a dropped image has no license)");
            }

            if (images.Count == 0) { skipped.Add($"{o.Slug}: no image"); continue; }
            objects.Add(new ImportObject
            {
                Slug = f.Slug, Name = f.Name, Aliases = f.Aliases, Category = f.Category, Subcategory = f.Subcategory,
                Constellation = f.Constellation, Ra = f.Ra, Dec = f.Dec, DistanceLy = f.DistanceLy,
                Magnitude = f.Magnitude, Discovered = f.Discovered, Facts = f.Facts, Images = images, Sources = f.Sources,
            });
        }
        return new Result(objects, skipped);
    }

    /// <summary>Batches of at most <see cref="MaxBatch"/> objects (D9: more is a 400).</summary>
    public static IReadOnlyList<IReadOnlyList<ImportObject>> Batches(IReadOnlyList<ImportObject> objects) =>
        objects.Chunk(MaxBatch).Select(c => (IReadOnlyList<ImportObject>)c).ToList();
}
