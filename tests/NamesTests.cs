namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D27 (name) and D36 (aliases as the sources write them).</summary>
public class NamesTests
{
    [Fact]
    public void The_OpenNGC_common_name_wins() =>
        Assert.Equal("Small Sgr Star Cloud", Names.Name(24, ["Small Sgr Star Cloud"], "Sagittarius Star Cloud"));

    [Fact]
    public void The_first_OpenNGC_common_name_is_the_name() =>
        Assert.Equal("Checkmark Nebula", Names.Name(17, ["Checkmark Nebula", "Swan Nebula"], "Omega Nebula"));

    [Fact]
    public void Without_a_common_name_the_Wikidata_label_is_used() =>
        Assert.Equal("Pinwheel Galaxy", Names.Name(101, [], "Pinwheel Galaxy"));

    [Fact]
    public void A_label_like_Messier_N_is_not_a_real_name() =>
        Assert.False(Names.IsRealName("Messier 13"));

    [Fact]
    public void Without_any_name_it_is_Messier_N() =>
        Assert.Equal("Messier 52", Names.Name(52, [], null));

    [Fact]
    public void Codes_are_kept_as_written()
    {
        var aliases = Names.Aliases("Crab Nebula", ["M1", "NGC 1952"], ["Crab Nebula"], "Crab Nebula");
        Assert.Equal(["M1", "NGC 1952"], aliases);
    }

    [Fact]
    public void Only_Messier_NGC_and_IC_codes_are_aliases()
    {
        var aliases = Names.Aliases("Andromeda Galaxy", ["M 31", "NGC 224", "PGC 2557", "PLX 124", "IC 4715", "2C 56"], ["Andromeda Galaxy"], "Andromeda Galaxy");
        Assert.Equal(["M 31", "NGC 224", "IC 4715"], aliases);
    }

    [Fact]
    public void Other_names_become_aliases_and_only_exact_duplicates_go()
    {
        var aliases = Names.Aliases("Checkmark Nebula", ["M 17", "NGC 6618"],
            ["Checkmark Nebula", "Lobster Nebula", "Swan Nebula", "omega Nebula"], "Omega Nebula");
        Assert.Equal(["M 17", "NGC 6618", "Lobster Nebula", "Swan Nebula", "omega Nebula", "Omega Nebula"], aliases);
    }

    [Fact]
    public void An_exact_duplicate_appears_once() =>
        Assert.Equal(["M 76", "NGC 650"], Names.Aliases("Little Dumbbell Nebula", ["M 76", "NGC 650", "NGC 650"], [], null));
}
