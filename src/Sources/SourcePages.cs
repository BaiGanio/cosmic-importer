using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BGAPI.Cosmic.Importer.Rules;

namespace BGAPI.Cosmic.Importer.Sources;

public sealed record ImageSize(string Name, string Url);

/// <summary>What one ESA/Hubble, ESA/Webb, ESO or NOIRLab image page says.</summary>
public sealed record DjangoplicityPage(string? Credit, string? Type, IReadOnlyList<string> Names, IReadOnlyList<ImageSize> Sizes)
{
    /// <summary>The page's text between its title and its credit, plain (D57: the model reads it).</summary>
    public string? Description { get; init; }
}

/// <summary>What one NASA metadata.json says (D54). Refused: why the item cannot be used, or null.</summary>
public sealed record NasaInfo(string? Credit, string? CreditFrom, string? License, string? Refused, string Text);

/// <summary>X01 stage 3: reads the cached detail answers. Values as the source writes them, except CleanCredit (D53).</summary>
public static partial class SourcePages
{
    [GeneratedRegex(@"<div class=""credit"">([\s\S]*?)</div>")] private static partial Regex CreditBox();
    [GeneratedRegex(@"aria-describedby=""About the Image"">([\s\S]*?)</table>")] private static partial Regex ImageTable();
    [GeneratedRegex(@"aria-describedby=""About the Object"">([\s\S]*?)</table>")] private static partial Regex ObjectTable();
    [GeneratedRegex(@"class=""archive_dl_text""><a href=""([^""]+)""")] private static partial Regex SizeLink();
    [GeneratedRegex(@"<[^>]+>")] private static partial Regex Tag();

    public static DjangoplicityPage Djangoplicity(string html)
    {
        var credit = CreditBox().Match(html);
        var image = ImageTable().Match(html);
        var obj = ObjectTable().Match(html);
        var names = obj.Success ? Row(obj.Groups[1].Value, "Name") : null;
        return new DjangoplicityPage(
            credit.Success ? ImageRules.CleanCredit(credit.Groups[1].Value) : null,
            image.Success ? Row(image.Groups[1].Value, "Type") : null,
            names is null ? [] : names.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList(),
            SizeLink().Matches(html).Select(m => WebUtility.HtmlDecode(m.Groups[1].Value))
                .Select(u => new ImageSize(SizeName(u), u)).ToList())
        { Description = Description(html) };
    }

    [GeneratedRegex(@"<div class=""archive-image[\s\S]*?</a>\s*</div>")] private static partial Regex ImageBox();
    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();

    private static string? Description(string html)
    {
        var credit = html.IndexOf("<strong>Credit:</strong>", StringComparison.Ordinal);
        if (credit < 0) return null;
        var h1 = html.LastIndexOf("</h1>", credit, StringComparison.Ordinal);
        if (h1 < 0) return null;
        var part = ImageBox().Replace(html[(h1 + 5)..credit], " ");
        part = Regex.Replace(part, @"</?(p|div|br|li|ul|ol|h\d|tr|td|table)\b[^>]*>", " ", RegexOptions.IgnoreCase);
        var text = Spaces().Replace(WebUtility.HtmlDecode(Tag().Replace(part, "")), " ").Trim();
        text = Regex.Replace(text, @" ([.,;:!?])", "$1");
        return text.Length == 0 ? null : text;
    }

    private static string? Row(string table, string label)
    {
        var m = Regex.Match(table, @"<th[^>]*>\s*" + Regex.Escape(label) + @":\s*</th>\s*<td>([\s\S]*?)</td>");
        return m.Success ? WebUtility.HtmlDecode(Tag().Replace(m.Groups[1].Value, "")).Trim() : null;
    }

    [GeneratedRegex(@"~([A-Za-z0-9]+)\.[A-Za-z0-9]+$")] private static partial Regex NasaSize();
    [GeneratedRegex(@"/([^/]+)/[^/]+$")] private static partial Regex Folder();

    /// <summary>NASA: the ~suffix (thumb, small, medium, large, orig). Others: the folder (thumb300y, screen, large, ...).</summary>
    public static string SizeName(string url)
    {
        var n = NasaSize().Match(url);
        if (n.Success) return n.Groups[1].Value;
        var f = Folder().Match(url);
        return f.Success ? f.Groups[1].Value : url;
    }

    [GeneratedRegex(@"^Creative Commons Attribution (\d\.\d)\b")] private static partial Regex CcBy();

    public static NasaInfo NasaMetadata(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        (string? credit, string? from) = First(root, ("XMP:Credit", "credit"), ("AVAIL:Creator", "creator"),
            ("XMP:Creator", "creator"), ("AVAIL:Photographer", "photographer"));
        credit = credit is null ? "NASA" : ImageRules.CleanCredit(credit);
        from ??= "fallback";

        var text = string.Join("\n", new[] { Str(root, "AVAIL:Title") ?? Str(root, "XMP:Title"), Str(root, "AVAIL:Description"),
            Str(root, "AVAIL:Keywords") }.OfType<string>());

        string? refused = null, license = null;
        var flag = root.TryGetProperty("Photoshop:CopyrightFlag", out var f)
            && (f.ValueKind == JsonValueKind.True || (f.ValueKind == JsonValueKind.String && bool.TryParse(f.GetString(), out var b) && b));
        if (flag || !string.IsNullOrWhiteSpace(Str(root, "AVAIL:Copyright")))
            refused = "third-party copyright";
        else if (Str(root, "XMP:UsageTerms") is { } terms && terms.Trim().Length > 0)
        {
            var cc = CcBy().Match(terms);
            if (cc.Success) license = $"CC-BY-{cc.Groups[1].Value}";
            else refused = $"unknown usage terms: {terms}";
        }
        else license = "public-domain";

        return new NasaInfo(credit, from, license, refused, text);
    }

    private static (string?, string?) First(JsonElement root, params (string Key, string From)[] keys)
    {
        foreach (var (key, from) in keys)
            if (Str(root, key) is { } v && v.Trim().Length > 0) return (v, from);
        return (null, null);
    }

    /// <summary>A string, or an array of strings joined by ", ". Null for null, a missing key, or another kind.</summary>
    private static string? Str(JsonElement root, string key) =>
        !root.TryGetProperty(key, out var v) ? null
        : v.ValueKind == JsonValueKind.String ? v.GetString()
        : v.ValueKind == JsonValueKind.Array ? string.Join(", ", v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()))
        : null;
}
