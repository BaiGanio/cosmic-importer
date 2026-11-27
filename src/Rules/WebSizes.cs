using BGAPI.Cosmic.Importer.Sources;

namespace BGAPI.Cosmic.Importer.Rules;

/// <summary>
/// X01 D57: the two links BGAPI takes (brief: url, thumbUrl), by one fixed rule. url: screen (about
/// 1280 px), large, medium, publicationjpg, small (NASA gives no larger copy when its original is small).
/// thumbUrl: thumb300y, thumb, small. Never an original (can be 700 MB).
/// </summary>
public static class WebSizes
{
    private static readonly string[] UrlOrder = ["screen", "large", "medium", "publicationjpg", "small"];
    private static readonly string[] ThumbOrder = ["thumb300y", "thumb", "small"];

    public static (string? Url, string? ThumbUrl) Pick(IReadOnlyList<ImageSize> sizes) =>
        (First(sizes, UrlOrder), First(sizes, ThumbOrder));

    private static string? First(IReadOnlyList<ImageSize> sizes, string[] order) =>
        order.Select(n => sizes.FirstOrDefault(s => s.Name == n)?.Url).FirstOrDefault(u => u is not null);
}
