using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D57: ranking rules move an image up or down; they never throw one away. D57 sizes: one fixed rule.</summary>
public class RankingTests
{
    private static ImageCandidate C(string source = "esahubble", string id = "opo9410a", string? type = "Observation", bool named = true) =>
        new("messier-42", source, id, "t", type, named, "credit", "credit", "CC-BY-4.0", "https://x", [], null, null);

    [Fact]
    public void Named_observation_main_image_from_a_curated_source_has_every_reason()
    {
        var (score, reasons) = Ranking.Rank(C());
        Assert.Equal(27, score);
        Assert.Equal(["named", "Observation", "main image", "ESA/ESO/NOIRLab"], reasons);
    }

    [Fact]
    public void Every_named_image_ranks_above_every_image_that_does_not_name_the_object()
    {
        var weakestNamed = Ranking.Rank(C(source: "nasa", id: "PIA1", type: null)).Score;
        var strongestNotNamed = Ranking.Rank(C(named: false)).Score;
        Assert.True(weakestNamed > strongestNotNamed);
    }

    [Fact]
    public void Named_counts_most() =>
        Assert.True(Ranking.Rank(C(type: null, source: "nasa", id: "PIA04227")).Score > Ranking.Rank(C(named: false)).Score);

    [Fact]
    public void Photographic_counts_less_than_observation() =>
        Assert.True(Ranking.Rank(C(type: "Photographic")).Score < Ranking.Rank(C()).Score);

    [Theory]
    [InlineData("heic1502a", true)]
    [InlineData("noirlab2020a", true)]
    [InlineData("eso0136a", true)]
    [InlineData("heic1502b", false)]
    [InlineData("noao-m31lgs_ubvIha", false)]
    [InlineData("gerd_huedepohl_8", false)]
    public void A_main_image_is_the_first_image_of_a_release(string id, bool main) =>
        Assert.Equal(main, Ranking.IsMainImage(id));

    [Fact]
    public void Nasa_ids_are_never_main_images() =>
        Assert.DoesNotContain("main image", Ranking.Rank(C(source: "nasa", id: "PIA04921a", type: null)).Reasons);

    [Fact]
    public void Order_is_score_then_source_then_id()
    {
        var list = new[] { C(id: "b1", named: false), C(source: "nasa", id: "z", type: null), C(id: "a1") };
        Assert.Equal(["a1", "z", "b1"], Ranking.Order(list).Select(c => c.SourceId));
    }

    [Fact]
    public void Djangoplicity_sizes_screen_and_thumb300y()
    {
        var (url, thumb) = WebSizes.Pick([new("thumb300y", "t"), new("original", "o"), new("large", "l"), new("screen", "s")]);
        Assert.Equal(("s", "t"), (url, thumb));
    }

    [Fact]
    public void Nasa_sizes_large_and_thumb()
    {
        var (url, thumb) = WebSizes.Pick([new("orig", "o"), new("medium", "m"), new("large", "l"), new("thumb", "t")]);
        Assert.Equal(("l", "t"), (url, thumb));
    }

    [Fact]
    public void Without_the_first_choice_the_next_size_is_used()
    {
        Assert.Equal(("l", "t"), WebSizes.Pick([new("large", "l"), new("thumb300y", "t")]));
        Assert.Equal(("m", "s"), WebSizes.Pick([new("medium", "m"), new("small", "s")]));
    }

    [Fact]
    public void A_small_nasa_item_uses_small_as_its_url() =>
        Assert.Equal(("s", "t"), WebSizes.Pick([new("small", "s"), new("thumb", "t"), new("orig", "o")]));

    [Fact]
    public void An_original_is_never_used() =>
        Assert.Equal((null, null), WebSizes.Pick([new("original", "o"), new("orig", "o2")]));
}
