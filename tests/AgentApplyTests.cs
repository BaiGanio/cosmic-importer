using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D58: an image the agent drops moves from Kept to Dropped, marked "agent", with its rules and reason.</summary>
public class AgentApplyTests
{
    private static ImageCandidate C(string id) =>
        new("messier-45", "esahubble", id, "t", "Observation", false, "c", "credit", "CC-BY-4.0", "https://p", [new("screen", "u")], "u", "t");

    [Fact]
    public void An_agent_drop_moves_the_image_to_dropped()
    {
        var results = new[] { new AgentResult("messier-45", "esahubble", "a", false, ["R9"], 1, "another object") };
        var (kept, dropped) = AgentApply.Apply([C("a"), C("b")], results);
        Assert.Equal(["b"], kept.Select(k => k.SourceId));
        var d = Assert.Single(dropped);
        Assert.Equal(("agent", "R9: another object"), (d.By, d.Reason));
        Assert.Equal("u", d.Url);
    }

    [Fact]
    public void An_agent_keep_or_no_answer_leaves_the_image_kept()
    {
        var results = new[] { new AgentResult("messier-45", "esahubble", "a", true, [], 9, "fine") };
        var (kept, dropped) = AgentApply.Apply([C("a"), C("b")], results);
        Assert.Equal(["a", "b"], kept.Select(k => k.SourceId));
        Assert.Empty(dropped);
    }

    [Fact]
    public void A_result_for_another_object_does_not_count()
    {
        var results = new[] { new AgentResult("messier-42", "esahubble", "a", false, ["R9"], 1, "x") };
        Assert.Equal(2, AgentApply.Apply([C("a"), C("b")], results).Kept.Count);
    }
}
