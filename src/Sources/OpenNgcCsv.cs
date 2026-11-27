using System.Globalization;
using System.Text;

namespace BGAPI.Cosmic.Importer.Sources;

/// <summary>Reads OpenNGC's ';'-separated files (NGC.csv, addendum.csv). Data only: nothing in it is run.</summary>
public static class OpenNgcCsv
{
    public static IReadOnlyList<OpenNgcRow> Parse(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToList();
        var header = Split(lines[0]);
        var index = header.Select((h, i) => (h, i)).ToDictionary(x => x.h, x => x.i, StringComparer.Ordinal);

        string? Get(string[] cells, string column)
        {
            var value = index.TryGetValue(column, out var i) && i < cells.Length ? cells[i].Trim() : "";
            return value.Length == 0 ? null : value;
        }

        return lines.Skip(1).Select(Split).Select(c => new OpenNgcRow
        {
            Name = Get(c, "Name") ?? "",
            Type = Get(c, "Type") ?? "",
            Ra = Get(c, "RA"),
            Dec = Get(c, "Dec"),
            Const = Get(c, "Const"),
            MajAx = Get(c, "MajAx"),
            MinAx = Get(c, "MinAx"),
            VMag = Get(c, "V-Mag"),
            Hubble = Get(c, "Hubble"),
            RadVel = Get(c, "RadVel"),
            Messier = int.TryParse(Get(c, "M"), NumberStyles.None, CultureInfo.InvariantCulture, out var m) ? m : null,
            CommonNames = (Get(c, "Common names") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        }).ToList();
    }

    /// <summary>Splits on ';', but not inside double quotes.</summary>
    private static string[] Split(string line)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        foreach (var ch in line)
        {
            if (ch == '"') quoted = !quoted;
            else if (ch == ';' && !quoted) { cells.Add(cell.ToString()); cell.Clear(); }
            else cell.Append(ch);
        }
        cells.Add(cell.ToString());
        return [.. cells];
    }
}
