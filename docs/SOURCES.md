# Cosmic: data and image sources

**For:** the Cosmic Origins website team. **From:** BGAPI lab X01 (step 9, the importer).
**Status:** draft, 2026-11-24. Checked live on 2026-11-24. This is **not** the contract.
The contract is `Docs/briefs/cosmic-origins.md`.

## What the website must show

1. **For each image:** the `credit` text exactly as BGAPI sends it. Do not shorten it or
   change it. Show it next to the image, and link it to `sourcePage`. ESA/Hubble, ESA/Webb,
   ESO and NOIRLab require this (CC BY 4.0: the credit must be visible, unaltered and linked).
2. **For each object:** the `sources` list (`catalog`, `license`, `url`).
3. **On one "Credits" page:** every source in the tables below, with its credit text and
   license link.
4. **Share-alike:** data from OpenNGC and HYG is CC BY-SA 4.0. The website may show it, but
   that data stays CC BY-SA 4.0. Anyone who copies it must also share it under CC BY-SA 4.0.
   This covers the data only, not the website code.

## Facts (downloaded files)

| Source | What we take | File | License | Credit text |
|---|---|---|---|---|
| OpenNGC (release v20260501) | Galaxies, nebulae, clusters: type, RA/Dec, constellation, magnitude, size, names | `NGC.csv`, `addendum.csv` from https://github.com/mattiaverga/OpenNGC | CC BY-SA 4.0 | "OpenNGC by Mattia Verga, https://github.com/mattiaverga/OpenNGC (CC BY-SA 4.0)" |
| Wikidata | Distance, aliases, discovery date, discoverer | One JSON per object: `https://www.wikidata.org/wiki/Special:EntityData/<Q-id>.json` | CC0 | None required. We show "Wikidata" as a courtesy |
| IAU Catalog of Star Names (IAU-CSN) | Official star names | `IAU-CSN.txt` from https://www.pas.rochester.edu/~emamajek/WGSN/ | IAU products: Creative Commons Attribution | "IAU Catalog of Star Names, IAU Working Group on Star Names (WGSN)" |
| HYG star database v4.1 | Stars: distance, magnitude, spectral type | `hygdata_v41.csv`. Now hosted at https://codeberg.org/astronexus/hyg | CC BY-SA 4.0 | "HYG Database, astronexus (CC BY-SA 4.0)" |
| Yale Bright Star Catalog (via NASA HEASARC) | All naked-eye stars (9,110) | `heasarc_bsc5p.tdat.gz` from https://heasarc.gsfc.nasa.gov/ | **Not checked yet** | **Not checked yet** |
| NASA Exoplanet Archive | Exoplanets (6,473) | `pscomppars` table as CSV | NASA, free use | "This research has made use of the NASA Exoplanet Archive, which is operated by the California Institute of Technology, under contract with the National Aeronautics and Space Administration under the Exoplanet Exploration Program." |
| NASA JPL Solar System Dynamics | Planets and moons | Pages `ssd.jpl.nasa.gov/planets/phys_par.html`, `ssd.jpl.nasa.gov/sats/phys_par/` | NASA content: acknowledge NASA. JPL's own terms page **not checked** (it gave 403) | "NASA/JPL Solar System Dynamics" |
| IAU Minor Planet Center (MPCORB) | Asteroids (about 1.59 million rows) | `MPCORB.DAT.gz` from https://minorplanetcenter.net/iau/MPCORB.html | **Not checked yet** | **Not checked yet** |
| ATNF Pulsar Catalogue v2.9.0 | Pulsars (4,503) | `psrcat_pkg.tar.gz` from https://www.atnf.csiro.au/research/pulsar/psrcat/ | **Not found** (no license in its README) | **Not checked yet** |

A source marked "not checked" is not used until its terms are known.

## Images (links only, BGAPI stores no image files)

| Source | Where | License | Credit | Image host | Host allowed in BGAPI? |
|---|---|---|---|---|---|
| NASA Image and Video Library | https://images.nasa.gov, API `https://images-api.nasa.gov/search` (no key) | Generally not subject to copyright in the US. "NASA should be acknowledged as the source." Some items are third-party copyright: those are not used | Per item: `XMP:Credit` in `https://images-assets.nasa.gov/image/<nasa_id>/metadata.json` | `images-assets.nasa.gov` | Yes |
| ESA/Hubble | https://esahubble.org/images/ | CC BY 4.0 | Per image, the "Credit:" line on its page, unaltered | `cdn.esahubble.org` | Yes |
| ESA/Webb | https://esawebb.org/images/ | CC BY 4.0 | Per image, the "Credit:" line on its page, unaltered | `cdn.esawebb.org` | Yes |
| ESO | https://www.eso.org/public/images/ | CC BY 4.0 | Per image, the "Credit:" line on its page, unaltered | `cdn.eso.org` | Yes |
| NOIRLab | https://noirlab.edu/public/images/ | CC BY 4.0 | Per image, the "Credit:" line on its page, unaltered | `storage.noirlab.edu` | Yes (D23) |

Example credits read live on 2026-11-24:
- ESA/Hubble `heic1502a` (M31): "NASA, ESA, J. Dalcanton (University of Washington, USA), B. F. Williams (University of Washington, USA), L. C. Johnson (University of Washington, USA), the PHAT team, and R. Gendler."
- ESA/Webb `weic2205a`: "NASA, ESA, CSA, and STScI"
- NASA `PIA04921` (M31): "NASA/JPL/California Institute of Technology"
