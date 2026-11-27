using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 stage 3: host check (D9), credit text (D53), image type (D51), NASA credit and license (D54).</summary>
public class ImageRulesTests
{
    private static readonly string[] Allowed =
        ["images-assets.nasa.gov", "cdn.esahubble.org", "cdn.esawebb.org", "cdn.eso.org", "storage.noirlab.edu"];

    [Theory]
    [InlineData("https://cdn.esahubble.org/archives/images/screen/opo0205d.jpg", true)]
    [InlineData("https://CDN.ESO.org/images/large/x.jpg", true)]
    [InlineData("https://storage.noirlab.edu/media/archives/images/large/x.jpg", true)]
    [InlineData("http://cdn.esahubble.org/archives/images/screen/opo0205d.jpg", false)]
    [InlineData("https://esahubble.org/media/archives/images/original/opo0205d.tif", false)]
    [InlineData("https://noirlab.edu/x.jpg", false)]
    [InlineData("https://evil.cdn.eso.org/x.jpg", false)]
    [InlineData("/media/archives/images/original/opo0205d.tif", false)]
    [InlineData("", false)]
    public void A_host_matches_the_allow_list_exactly_and_only_on_https(string url, bool ok) =>
        Assert.Equal(ok, ImageRules.HostAllowed(url, Allowed));

    [Fact]
    public void A_credit_keeps_its_words_and_drops_only_the_html() =>
        Assert.Equal("NASA/ESA and C.R. O'Dell (Vanderbilt University)", ImageRules.CleanCredit(
            "<p><a href=\"http://www.nasa.gov/\">NASA</a>/<a href=\"http://www.esa.int\">ESA</a> and C.R. O'Dell (Vanderbilt University)</p>"));

    [Fact]
    public void A_line_break_in_a_credit_stays_a_line_break() =>
        Assert.Equal("NASA, ESA, CSA\nScience leads and image processing: M. McCaughrean, S. Pearson",
            ImageRules.CleanCredit("<p>NASA, ESA, CSA<br />Science leads and image processing: M. McCaughrean, S. Pearson</p>"));

    [Fact]
    public void Html_codes_become_their_characters() =>
        Assert.Equal("ESA/Hubble & NASA", ImageRules.CleanCredit("ESA/Hubble &amp; NASA"));

    [Fact]
    public void Two_paragraphs_are_two_lines_and_spaces_collapse_as_a_browser_shows_them() =>
        Assert.Equal("ESA/Hubble & NASA Acknowledgement: Judy Schmidt\nSecond line",
            ImageRules.CleanCredit("<p>ESA/Hubble &amp; NASA  Acknowledgement:\n   Judy Schmidt</p><p>Second line</p>"));

    [Theory]
    [InlineData("Artwork", true)]
    [InlineData("Simulation", true)]
    [InlineData("Chart", true)]
    [InlineData("Observation", false)]
    [InlineData("Photographic", false)]
    [InlineData("Collage", true)]
    [InlineData("Planetary", false)]
    [InlineData(null, false)]
    public void D51_D56_artwork_simulation_chart_and_collage_are_not_real_pictures(string? type, bool refused) =>
        Assert.Equal(refused, ImageRules.NotARealPicture(type));

    [Theory]
    [InlineData("Artist's concept of a planet", true)]
    [InlineData("This illustration shows", true)]
    [InlineData("An ARTIST rendering", true)]
    [InlineData("Orion Nebula and Bow Shock", false)]
    [InlineData("Conceptual design review", false)]
    public void D51_nasa_text_with_artist_illustration_or_concept_is_not_a_real_picture(string text, bool refused) =>
        Assert.Equal(refused, ImageRules.NasaTextSaysNotReal(text));
}
