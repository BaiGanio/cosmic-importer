namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D57 step 3: the question to the local vision model, and how its answer is read.</summary>
public class ModelCheckTests
{
    [Fact]
    public void The_prompt_names_the_object_and_carries_the_page_text()
    {
        var p = ModelCheck.Prompt("Great Orion Nebula", ["M 42", "Orion Nebula"], "esahubble", "opo9410a",
            "Hubble Probes the Great Orion Nebula", "Observation", ["M 42"], "A Hubble image.", "NASA");
        Assert.Contains("Great Orion Nebula (M 42, Orion Nebula)", p);
        Assert.Contains("Title: Hubble Probes the Great Orion Nebula", p);
        Assert.Contains("Type: Observation", p);
        Assert.Contains("Names on the page: M 42", p);
        Assert.Contains("Description: A Hubble image.", p);
        Assert.Contains("\"keep\"", p);
    }

    [Fact]
    public void A_long_description_is_cut_to_1000_characters()
    {
        var p = ModelCheck.Prompt("X", [], "nasa", "id", "t", null, [], new string('d', 5000), "NASA");
        Assert.Contains(new string('d', 1000) + "…", p);
        Assert.DoesNotContain(new string('d', 1001), p);
    }

    [Fact]
    public void A_clean_answer_is_read()
    {
        var a = ModelCheck.Parse("""{"realPhoto": true, "showsObject": true, "hasLabelsOrPanels": false, "score": 8, "keep": true, "reason": "clear Hubble photo"}""");
        Assert.Equal((true, 8, "clear Hubble photo"), (a!.Keep, a.Score, a.Reason));
        Assert.True(a.RealPhoto);
    }

    [Fact]
    public void An_answer_inside_a_code_fence_with_extra_words_is_read()
    {
        var a = ModelCheck.Parse("Sure!\n```json\n{\"keep\": false, \"score\": 2, \"reason\": \"diagram\"}\n```");
        Assert.Equal((false, 2, "diagram"), (a!.Keep, a.Score, a.Reason));
    }

    [Fact]
    public void A_score_outside_0_to_10_is_clamped() =>
        Assert.Equal(10, ModelCheck.Parse("""{"keep": true, "score": 14, "reason": "x"}""")!.Score);

    [Fact]
    public void An_answer_without_keep_is_unreadable() =>
        Assert.Null(ModelCheck.Parse("""{"score": 5}"""));

    [Fact]
    public void Not_json_is_unreadable() => Assert.Null(ModelCheck.Parse("I think it is a nice picture."));
}
