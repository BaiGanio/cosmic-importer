using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 stage 3: one hit becomes a candidate, or a refusal with its reason (D46, D50, D51, D53, D54).</summary>
public class ImageCandidatesTests
{
    private static readonly string[] Allowed = ["images-assets.nasa.gov", "cdn.esahubble.org"];
    private static readonly string[] Terms = ["Great Orion Nebula", "M 42", "NGC 1976", "Orion Nebula", "M42", "Messier 42"];

    private static SearchHit EsaHit(string title = "Hubble Probes the Great Orion Nebula") =>
        new("opo9410a", title, [], "https://cdn.esahubble.org/archives/images/thumb300y/opo9410a.jpg", "/images/opo9410a/")
        { Links = ["https://cdn.esahubble.org/archives/images/thumb300y/opo9410a.jpg"] };

    private static DjangoplicityPage Page(string? credit = "NASA/ESA", string? type = "Observation", string[]? names = null) =>
        new(credit, type, names ?? ["M 42"], [
            new("original", "/media/archives/images/original/opo9410a.tif"),
            new("screen", "https://cdn.esahubble.org/archives/images/screen/opo9410a.jpg")]);

    private static ImageCandidates.Result Esa(SearchHit hit, DjangoplicityPage? page) =>
        ImageCandidates.FromDjangoplicity("messier-42", "esahubble", "https://esahubble.org", hit, page, Terms, Allowed);

    [Fact]
    public void A_good_page_is_a_candidate_with_its_allowed_sizes_only()
    {
        var c = Esa(EsaHit(), Page()).Candidate!;
        Assert.Equal("opo9410a", c.SourceId);
        Assert.Equal("NASA/ESA", c.Credit);
        Assert.Equal("CC-BY-4.0", c.License);
        Assert.Equal("https://esahubble.org/images/opo9410a/", c.SourcePage);
        Assert.Equal(["thumb300y", "screen"], c.Sizes.Select(s => s.Name));
        Assert.True(c.Named);
    }

    [Fact]
    public void Named_comes_from_the_page_name_field_when_the_title_does_not_name_it() =>
        Assert.True(Esa(EsaHit("A beautiful sky"), Page(names: ["Messier 42"])).Candidate!.Named);

    [Fact]
    public void Not_named_is_still_a_candidate_D46()
    {
        var c = Esa(EsaHit("A beautiful sky"), Page(names: ["NGC 1977"])).Candidate!;
        Assert.False(c.Named);
    }

    [Fact]
    public void A_good_page_gets_its_url_and_thumb_by_the_size_rule()
    {
        var c = Esa(EsaHit(), Page()).Candidate!;
        Assert.Equal("https://cdn.esahubble.org/archives/images/screen/opo9410a.jpg", c.Url);
        Assert.Equal("https://cdn.esahubble.org/archives/images/thumb300y/opo9410a.jpg", c.ThumbUrl);
    }

    [Fact]
    public void A_dropped_image_keeps_its_thumbnail_page_and_reason_for_the_quick_look()
    {
        var d = Esa(EsaHit(), Page(type: "Collage")).Dropped!;
        Assert.Equal("rule", d.By);
        Assert.Equal("not a real picture (type Collage)", d.Reason);
        Assert.Equal("https://cdn.esahubble.org/archives/images/thumb300y/opo9410a.jpg", d.ThumbUrl);
        Assert.Equal("https://esahubble.org/images/opo9410a/", d.SourcePage);
    }

    [Fact]
    public void A_page_with_only_a_thumbnail_is_refused()
    {
        var page = new DjangoplicityPage("NASA", "Observation", [], []);
        Assert.Equal("no web size or no thumbnail on an allowed host", Esa(EsaHit(), page).Refused);
    }

    [Fact]
    public void No_page_on_disk_is_refused() => Assert.Equal("page not fetched", Esa(EsaHit(), null).Refused);

    [Fact]
    public void No_credit_is_refused() => Assert.Equal("missing credit", Esa(EsaHit(), Page(credit: null)).Refused);

    [Fact]
    public void Artwork_is_refused() => Assert.Equal("not a real picture (type Artwork)", Esa(EsaHit(), Page(type: "Artwork")).Refused);

    [Fact]
    public void A_credit_longer_than_500_is_refused() =>
        Assert.Equal("credit longer than 500 characters", Esa(EsaHit(), Page(credit: new string('x', 501))).Refused);

    [Fact]
    public void No_size_on_an_allowed_host_is_refused()
    {
        var page = new DjangoplicityPage("NASA", "Observation", [], [new("original", "/media/x.tif")]);
        var r = ImageCandidates.FromDjangoplicity("messier-42", "esahubble", "https://esahubble.org",
            new SearchHit("x", "t", [], null, "/images/x/"), page, Terms, Allowed);
        Assert.Equal("no image on an allowed host", r.Refused);
    }

    private static SearchHit NasaHit(string title = "Orion Nebula and Bow Shock", string[]? keywords = null) =>
        new("PIA04227", title, keywords ?? [], "https://images-assets.nasa.gov/image/PIA04227/PIA04227~thumb.jpg", null)
        { Links = ["https://images-assets.nasa.gov/image/PIA04227/PIA04227~medium.jpg", "https://images-assets.nasa.gov/image/PIA04227/PIA04227~thumb.jpg"] };

    private static NasaInfo Info(string? refused = null, string text = "Orion Nebula and Bow Shock") =>
        new("NASA and the Hubble Heritage Team STScI/AURA", "credit", refused is null ? "public-domain" : null, refused, text);

    [Fact]
    public void A_good_nasa_item_is_a_candidate()
    {
        var c = ImageCandidates.FromNasa("messier-42", NasaHit(), Info(), Terms, Allowed).Candidate!;
        Assert.Equal("https://images.nasa.gov/details/PIA04227", c.SourcePage);
        Assert.Equal("public-domain", c.License);
        Assert.Equal("credit", c.CreditFrom);
        Assert.Equal(["medium", "thumb"], c.Sizes.Select(s => s.Name));
        Assert.True(c.Named);
    }

    [Fact]
    public void A_nasa_refusal_keeps_its_reason() =>
        Assert.Equal("third-party copyright", ImageCandidates.FromNasa("messier-42", NasaHit(), Info("third-party copyright"), Terms, Allowed).Refused);

    [Fact]
    public void A_nasa_artist_concept_is_refused() =>
        Assert.Equal("not a real picture (NASA text)", ImageCandidates.FromNasa("messier-42", NasaHit(),
            Info(text: "Artist's concept of the Orion Nebula"), Terms, Allowed).Refused);

    [Fact]
    public void No_nasa_metadata_on_disk_is_refused() =>
        Assert.Equal("metadata not fetched", ImageCandidates.FromNasa("messier-42", NasaHit(), null, Terms, Allowed).Refused);
}
