namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 D28 (which value) and D29 (how many digits).</summary>
public class DistanceTests
{
    private static DistanceClaim C(string amount, string unit, string rank = "normal", bool paper = false) =>
        new(amount, unit, rank, paper);

    [Theory]
    [InlineData("+11.27", 4)]
    [InlineData("+0.82", 2)]
    [InlineData("+786.968", 6)]
    [InlineData("+0.9", 1)]
    [InlineData("+300", 3)]
    [InlineData("+0.0820", 3)]
    [InlineData("-5", 1)]
    public void Significant_digits_ignore_the_sign_and_leading_zeros(string number, int expected) =>
        Assert.Equal(expected, Distance.SignificantDigits(number));

    [Fact]
    public void Megaparsec_keeps_the_source_digits() =>
        Assert.Equal(36_760_000d, Distance.ToLightYears("+11.27", Distance.Megaparsec));

    [Fact]
    public void Kiloparsec_with_one_digit_gives_one_digit() =>
        Assert.Equal(3_000d, Distance.ToLightYears("+0.9", Distance.Kiloparsec));

    [Fact]
    public void Parsec_converts_with_the_source_digits() =>
        Assert.Equal(2_566.75d, Distance.ToLightYears("+786.968", Distance.Parsec));

    [Fact]
    public void Light_years_are_kept_as_they_are() =>
        Assert.Equal(25_000d, Distance.ToLightYears("+25000", Distance.LightYear));

    [Fact]
    public void An_unknown_unit_gives_no_value() =>
        Assert.Null(Distance.ToLightYears("+5", "Q999"));

    [Fact]
    public void A_deprecated_value_is_dropped() =>
        Assert.Equal(2d, Distance.Pick([C("+1", Distance.LightYear, "deprecated"), C("+2", Distance.LightYear)]).LightYears);

    [Fact]
    public void A_preferred_value_wins_over_a_normal_one() =>
        Assert.Equal(2d, Distance.Pick([C("+1", Distance.LightYear, paper: true), C("+2", Distance.LightYear, "preferred")]).LightYears);

    [Fact]
    public void A_single_normal_value_is_taken() =>
        Assert.Equal(25_000d, Distance.Pick([C("+25000", Distance.LightYear)]).LightYears);

    [Fact]
    public void A_published_work_beats_a_web_page()
    {
        // M74: 35 million ly from the SEDS page, 7.2 Mpc from a paper.
        var pick = Distance.Pick([C("+35000000", Distance.LightYear), C("+7.2", Distance.Megaparsec, paper: true)]);
        Assert.Equal(23_000_000d, pick.LightYears);
        Assert.Null(pick.WhyEmpty);
    }

    [Fact]
    public void Two_published_values_leave_the_field_empty()
    {
        var pick = Distance.Pick([C("+52000000", Distance.LightYear, paper: true), C("+16.07", Distance.Megaparsec, paper: true)]);
        Assert.Null(pick.LightYears);
        Assert.Contains("2 values cite a published work", pick.WhyEmpty);
    }

    [Fact]
    public void Several_values_without_a_published_work_leave_the_field_empty()
    {
        var pick = Distance.Pick([C("+1", Distance.LightYear), C("+2", Distance.LightYear)]);
        Assert.Null(pick.LightYears);
        Assert.Contains("none cites a published work", pick.WhyEmpty);
    }

    [Fact]
    public void No_value_leaves_the_field_empty()
    {
        var pick = Distance.Pick([]);
        Assert.Null(pick.LightYears);
        Assert.NotNull(pick.WhyEmpty);
    }

    [Fact]
    public void We_never_average()
    {
        var pick = Distance.Pick([C("+1000", Distance.LightYear), C("+3000", Distance.LightYear)]);
        Assert.NotEqual(2000d, pick.LightYears);
    }
}
