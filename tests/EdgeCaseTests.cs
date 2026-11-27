using System.Text.Json;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D26: the importer applies every edge case, open ones too.</summary>
public class EdgeCaseTests
{
    private static EdgeCase Case(string slug, string field, string valueJson) => new()
    {
        Id = "EC99", Slug = slug, Field = field, Status = "open",
        Value = JsonDocument.Parse(valueJson).RootElement.Clone(),
    };

    private static readonly OpenNgcRow M102Dup = new() { Name = "M102", Type = "Dup", Messier = 101 };
    private static readonly OpenNgcRow Ngc5457 = new() { Name = "NGC5457", Type = "G", Messier = 101 };
    private static readonly OpenNgcRow Ngc5866 = new() { Name = "NGC5866", Type = "G" };

    [Fact]
    public void The_file_is_read()
    {
        const string json = """
            { "$comment": "x", "entries": [
              { "id": "EC01", "slug": "messier-102", "field": "identity", "value": "NGC5866", "status": "open", "verifiedBy": null } ] }
            """;
        var c = Assert.Single(EdgeCases.Parse(json));
        Assert.Equal("EC01", c.Id);
        Assert.Equal("NGC5866", c.Value.GetString());
    }

    [Fact]
    public void A_Dup_row_is_never_the_object() =>
        Assert.Equal("NGC5457", EdgeCases.RowFor(101, [M102Dup, Ngc5457, Ngc5866], [])?.Name);

    [Fact]
    public void An_identity_case_names_the_row() =>
        Assert.Equal("NGC5866", EdgeCases.RowFor(102, [M102Dup, Ngc5457, Ngc5866], [Case("messier-102", "identity", "\"NGC5866\"")])?.Name);

    [Fact]
    public void Without_a_case_a_missing_number_has_no_row() =>
        Assert.Null(EdgeCases.RowFor(102, [M102Dup, Ngc5457, Ngc5866], []));

    private static readonly CosmicObject M31 = new() { Slug = "messier-31", Discovered = "1964-01", DistanceLy = 5 };

    [Fact]
    public void A_case_replaces_discovered() =>
        Assert.Equal("964", EdgeCases.Apply(M31, [Case("messier-31", "discovered", "\"964\"")]).Discovered);

    [Fact]
    public void A_null_value_empties_the_field() =>
        Assert.Null(EdgeCases.Apply(M31, [Case("messier-31", "distanceLy", "null")]).DistanceLy);

    [Fact]
    public void A_number_value_sets_the_distance() =>
        Assert.Equal(1234d, EdgeCases.Apply(M31, [Case("messier-31", "distanceLy", "1234")]).DistanceLy);

    [Fact]
    public void A_case_sets_the_category() =>
        Assert.Equal("nebula", EdgeCases.Apply(M31, [Case("messier-31", "category", "\"nebula\"")]).Category);

    [Fact]
    public void A_case_for_another_object_changes_nothing() =>
        Assert.Equal(M31, EdgeCases.Apply(M31, [Case("messier-42", "discovered", "\"1\"")]));

    [Fact]
    public void An_unknown_field_stops_the_run() =>
        Assert.Throws<InvalidOperationException>(() => EdgeCases.Apply(M31, [Case("messier-31", "colour", "\"red\"")]));
}
