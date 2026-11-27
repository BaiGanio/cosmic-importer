namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>Wikidata's precision is kept: year, month or day.</summary>
public class DiscoveredTests
{
    private const string Gregorian = "Q1985727";
    private const string Julian = "Q1985786";

    [Theory]
    [InlineData("+1764-05-03T00:00:00Z", 11, "1764-05-03")]
    [InlineData("+1964-01-00T00:00:00Z", 10, "1964-01")]
    [InlineData("+1681-01-01T00:00:00Z", 9, "1681")]
    [InlineData("+0130-00-00T00:00:00Z", 9, "130")]
    [InlineData("-0260-00-00T00:00:00Z", 9, "260 BC")]
    public void The_precision_is_kept(string time, int precision, string expected) =>
        Assert.Equal(expected, Discovered.Format(new WikidataTime(time, precision, time.StartsWith('-') ? Julian : Gregorian)));

    [Fact]
    public void A_precision_coarser_than_a_year_gives_null() =>
        Assert.Null(Discovered.Format(new WikidataTime("+1600-00-00T00:00:00Z", 8, Gregorian)));

    [Fact]
    public void No_date_gives_null() =>
        Assert.Null(Discovered.Format(null));
}
