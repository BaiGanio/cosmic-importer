using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 stage 5: the import body = facts + images (kept - drop + keep, D57), all images (D60).</summary>
public class ImportPayloadTests
{
    private static CosmicObject Fact(string slug) => new()
    {
        Slug = slug, Name = "Andromeda Galaxy", Category = "galaxy", Magnitude = 3.44,
        Sources = [new("OpenNGC", "NGC0224", "CC-BY-SA-4.0", "https://github.com/mattiaverga/OpenNGC")],
    };

    private static ImageCandidate C(string id, string? url = "https://u/") =>
        new("messier-31", "esahubble", id, "t", "Observation", true, "credit " + id, "credit", "CC-BY-4.0",
            "https://esahubble.org/images/" + id + "/", [], url is null ? null : url + id, "https://t/" + id);

    private static DroppedImage D(string id) =>
        new("messier-31", "esahubble", id, "t", "Collage", "rule", "not a real picture", "c", "https://p", [], "https://u", "https://t");

    private static BuildCandidates.CandidatesFile File(IReadOnlyList<ImageCandidate> kept, IReadOnlyList<DroppedImage>? dropped = null, string slug = "messier-31") =>
        new([new BuildCandidates.ObjectCandidates(slug, "Andromeda Galaxy", kept, dropped ?? [])]);

    private static readonly Dictionary<string, ReviewServer.Choices> NoPicks = new();

    [Fact]
    public void Every_kept_image_goes_in_in_the_kept_order_with_the_facts()
    {
        var r = ImportPayload.Build([Fact("messier-31")], File([C("a"), C("b"), C("c")]), NoPicks);
        var o = Assert.Single(r.Objects);
        Assert.Equal(["a", "b", "c"], o.Images.Select(i => i.Credit.Split(' ')[1]));
        Assert.Equal(("Andromeda Galaxy", "galaxy", 3.44), (o.Name, o.Category, o.Magnitude));
        Assert.Equal(new ImportImage("https://u/a", "https://t/a", "credit a", "CC-BY-4.0", "https://esahubble.org/images/a/"), o.Images[0]);
        Assert.Empty(r.Skipped);
    }

    [Fact]
    public void A_human_drop_takes_the_image_out()
    {
        var picks = new Dictionary<string, ReviewServer.Choices> { ["messier-31"] = new([new("esahubble", "b")], []) };
        var o = Assert.Single(ImportPayload.Build([Fact("messier-31")], File([C("a"), C("b")]), picks).Objects);
        Assert.Equal(["https://u/a"], o.Images.Select(i => i.Url));
    }

    [Fact]
    public void A_brought_back_image_is_listed_not_sent_because_it_has_no_license()
    {
        var picks = new Dictionary<string, ReviewServer.Choices> { ["messier-31"] = new([], [new("esahubble", "x")]) };
        var r = ImportPayload.Build([Fact("messier-31")], File([C("a")], [D("x")]), picks);
        Assert.Single(Assert.Single(r.Objects).Images);
        Assert.Contains(r.Skipped, s => s.Contains("esahubble x"));
    }

    [Fact]
    public void An_object_with_no_image_left_is_not_sent()
    {
        var picks = new Dictionary<string, ReviewServer.Choices> { ["messier-31"] = new([new("esahubble", "a")], []) };
        var r = ImportPayload.Build([Fact("messier-31")], File([C("a")]), picks);
        Assert.Empty(r.Objects);
        Assert.Equal(["messier-31: no image"], r.Skipped);
    }

    [Fact]
    public void An_image_without_a_web_size_is_not_sent() =>
        Assert.Equal(["https://u/a"], Assert.Single(ImportPayload.Build([Fact("messier-31")], File([C("a"), C("b", url: null)]), NoPicks).Objects).Images.Select(i => i.Url));

    [Fact]
    public void An_object_not_in_the_facts_is_listed() =>
        Assert.Equal(["messier-99: not in messier.json"], ImportPayload.Build([Fact("messier-31")], File([C("a")], slug: "messier-99"), NoPicks).Skipped);

    [Fact]
    public void Batches_hold_at_most_1000()
    {
        var many = Enumerable.Range(0, 2001).Select(i => new ImportObject { Slug = "s" + i, Images = [] }).ToList();
        Assert.Equal([1000, 1000, 1], ImportPayload.Batches(many).Select(b => b.Count));
    }
}
