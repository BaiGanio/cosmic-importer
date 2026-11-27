namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D46: a hit "names" the object when its text holds a term, ignoring only spaces and case.</summary>
public class PairingTests
{
    private static readonly string[] M31 = ["Andromeda Galaxy", "M 31", "NGC 224"];

    [Theory]
    [InlineData("Panorama of Spiral Galaxy, M31")]
    [InlineData("Messier 31 and m 31")]
    [InlineData("the andromeda galaxy")]
    [InlineData("Sharpest ever view of the Andromeda  Galaxy")]
    [InlineData("NGC224 in blue")]
    [InlineData("(M 31)")]
    public void A_text_that_holds_a_term_names_the_object(string text) =>
        Assert.True(Pairing.Names(text, M31));

    [Theory]
    [InlineData("M310 and M3100")]
    [InlineData("Star forming region, NGC 2244")]
    [InlineData("PORTRAIT - WALKER, DAVID M., ASTRONAUT, 31 January")]
    [InlineData("SM31 sample")]
    [InlineData("Andromeda constellation")]
    [InlineData("")]
    public void A_text_without_a_whole_term_does_not(string text) =>
        Assert.False(Pairing.Names(text, M31));

    [Fact]
    public void M_3_does_not_name_M_31() =>
        Assert.False(Pairing.Names("Hubble view of M 31", ["M 3"]));

    [Fact]
    public void No_text_names_nothing() =>
        Assert.False(Pairing.Names(null, M31));
}
