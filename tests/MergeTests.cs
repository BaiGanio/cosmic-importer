namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D27: one owner per field, no gap filling. D34 facts, D35 slug and sources.</summary>
public class MergeTests
{
    private static OpenNgcRow M31Row(string? vMag = "3.44") => new()
    {
        Name = "NGC0224", Type = "G", Ra = "00:42:44.35", Dec = "+41:16:08.6", Const = "And",
        MajAx = "177.83", MinAx = "69.66", VMag = vMag, Hubble = "Sb", RadVel = "-300", Messier = 31,
        CommonNames = ["Andromeda Galaxy"],
    };

    private static WikidataEntity M31Entity(string? label = "Andromeda Galaxy") => new()
    {
        Id = "Q2469", Label = label, Messier = 31,
        CatalogCodes = ["M 31", "NGC 224", "PGC 2557"],
        Distances = [new("+0.82", Distance.Megaparsec, "normal", true)],
        Discovered = new("+1964-01-00T00:00:00Z", 10, "Q1985727"),
        Magnitudes = [5.0],
    };

    [Fact]
    public void OpenNGC_owns_position_magnitude_constellation_and_type()
    {
        var o = Merge.Merged(31, M31Row(), M31Entity());
        Assert.Equal("messier-31", o.Slug);
        Assert.Equal("00h 42m 44.35s", o.Ra);
        Assert.Equal("+41° 16′ 08.6″", o.Dec);
        Assert.Equal(3.44, o.Magnitude);
        Assert.Equal("Andromeda", o.Constellation);
        Assert.Equal("galaxy", o.Category);
        Assert.Equal("spiral (Sb)", o.Subcategory);
    }

    [Fact]
    public void Wikidata_owns_distance_and_discovered()
    {
        var o = Merge.Merged(31, M31Row(), M31Entity());
        Assert.Equal(2_700_000d, o.DistanceLy);
        Assert.Equal("1964-01", o.Discovered);
    }

    [Fact]
    public void Wikidata_never_fills_an_OpenNGC_gap() =>
        Assert.Null(Merge.Merged(31, M31Row(vMag: null), M31Entity()).Magnitude);

    [Fact]
    public void Name_and_aliases_come_from_both()
    {
        var o = Merge.Merged(31, M31Row(), M31Entity());
        Assert.Equal("Andromeda Galaxy", o.Name);
        Assert.Equal(["M 31", "NGC 224"], o.Aliases);
    }

    [Fact]
    public void Both_sources_are_listed()
    {
        var o = Merge.Merged(31, M31Row(), M31Entity());
        Assert.Equal(
            [
                new Source("OpenNGC", "NGC0224", "CC-BY-SA-4.0", "https://github.com/mattiaverga/OpenNGC"),
                new Source("Wikidata", "Q2469", "CC0-1.0", "https://www.wikidata.org/wiki/Q2469"),
            ],
            o.Sources);
    }

    [Fact]
    public void Facts_keep_the_OpenNGC_digits()
    {
        var o = Merge.Merged(31, M31Row(), M31Entity());
        Assert.Equal(
            [
                new Fact("apparentSize", "Apparent size", "177.83 × 69.66", "arcmin"),
                new Fact("radialVelocity", "Radial velocity", "-300", "km/s"),
            ],
            o.Facts);
    }

    [Fact]
    public void Apparent_size_without_a_minor_axis_is_the_major_axis_alone()
    {
        var row = M31Row() with { MajAx = "8.00", MinAx = null, RadVel = null };
        Assert.Equal([new Fact("apparentSize", "Apparent size", "8.00", "arcmin")], Merge.Merged(31, row, M31Entity()).Facts);
    }

    [Fact]
    public void Without_an_OpenNGC_row_there_is_no_category()
    {
        var o = Merge.Merged(31, null, M31Entity());
        Assert.Null(o.Category);
        Assert.Equal("Andromeda Galaxy", o.Name);
    }

    [Fact]
    public void The_source_files_hold_only_their_own_fields()
    {
        var openNgc = Merge.FromOpenNgc(31, M31Row());
        var wikidata = Merge.FromWikidata(31, M31Entity());
        Assert.Null(openNgc.DistanceLy);
        Assert.Null(openNgc.Discovered);
        Assert.Null(wikidata.Magnitude);
        Assert.Null(wikidata.Ra);
        Assert.Equal(["M 31", "NGC 224"], wikidata.Aliases);
    }
}
