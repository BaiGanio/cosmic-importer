namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D31 (category) and D32 (subcategory).</summary>
public class TypeMapTests
{
    [Theory]
    [InlineData("G", "galaxy")]
    [InlineData("GCl", "cluster")]
    [InlineData("OCl", "cluster")]
    [InlineData("PN", "nebula")]
    [InlineData("SNR", "nebula")]
    [InlineData("Neb", "nebula")]
    [InlineData("HII", "nebula")]
    [InlineData("RfN", "nebula")]
    [InlineData("EmN", "nebula")]
    public void A_clear_type_maps_to_a_category(string type, string expected) =>
        Assert.Equal(expected, TypeMap.Category(type));

    [Theory]
    [InlineData("Cl+N")]
    [InlineData("*Ass")]
    [InlineData("**")]
    [InlineData("Other")]
    [InlineData("Dup")]
    [InlineData("*")]
    public void Any_other_type_needs_an_edge_case(string type) =>
        Assert.Null(TypeMap.Category(type));

    [Theory]
    [InlineData("E", "elliptical (E)")]
    [InlineData("Sb", "spiral (Sb)")]
    [InlineData("Sbc", "spiral (Sbc)")]
    [InlineData("SABb", "barred spiral (SABb)")]
    [InlineData("SBc", "barred spiral (SBc)")]
    [InlineData("S0-a", "lenticular (S0-a)")]
    public void A_galaxy_gets_a_word_and_its_code(string hubble, string expected) =>
        Assert.Equal(expected, TypeMap.Subcategory("G", hubble));

    [Theory]
    [InlineData("S?")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Irr")]
    public void An_unsure_or_unknown_galaxy_code_gives_no_subcategory(string? hubble) =>
        Assert.Null(TypeMap.Subcategory("G", hubble));

    [Theory]
    [InlineData("GCl", "globular")]
    [InlineData("OCl", "open")]
    [InlineData("PN", "planetary")]
    [InlineData("SNR", "supernova remnant")]
    [InlineData("HII", "emission")]
    [InlineData("EmN", "emission")]
    [InlineData("RfN", "reflection")]
    public void Other_types_get_a_word_only(string type, string expected) =>
        Assert.Equal(expected, TypeMap.Subcategory(type, null));

    [Theory]
    [InlineData("Neb")]
    [InlineData("Cl+N")]
    [InlineData("Other")]
    public void Types_without_a_mapping_give_no_subcategory(string type) =>
        Assert.Null(TypeMap.Subcategory(type, null));
}
