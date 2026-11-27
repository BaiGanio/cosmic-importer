# Cosmic: how we get the data (the approach)

**Why this file:** we import about once a year (D4). This file says how we did it, so next
year nobody has to work it out again. Change this file when the approach changes, and add a
decision row (D…) in `README.md`.

Sources and credits: `SOURCES.md`. What is on disk: `INVENTORY.md`.

## The pipeline

Each stage writes files. The next stage reads only those files. Nothing goes in that a human
did not check (D5).

| # | Stage | Reads | Writes | Asks the web? |
|---|---|---|---|---|
| 1 | Download | `INVENTORY.md` | `downloads/<source>/`, then updates `INVENTORY.md` | Yes, files only, slowly |
| 2 | Extract | `downloads/` | JSON in the brief's object shape, without images, in `out/`: one file per source and the merged file (D25). The report (empty fields, disagreements, open edge cases) goes to `docs/report.md`, committed (D30) | No |
| 3 | Check | the normalized files | image candidates with license and exact credit, cached on disk | Yes, image APIs and pages, slowly |
| 4 | Manifest | stages 2 and 3 | one hand-checkable file: slug, catalog id, image source + id, license, credit | No |
| 5 | Import | the **checked** manifest | batches of at most 1000 to `POST /api/cosmic/import` | BGAPI only |

## Rules for every stage

- **Look in `INVENTORY.md` first.** Do not download what is already there.
- **Files before APIs.** Use a whole file where one exists. Use an API only where no file
  exists (Wikidata per object, images).
- **Slow and polite.** One request at a time, about 1 per second. Obey a 429 and its
  `Retry-After`. Generic User-Agent, no personal data.
- **Cache every answer on disk.** A second run asks the web nothing it already asked.
- **Downloads are untrusted.** Each source has its own folder. Code in a download is deleted,
  never run.
- **A source with unknown terms is not used** (`SOURCES.md` marks it "not checked").
- **Do not change BGAPI or the brief to fit the data.** A misfit becomes a `CF-` item on the BGAPI board
  or a platform-plan item.

## Which objects go in

The brief: no real photo, no object. So:
1. Start from a named object in the fact files (OpenNGC common name, IAU star name, …).
2. Keep it only if stage 3 finds an image with an allowed host, a known license and an exact
   credit.
3. A human checks it in the manifest.

First set: the 110 Messier objects (D5). Later sets: **open**, decided per category.

## Images

**How many per object: all of them** (D60). Every image that is left after the rules, the agent and the human check goes into the import. Stage 3 keeps every good candidate in its cache.

Known facts (2026-11-24):
- `/catalog` shows only the first image (`thumbUrl`, `credit`). `/objects/{slug}` shows all.
- BGAPI has no limit on the number of images per object (`CosmicLimits` has none).
- Every image is a row a human must check.
- NOIRLab images are on `storage.noirlab.edu`. BGAPI allows it since D23.

## Refresh (next year)

**Open, not decided yet.** Proposal: download a source again only when it has a new release,
or its file is older than the last import.
