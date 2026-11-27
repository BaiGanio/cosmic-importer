namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D47, D48: the search forms of one object, and what kind each one is.</summary>
public class SearchFormsTests
{
    private static CosmicObject M31 => new() { Slug = "messier-31", Name = "Andromeda Galaxy", Aliases = ["M 31", "NGC 224"] };

    [Fact]
    public void Every_term_of_messier_json_is_searched_as_written_plus_M31_and_Messier_31()
    {
        var terms = SearchForms.For(M31).Select(f => f.Term);
        Assert.Equal(["Andromeda Galaxy", "M 31", "NGC 224", "M31", "Messier 31"], terms);
    }

    [Fact]
    public void A_term_that_is_already_there_is_searched_once()
    {
        var m102 = new CosmicObject { Slug = "messier-102", Name = "Messier 102", Aliases = ["M 102", "NGC 5866"] };
        Assert.Equal(["Messier 102", "M 102", "NGC 5866", "M102"], SearchForms.For(m102).Select(f => f.Term));
    }

    [Theory]
    [InlineData("M 31", FormKind.MSpaced)]
    [InlineData("M31", FormKind.MCompact)]
    [InlineData("Messier 31", FormKind.MessierN)]
    [InlineData("NGC 224", FormKind.Ngc)]
    [InlineData("NGC224", FormKind.Ngc)]
    [InlineData("IC 4715", FormKind.Ic)]
    [InlineData("Andromeda Galaxy", FormKind.Name)]
    [InlineData("Messier 31 Galaxy", FormKind.Name)]
    public void Each_term_has_its_kind(string term, FormKind kind) =>
        Assert.Equal(kind, SearchForms.Kind(term));

    [Fact]
    public void D48_nasa_searches_without_M_space_N_and_without_NGC() =>
        Assert.Equal(["Andromeda Galaxy", "M31", "Messier 31"], SearchForms.ForSource(M31, "nasa").Select(f => f.Term));

    [Theory]
    [InlineData("esahubble")]
    [InlineData("esawebb")]
    [InlineData("eso")]
    [InlineData("noirlab")]
    public void D48_the_other_sources_search_without_M_space_N(string source) =>
        Assert.Equal(["Andromeda Galaxy", "NGC 224", "M31", "Messier 31"], SearchForms.ForSource(M31, source).Select(f => f.Term));

    [Fact]
    public void A_slug_that_is_not_messier_N_stops_the_run() =>
        Assert.Throws<InvalidOperationException>(() => SearchForms.For(new CosmicObject { Slug = "orion", Name = "Orion" }));
}
