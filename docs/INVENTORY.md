# Cosmic: what is downloaded

**Read this before you download anything.** If a source is here and its file is still on disk
with the same SHA-256, do not download it again. Download again only when the approach
(`APPROACH.md`, "Refresh") says so.

Files live in `downloads/<source>/` (local only, not committed, D10).
If the folder is lost, this list says what to get again, and from where.
SHA-256 is shortened to its first 16 characters.

| Source | File | Got on | Bytes | SHA-256 (16) | Version / note | From |
|---|---|---|---|---|---|---|
| OpenNGC | `openngc/NGC.csv` | 2026-11-24 | 3,876,288 | `840fe0c9ee1332e5` | Release `v20260501`, 13,969 rows | https://raw.githubusercontent.com/mattiaverga/OpenNGC/v20260501/database_files/NGC.csv |
| OpenNGC | `openngc/addendum.csv` | 2026-11-24 | 17,484 | `1d8f0914e643ada3` | Release `v20260501`, 64 rows (M40, M45 are here) | https://raw.githubusercontent.com/mattiaverga/OpenNGC/v20260501/database_files/addendum.csv |
| Wikidata | `wikidata/entities/Q*.json` (110 files) | 2026-11-24 | 6.7 MB total | one per file, not listed | The 110 Messier objects. Q-id from enwiki title `Messier N`, checked by P528/P972 (D21) | https://www.wikidata.org/wiki/Special:EntityData/<Q-id>.json |
| HYG | `hyg/hygdata_v41.csv` | 2026-11-24 | 33,932,548 | `d9f69fd86bbf90a4` | v4.1, 119,626 stars. Repo moved to codeberg.org/astronexus/hyg | https://raw.githubusercontent.com/astronexus/HYG-Database/main/hyg/CURRENT/hygdata_v41.csv |
| IAU-CSN | `iau/IAU-CSN.txt` | 2026-11-24 | 71,800 | `84fac0c90f1b19ab` | File says "last updated 2022-04-04", 455 names | https://www.pas.rochester.edu/~emamajek/WGSN/IAU-CSN.txt |
| Yale BSC (HEASARC) | `yale/heasarc_bsc5p.tdat.gz` | 2026-11-24 | 913,895 | `122628cde2d8bedf` | 9,110 stars | https://heasarc.gsfc.nasa.gov/FTP/heasarc/dbase/tdat_files/heasarc_bsc5p.tdat.gz |
| NASA Exoplanet Archive | `exoplanets/pscomppars.csv` | 2026-11-24 | 83,223,078 | `215e4427e899d7bf` | Table `pscomppars`, 6,473 planets, 703 columns | https://exoplanetarchive.ipac.caltech.edu/TAP/sync?query=select+*+from+pscomppars&format=csv |
| NASA JPL SSD | `jpl/planets_phys_par.html` | 2026-11-24 | 58,244 | `e49a487947295c38` | Web page with a table | https://ssd.jpl.nasa.gov/planets/phys_par.html |
| NASA JPL SSD | `jpl/sats_phys_par.html` | 2026-11-24 | 96,641 | `9a9c004d950923ea` | Web page, 59 table rows; may not list every moon | https://ssd.jpl.nasa.gov/sats/phys_par/ |
| MPC | `mpc/MPCORB.DAT.gz` | 2026-11-24 | 94,822,385 | `66dfb6e71b9a31e1` | About 1.59 million lines | https://minorplanetcenter.net/iau/MPCORB/MPCORB.DAT.gz |
| ATNF | `atnf/psrcat_pkg.tar.gz` | 2026-11-24 | 1,249,005 | `55daa29f71f344c8` | Catalogue v2.9.0, 4,503 pulsars | https://www.atnf.csiro.au/research/pulsar/psrcat/downloads/psrcat_pkg.tar.gz |
| ATNF | `atnf/extracted/psrcat_tar/psrcat.db` | 2026-11-24 | 10,548,603 | `bac32471fef3e60c` | Extracted. The C code and `Makefile` were deleted, never run (D21) | from the package above |
| nasa (images) | `nasa/search/**` (29 files), `nasa/metadata/*` (100 files) | 2026-11-24 | 1,067,332 + 370,950 | one per file, in `nasa/index.tsv` | Stage 3, 6 sample objects: search answers (D48) and `metadata.json` of every hit (D46) | see `nasa/index.tsv` |
| esahubble (images) | `esahubble/search/**` (35 files), `esahubble/pages/*` (266 files) | 2026-11-24 | 4,421,879 + 34,246,346 | one per file, in `esahubble/index.tsv` | Stage 3, 6 sample objects: search answers (D48) and the image page of every hit (D46) | see `esahubble/index.tsv` |
| esawebb (images) | `esawebb/search/**` (27 files), `esawebb/pages/*` (19 files) | 2026-11-24 | 1,706,479 + 1,472,689 | one per file, in `esawebb/index.tsv` | Stage 3, 6 sample objects: search answers (D48) and the image page of every hit (D46) | see `esawebb/index.tsv` |
| eso (images) | `eso/search/**` (27 files), `eso/pages/*` (124 files) | 2026-11-24 | 2,104,972 + 10,203,341 | one per file, in `eso/index.tsv` | Stage 3, 6 sample objects: search answers (D48) and the image page of every hit (D46) | see `eso/index.tsv` |
| noirlab (images) | `noirlab/search/**` (28 files), `noirlab/pages/*` (165 files) | 2026-11-24 | 2,218,257 + 13,473,047 | one per file, in `noirlab/index.tsv` | Stage 3, 6 sample objects: search answers (D48) and the image page of every hit (D46) | see `noirlab/index.tsv` |
| nasa (images) | `nasa/images/*` (6 files) | 2026-11-24 | 656,491 | one per file, in `nasa/index.tsv` | Stage 3, the `url` size of images sent to the local model (D57, D58) | see `nasa/index.tsv` |
| esahubble (images) | `esahubble/images/*` (25 files) | 2026-11-24 | 8,879,269 | one per file, in `esahubble/index.tsv` | Stage 3, the `url` size of images sent to the local model (D57, D58) | see `esahubble/index.tsv` |

## Not downloaded yet

| What | Why not yet |
|---|---|
| Image pages and NASA `metadata.json` for the other 104 objects | Stage 3: only the 6 sample objects are on disk |
| Gaia, SDSS, SIMBAD, VizieR, NED | Too big to download, or query only. Not needed for 1-3k objects |
