namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D59: images whose page names a different object are kept in a list for later sets.</summary>
public class OtherObjectsTests
{
    private static readonly string[] M42 = ["Great Orion Nebula", "M 42", "NGC 1976", "Orion Nebula", "M42", "Messier 42"];

    [Fact]
    public void A_page_that_names_only_other_objects_is_another_object() =>
        Assert.True(OtherObjects.IsOther(["N 83B", "NGC 1748"], M42));

    [Fact]
    public void A_page_that_also_names_our_object_is_not() =>
        Assert.False(OtherObjects.IsOther(["M 42", "M 43", "Trapezium"], M42));

    [Fact]
    public void A_page_without_names_is_not_listed() =>
        Assert.False(OtherObjects.IsOther([], M42));

    [Fact]
    public void The_same_image_found_by_two_objects_is_one_entry_with_both_finds()
    {
        var list = OtherObjects.Merge([
            new OtherObjects.Find("esahubble", "heic0104a", "N83B", ["N 83B", "NGC 1748"], "credit", "https://p", "kept", "messier-42", "Orion Nebula"),
            new OtherObjects.Find("esahubble", "heic0104a", "N83B", ["N 83B", "NGC 1748"], "credit", "https://p", "kept", "messier-31", "M31"),
        ]);
        var e = Assert.Single(list);
        Assert.Equal(["N 83B", "NGC 1748"], e.NamesOnPage);
        Assert.Equal(["messier-42: Orion Nebula", "messier-31: M31"], e.FoundWhileSearching);
    }

    [Fact]
    public void Entries_are_sorted_by_their_first_name_as_written()
    {
        var list = OtherObjects.Merge([
            new OtherObjects.Find("eso", "b", "t", ["NGC 2070"], "c", "p", "kept", "messier-45", "Pleiades"),
            new OtherObjects.Find("esahubble", "a", "t", ["N 83B"], "c", "p", "kept", "messier-42", "Orion Nebula"),
        ]);
        Assert.Equal(["a", "b"], list.Select(e => e.SourceId));
    }
}
