namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D33: OpenNGC's digits, only the symbols change.</summary>
public class SexagesimalTests
{
    [Theory]
    [InlineData("00:42:44.35", "00h 42m 44.35s")]
    [InlineData("12:22:16.1", "12h 22m 16.1s")]
    public void Ra_keeps_the_digits(string input, string expected) =>
        Assert.Equal(expected, Sexagesimal.FormatRa(input));

    [Theory]
    [InlineData("+41:16:08.6", "+41° 16′ 08.6″")]
    [InlineData("+58:05:04", "+58° 05′ 04″")]
    [InlineData("-11:37:23.0", "-11° 37′ 23.0″")]
    public void Dec_keeps_the_digits_and_the_sign(string input, string expected) =>
        Assert.Equal(expected, Sexagesimal.FormatDec(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12:22")]
    [InlineData("1:2:3")]
    public void A_bad_ra_gives_null(string? input) =>
        Assert.Null(Sexagesimal.FormatRa(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("+41:16")]
    [InlineData("41:16:08.6")]
    public void A_bad_dec_gives_null(string? input) =>
        Assert.Null(Sexagesimal.FormatDec(input));

    [Fact]
    public void Ra_in_degrees() =>
        Assert.Equal(10.684792, Sexagesimal.RaDegrees("00:42:44.35")!.Value, 6);

    [Fact]
    public void A_negative_dec_in_degrees() =>
        Assert.Equal(-11.623056, Sexagesimal.DecDegrees("-11:37:23.0")!.Value, 6);
}
