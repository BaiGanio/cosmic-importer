namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D58: the agent applies Lyuben's rules file (image-rules.md) to every kept image.</summary>
public class AgentCheckTests
{
    private const string Rules = "## Drop rules\n\n- R1: A cover of a magazine.\n- R9: The picture shows another object.\n";

    [Fact]
    public void The_prompt_carries_the_rules_file_as_written_then_the_object_and_the_page_text()
    {
        var p = AgentCheck.Prompt(Rules, "Pleiades", ["M 45"], "esahubble", "potw1232a",
            "Close encounter with the Tarantula", "Observation", ["Tarantula Nebula"], "A Hubble image.", "ESA/Hubble & NASA");
        Assert.Contains("- R1: A cover of a magazine.", p);
        Assert.Contains("The object: Pleiades (M 45).", p);
        Assert.Contains("Names on the page: Tarantula Nebula", p);
        Assert.True(p.IndexOf("R1:", StringComparison.Ordinal) < p.IndexOf("The object:", StringComparison.Ordinal));
    }

    [Fact]
    public void The_rule_ids_are_read_from_the_file() =>
        Assert.Equal(["R1", "R9"], AgentCheck.RuleIds(Rules));

    [Fact]
    public void An_answer_with_broken_rules_is_read()
    {
        var a = AgentCheck.Parse("""{"keep": false, "broken": ["R9"], "score": 1, "reason": "Tarantula, not Pleiades"}""", ["R1", "R9"])!;
        Assert.False(a.Keep);
        Assert.Equal(["R9"], a.Broken);
        Assert.Equal("Tarantula, not Pleiades", a.Reason);
    }

    [Fact]
    public void One_broken_rule_drops_the_image_even_when_the_model_says_keep() =>
        Assert.False(AgentCheck.Parse("""{"keep": true, "broken": ["R1"], "score": 6, "reason": "x"}""", ["R1"])!.Keep);

    [Fact]
    public void A_rule_id_that_is_not_in_the_file_is_unreadable() =>
        Assert.Null(AgentCheck.Parse("""{"keep": false, "broken": ["R77"], "score": 1, "reason": "x"}""", ["R1"]));

    [Fact]
    public void No_broken_list_means_none() =>
        Assert.Empty(AgentCheck.Parse("""{"keep": true, "score": 9, "reason": "fine"}""", ["R1"])!.Broken);

    [Fact]
    public void Not_json_is_unreadable() => Assert.Null(AgentCheck.Parse("", ["R1"]));
}
