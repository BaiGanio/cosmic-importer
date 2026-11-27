using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BGAPI.Cosmic.Importer;
using BGAPI.Cosmic.Importer.Rules;
using BGAPI.Cosmic.Importer.Sources;

// X01 step 9, stage 2 (Extract). Reads only local files; asks the web nothing (APPROACH.md).
var root = FindRepoRoot(Directory.GetCurrentDirectory());
var importer = root;
var downloads = Path.Combine(importer, "downloads");
var outDir = Path.Combine(importer, "out");
var docs = Path.Combine(root, "docs");

// X01 stage 3 (Check): measure the search forms (D48). Without arguments the importer runs stage 2.
if (args is ["measure-search", .. var rest])
    return await SearchMeasure.RunAsync(importer, docs, rest);
if (args is ["fetch-details", .. var restDetails])
    return await ImageDetails.RunAsync(importer, restDetails);
if (args is ["build-candidates", .. var restBuild])
    return BuildCandidates.Run(root, importer, docs, restBuild);
if (args is ["model-test", .. var restModel])
    return await ModelTest.RunAsync(importer, restModel);
if (args is ["agent", .. var restAgent])
    return await AgentRun.RunAsync(importer, docs, restAgent);
if (args is ["send", .. var restSend])
    return await Send.RunAsync(importer, docs, restSend);
if (args is ["review"])
    return await ReviewServer.RunAsync(importer, docs);

var rows = new List<OpenNgcRow>();
foreach (var name in new[] { "NGC.csv", "addendum.csv" })
{
    var parsed = OpenNgcCsv.Parse(File.ReadAllText(Path.Combine(downloads, "openngc", name)));
    Console.WriteLine($"OpenNGC {name}: {parsed.Count} rows");
    rows.AddRange(parsed);
}

var entities = Directory.GetFiles(Path.Combine(downloads, "wikidata", "entities"), "Q*.json")
    .Select(f => WikidataJson.Parse(File.ReadAllText(f)))
    .ToList();
var twice = entities.Where(e => e.Messier is not null).GroupBy(e => e.Messier!.Value).Where(g => g.Count() > 1).ToList();
if (twice.Count > 0)
    throw new InvalidOperationException($"Two Wikidata files for one Messier number: {string.Join(", ", twice.Select(g => $"M{g.Key}"))}");
var byMessier = entities.Where(e => e.Messier is not null).ToDictionary(e => e.Messier!.Value);
Console.WriteLine($"Wikidata: {entities.Count} files, {byMessier.Count} with a Messier code");

var cases = EdgeCases.Parse(File.ReadAllText(Path.Combine(docs, "edge-cases.json")));
Console.WriteLine($"Edge cases: {cases.Count}");

var runs = new List<ObjectRun>();
var noWikidata = new List<int>();
var openNgcObjects = new List<CosmicObject>();
var wikidataObjects = new List<CosmicObject>();
for (var m = 1; m <= 110; m++)
{
    if (!byMessier.TryGetValue(m, out var entity)) { noWikidata.Add(m); continue; }
    var row = EdgeCases.RowFor(m, rows, cases);
    if (row is not null) openNgcObjects.Add(Merge.FromOpenNgc(m, row));
    wikidataObjects.Add(Merge.FromWikidata(m, entity));
    var final = EdgeCases.Apply(Merge.Merged(m, row, entity), cases);
    runs.Add(new ObjectRun(m, row, entity, final, Distance.Pick(entity.Distances)));
}

var json = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
Directory.CreateDirectory(outDir);
void WriteObjects(string file, IEnumerable<CosmicObject> objects) =>
    File.WriteAllText(Path.Combine(outDir, file), JsonSerializer.Serialize(new { objects }, json) + "\n");

var merged = runs.Where(r => r.Final.Category is not null).Select(r => r.Final).ToList();
WriteObjects("openngc.json", openNgcObjects);
WriteObjects("wikidata.json", wikidataObjects);
WriteObjects("messier.json", merged);
File.WriteAllText(Path.Combine(docs, "report.md"),
    Report.Build(runs, cases, noWikidata, DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

Console.WriteLine($"Written: {merged.Count} objects to out/messier.json; report: docs/report.md");
return 0;

static string FindRepoRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "cosmic-importer.sln"))) return dir.FullName;
    throw new InvalidOperationException("cosmic-importer.sln not found above the current directory");
}
