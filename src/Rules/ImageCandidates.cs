using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>One image candidate for one object (D46, D50, D57). Sizes: only links on an allowed host (D9).</summary>
public sealed record ImageCandidate(
    string Slug, string Source, string SourceId, string Title, string? Type, bool Named,
    string Credit, string CreditFrom, string License, string SourcePage, IReadOnlyList<ImageSize> Sizes,
    string? Url, string? ThumbUrl)
{
    public int Score { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}

/// <summary>An image a rule threw away (D57), with what is needed for a quick look in the review.</summary>
public sealed record DroppedImage(
    string Slug, string Source, string SourceId, string Title, string? Type, string By, string Reason,
    string? Credit, string? SourcePage, IReadOnlyList<ImageSize> Sizes, string? Url, string? ThumbUrl);

/// <summary>X01 stage 3: one hit becomes a candidate, or a dropped image with its reason (throw-away rules, D57).</summary>
public static class ImageCandidates
{
    public sealed record Result(ImageCandidate? Candidate, DroppedImage? Dropped)
    {
        public string? Refused => Dropped?.Reason;
    }

    public static Result FromDjangoplicity(string slug, string source, string site, SearchHit hit, DjangoplicityPage? page,
        IReadOnlyList<string> terms, IReadOnlyList<string> allowed)
    {
        var sizes = Sizes(hit.Links.Concat(page?.Sizes.Select(s => s.Url) ?? []), allowed);
        var (url, thumb) = WebSizes.Pick(sizes);
        var sourcePage = site + hit.PagePath;
        Result No(string why) => new(null, new DroppedImage(slug, source, hit.Id, hit.Title, page?.Type, "rule", why,
            page?.Credit, sourcePage, sizes, url, thumb));

        if (page is null) return No("page not fetched");
        if (page.Credit is null) return No("missing credit");
        if (ImageRules.NotARealPicture(page.Type)) return No($"not a real picture (type {page.Type})");
        if (page.Credit.Length > 500) return No("credit longer than 500 characters");
        if (sizes.Count == 0) return No("no image on an allowed host");
        if (url is null || thumb is null) return No("no web size or no thumbnail on an allowed host");
        var named = Pairing.Names(hit.Title, terms) || page.Names.Any(n => Pairing.Names(n, terms));
        return new(new ImageCandidate(slug, source, hit.Id, hit.Title, page.Type, named,
            page.Credit, "credit", "CC-BY-4.0", sourcePage, sizes, url, thumb), null);
    }

    public static Result FromNasa(string slug, SearchHit hit, NasaInfo? info, IReadOnlyList<string> terms, IReadOnlyList<string> allowed)
    {
        var sizes = Sizes(hit.Links, allowed);
        var (url, thumb) = WebSizes.Pick(sizes);
        var sourcePage = $"https://images.nasa.gov/details/{Uri.EscapeDataString(hit.Id)}";
        Result No(string why) => new(null, new DroppedImage(slug, "nasa", hit.Id, hit.Title, null, "rule", why,
            info?.Credit, sourcePage, sizes, url, thumb));

        if (info is null) return No("metadata not fetched");
        if (info.Refused is not null) return No(info.Refused);
        if (ImageRules.NasaTextSaysNotReal(info.Text) || ImageRules.NasaTextSaysNotReal(hit.Title)) return No("not a real picture (NASA text)");
        if (info.Credit!.Length > 500) return No("credit longer than 500 characters");
        if (sizes.Count == 0) return No("no image on an allowed host");
        if (url is null || thumb is null) return No("no web size or no thumbnail on an allowed host");
        var named = Pairing.Names(hit.Title, terms) || hit.Keywords.Any(k => Pairing.Names(k, terms));
        return new(new ImageCandidate(slug, "nasa", hit.Id, hit.Title, null, named,
            info.Credit, info.CreditFrom!, info.License!, sourcePage, sizes, url, thumb), null);
    }

    private static List<ImageSize> Sizes(IEnumerable<string> urls, IReadOnlyList<string> allowed) =>
        urls.Where(u => ImageRules.HostAllowed(u, allowed)).Distinct(StringComparer.Ordinal)
            .Select(u => new ImageSize(SourcePages.SizeName(u), u)).ToList();
}
