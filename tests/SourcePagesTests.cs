using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 stage 3: an ESA/Hubble, ESA/Webb, ESO or NOIRLab image page (cut from opo0205d, 2026-11-24), and a NASA metadata.json.</summary>
public class SourcePagesTests
{
    private const string Page = """
        <h1>Orion Nebula, NGC 1976, M42</h1>
        <strong>Credit:</strong><div class="credit"><p><a href="http://www.nasa.gov/">NASA</a>/<a href="http://www.esa.int">ESA</a> and C.R. O'Dell (Vanderbilt University)</p></div>
        <div class="copyright"><a href="/copyright/">Usage of ESA/Hubble Images and Videos</a></div>
        <h3>About the Image</h3><div><table aria-describedby="About the Image"><tr><th class="title" scope="row">Id:</th><td>opo0205d</td></tr><tr><th class="title" scope="row">Type:</th><td>Collage</td></tr></table></div><hr>
        <h3>About the Object</h3><div><table aria-describedby="About the Object"><tr><th class="title" scope="row">Name:</th><td><a href="http://simbad.u-strasbg.fr/simbad/sim-id?Ident=M+42">M 42</a>, <a href="x">Messier 42</a>, <a href="x">NGC 1976</a>, <a href="x">Orion Nebula</a></td></tr><tr><th class="title" scope="row">Type:</th><td>Milky Way : Nebula : Type : Star Formation</td></tr></table></div>
        <div><h3  class="archivegrouptitle" >Image Formats</h3>
        <div class="archive_download"><span class="archive_dl_text"><a href="/media/archives/images/original/opo0205d.tif">Fullsize Original</a></span></div>
        <div class="archive_download"><span class="archive_dl_text"><a href="https://cdn.esahubble.org/archives/images/large/opo0205d.jpg">Large JPEG</a></span></div>
        <div class="archive_download"><span class="archive_dl_text"><a href="https://cdn.esahubble.org/archives/images/screen/opo0205d.jpg">Screensize JPEG</a></span></div>
        </div>
        """;

    [Fact]
    public void The_page_gives_credit_type_names_and_every_size()
    {
        var p = SourcePages.Djangoplicity(Page);
        Assert.Equal("NASA/ESA and C.R. O'Dell (Vanderbilt University)", p.Credit);
        Assert.Equal("Collage", p.Type);
        Assert.Equal(["M 42", "Messier 42", "NGC 1976", "Orion Nebula"], p.Names);
        Assert.Equal(["original", "large", "screen"], p.Sizes.Select(s => s.Name));
        Assert.Equal("https://cdn.esahubble.org/archives/images/screen/opo0205d.jpg", p.Sizes[2].Url);
    }

    [Fact]
    public void The_page_gives_its_description_as_plain_text()
    {
        var html = """
            <h1>Hubble Probes the Great Orion Nebula</h1>
            <div class="archive-image archive-image-dark popup"><a class="popup" href="https://cdn.esahubble.org/x.jpg" title="t"><img src="x" /></a></div>
            <div style="margin-top: 10px"><p>A <a href="http://www.nasa.gov">NASA/</a><a href="http://www.esa.int">ESA</a> Hubble image of the Great Nebula in Orion.</p>
            <p>The nebula is a giant gas cloud.</p></div>
            <strong>Credit:</strong><div class="credit"><p>NASA</p></div>
            """;
        Assert.Equal("A NASA/ESA Hubble image of the Great Nebula in Orion. The nebula is a giant gas cloud.", SourcePages.Djangoplicity(html).Description);
    }

    [Fact]
    public void A_page_without_the_tables_has_no_type_and_no_names()
    {
        var p = SourcePages.Djangoplicity("""<strong>Credit:</strong><div class="credit"><p>NOIRLab</p></div>""");
        Assert.Equal("NOIRLab", p.Credit);
        Assert.Null(p.Type);
        Assert.Empty(p.Names);
    }

    [Fact]
    public void A_page_without_a_credit_box_has_no_credit() =>
        Assert.Null(SourcePages.Djangoplicity("<p>nothing</p>").Credit);

    private static string Nasa(string fields) => "{" + fields + "}";

    [Fact]
    public void D54_nasa_credit_comes_from_the_credit_field_first()
    {
        var m = SourcePages.NasaMetadata(Nasa("""
            "XMP:Credit": "NASA and the Hubble Heritage Team STScI/AURA", "AVAIL:Creator": "Someone", "AVAIL:Title": "Orion Nebula and Bow Shock"
            """));
        Assert.Equal("NASA and the Hubble Heritage Team STScI/AURA", m.Credit);
        Assert.Equal("credit", m.CreditFrom);
        Assert.Equal("public-domain", m.License);
        Assert.Null(m.Refused);
    }

    [Fact]
    public void D54_without_a_credit_the_creator_then_the_photographer_then_NASA()
    {
        Assert.Equal(("NASA Goddard", "creator"), Pick(""" "AVAIL:Creator": "NASA Goddard", "AVAIL:Photographer": "Bill" """));
        Assert.Equal(("Bill Ingalls", "photographer"), Pick(""" "AVAIL:Creator": "", "AVAIL:Photographer": "Bill Ingalls" """));
        Assert.Equal(("NASA", "fallback"), Pick(""" "AVAIL:Center": "MSFC" """));

        static (string?, string?) Pick(string fields) { var m = SourcePages.NasaMetadata(Nasa(fields)); return (m.Credit, m.CreditFrom); }
    }

    [Theory]
    [InlineData("Creative Commons Attribution 4.0 International License", "CC-BY-4.0")]
    [InlineData("Creative Commons Attribution 3.0 Unported license", "CC-BY-3.0")]
    public void A_nasa_item_with_creative_commons_terms_has_that_license(string terms, string license) =>
        Assert.Equal(license, SourcePages.NasaMetadata(Nasa($$"""
            "XMP:Credit": "ESA/Hubble & NASA", "XMP:UsageTerms": "{{terms}}"
            """)).License);

    [Fact]
    public void A_nasa_item_with_other_terms_is_refused() =>
        Assert.Equal("unknown usage terms: All rights reserved", SourcePages.NasaMetadata(Nasa("""
            "XMP:Credit": "X", "XMP:UsageTerms": "All rights reserved"
            """)).Refused);

    [Fact]
    public void A_nasa_item_marked_copyrighted_is_refused()
    {
        Assert.Equal("third-party copyright", SourcePages.NasaMetadata(Nasa(""" "XMP:Credit": "X", "Photoshop:CopyrightFlag": true """)).Refused);
        Assert.Equal("third-party copyright", SourcePages.NasaMetadata(Nasa(""" "XMP:Credit": "X", "AVAIL:Copyright": "Getty Images" """)).Refused);
        Assert.Null(SourcePages.NasaMetadata(Nasa(""" "XMP:Credit": "X", "Photoshop:CopyrightFlag": false, "AVAIL:Copyright": null """)).Refused);
    }

    [Fact]
    public void Nasa_text_for_the_artist_check_is_title_description_and_keywords()
    {
        var m = SourcePages.NasaMetadata(Nasa("""
            "XMP:Credit": "X", "AVAIL:Title": "Planet", "AVAIL:Description": "Artist's concept", "AVAIL:Keywords": ["a", "b"]
            """));
        Assert.Contains("Artist's concept", m.Text);
        Assert.Contains("Planet", m.Text);
        Assert.Contains("b", m.Text);
    }

    [Theory]
    [InlineData("https://images-assets.nasa.gov/image/PIA04227/PIA04227~thumb.jpg", "thumb")]
    [InlineData("https://images-assets.nasa.gov/image/PIA04227/PIA04227~orig.jpg", "orig")]
    [InlineData("https://cdn.eso.org/images/wallpaper1/x.jpg", "wallpaper1")]
    [InlineData("https://cdn.esahubble.org/archives/images/thumb300y/heic1502a.jpg", "thumb300y")]
    public void A_size_is_named_by_its_link(string url, string name) =>
        Assert.Equal(name, SourcePages.SizeName(url));
}
