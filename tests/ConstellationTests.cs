namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D35: IAU full names.</summary>
public class ConstellationTests
{
    [Theory]
    [InlineData("And", "Andromeda")]
    [InlineData("UMa", "Ursa Major")]
    [InlineData("CVn", "Canes Venatici")]
    [InlineData("Sgr", "Sagittarius")]
    [InlineData("Sct", "Scutum")]
    [InlineData("Se1", "Serpens")]
    [InlineData("Se2", "Serpens")]
    public void A_code_gets_its_full_name(string code, string expected) =>
        Assert.Equal(expected, Constellations.FullName(code));

    [Theory]
    [InlineData(null)]
    [InlineData("Xyz")]
    [InlineData("and")]
    public void An_unknown_code_gives_null(string? code) =>
        Assert.Null(Constellations.FullName(code));

    [Fact]
    public void There_are_88_constellations() =>
        Assert.Equal(88, Constellations.AllNames.Count);
}
