using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Tests;

/// <summary>X01 stage 3: the search answers, in the shape the sources sent on 2026-11-24 (probe "M 31").</summary>
public class SearchParseTests
{
    private const string Nasa = """
        {"collection":{"version":"1.0","metadata":{"total_hits":109},
          "items":[
            {"href":"https://images-assets.nasa.gov/image/PIA04921/collection.json",
             "data":[{"center":"JPL","title":"Andromeda Galaxy","nasa_id":"PIA04921","media_type":"image",
                      "keywords":["Galaxy Evolution Explorer GALEX","M31"],"description":"This image of M31 ..."}],
             "links":[{"href":"https://images-assets.nasa.gov/image/PIA04921/PIA04921~medium.jpg","rel":"alternate","render":"image"},
                      {"href":"https://images-assets.nasa.gov/image/PIA04921/PIA04921~thumb.jpg","rel":"preview","render":"image"}]},
            {"href":"https://images-assets.nasa.gov/image/S78-35285/collection.json",
             "data":[{"center":"JSC","title":"PORTRAIT - WALKER, DAVID M., ASTRONAUT CANDIDATE","nasa_id":"S78-35285","media_type":"image"}]}
          ]}}
        """;

    [Fact]
    public void Nasa_gives_the_total_and_each_hit_with_id_title_keywords_and_preview()
    {
        var page = SearchParse.Nasa(Nasa);
        Assert.Equal(109, page.Total);
        Assert.Equal(2, page.Hits.Count);
        var hit = page.Hits[0];
        Assert.Equal("PIA04921", hit.Id);
        Assert.Equal("Andromeda Galaxy", hit.Title);
        Assert.Equal(["Galaxy Evolution Explorer GALEX", "M31"], hit.Keywords);
        Assert.Equal("https://images-assets.nasa.gov/image/PIA04921/PIA04921~thumb.jpg", hit.ThumbUrl);
        Assert.Equal(["https://images-assets.nasa.gov/image/PIA04921/PIA04921~medium.jpg",
            "https://images-assets.nasa.gov/image/PIA04921/PIA04921~thumb.jpg"], hit.Links);
    }

    [Fact]
    public void A_nasa_hit_without_keywords_or_links_still_counts()
    {
        var hit = SearchParse.Nasa(Nasa).Hits[1];
        Assert.Equal("S78-35285", hit.Id);
        Assert.Empty(hit.Keywords);
        Assert.Null(hit.ThumbUrl);
    }

    private const string Esa = """
        <div class="results">Showing 1 to 50 of 60
        </div>
        <script>
        var images = [
            
            {
                id: 'heic1502a',
                title: 'Sharpest ever view of the Andromeda Galaxy',
                width: 69536,
                height: 22230,
                src: 'https://cdn.esahubble.org/archives/images/thumb300y/heic1502a.jpg',
                url: '/images/heic1502a/',
                potw: ''
            },
            
            {
                id: 'potw2402a',
                title: 'When one plus one (eventually) equals one, Hubble\'s view',
                width: 4070,
                height: 3726,
                src: 'https://cdn.esahubble.org/archives/images/thumb300y/potw2402a.jpg',
                url: '/images/potw2402a/',
                potw: '8 January 2024'
            },
        ];
        </script>
        """;

    [Fact]
    public void An_esa_page_gives_the_total_and_each_hit_with_id_title_thumb_and_page()
    {
        var page = SearchParse.Djangoplicity(Esa);
        Assert.Equal(60, page.Total);
        Assert.Equal(["heic1502a", "potw2402a"], page.Hits.Select(h => h.Id));
        Assert.Equal("Sharpest ever view of the Andromeda Galaxy", page.Hits[0].Title);
        Assert.Equal("https://cdn.esahubble.org/archives/images/thumb300y/heic1502a.jpg", page.Hits[0].ThumbUrl);
        Assert.Equal("/images/heic1502a/", page.Hits[0].PagePath);
    }

    [Fact]
    public void An_escaped_quote_in_a_title_is_read_as_a_quote() =>
        Assert.Equal("When one plus one (eventually) equals one, Hubble's view", SearchParse.Djangoplicity(Esa).Hits[1].Title);

    [Fact]
    public void No_images_found_is_zero_hits() =>
        Assert.Equal(0, SearchParse.Djangoplicity("<p>Sort by: Ranking Date No images were found</p>").Total);

    [Fact]
    public void A_page_that_is_neither_a_list_nor_empty_stops_the_run() =>
        Assert.Throws<InvalidOperationException>(() => SearchParse.Djangoplicity("<html>Access denied</html>"));
}
