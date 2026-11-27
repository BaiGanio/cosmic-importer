# Cosmic: extract report (X01 step 9, stage 2)

Written by the importer (`dotnet run --project src`). Do not edit by hand: the next run writes it again (D30).
Run on 2026-11-24. Sources: OpenNGC `v20260501`, Wikidata entities of 2026-11-24 (`INVENTORY.md`). Edge cases: `edge-cases.json`.

## Summary

- Objects written to `out/messier.json`: **110 of 110**
- Left out (no category): 0
- Open edge cases: 10 of 10
- Gaps (empty fields): 26
- Disagreements (the owner's value stays, D27): 21
- Discoverer: no object has it yet. Wikidata gives only Q-ids (107 of 110 have P61); the names need a stage 1 download (D34)
- Not compared: constellation and category. Wikidata gives Q-ids (P59, P31) whose names are not on disk

## Open edge cases (D26)

Applied in this run. A human checks each one and sets `verified`.

| Id | Object | Field | Value used | Reason |
|---|---|---|---|---|
| EC01 | messier-102 | identity | NGC5866 | Most sky atlases use NGC 5866. With it the Messier set has 110 objects. OpenNGC row NGC5866 gives the facts. |
| EC02 | messier-31 | discovered | 964 | Al-Sufi wrote it down in 964 (Book of Fixed Stars). The brief's example also says 964. Wikidata's 1964 is an error. |
| EC03 | messier-49 | distanceLy | (empty) | D28: more than one value cites a published work. Empty until a human picks one of them. |
| EC04 | messier-63 | distanceLy | (empty) | D28: more than one value cites a published work. Empty until a human picks one of them. |
| EC05 | messier-67 | distanceLy | (empty) | D28: more than one value cites a published work. Empty until a human picks one of them. |
| EC06 | messier-105 | distanceLy | (empty) | D28: more than one value cites a published work. Empty until a human picks one of them. |
| EC07 | messier-42 | category | nebula | D31: the type has no clear category. Known and shown as the Orion Nebula; the cluster (Trapezium) sits inside it. |
| EC08 | messier-24 | category | cluster | D31: the type has no clear category. A dense star field of the Milky Way. The brief has no better category. |
| EC09 | messier-40 | category | star | D31: the type has no clear category. Two stars that only look close (an optical double). The brief has no double-star category. |
| EC10 | messier-73 | category | star | D31: the type has no clear category. An asterism of 4 stars. The brief has no asterism category. |

## Gaps

Empty fields. Next year: check whether a source has the value now (D30).

| Object | Field | Why |
|---|---|---|
| messier-1 | facts.radialVelocity | OpenNGC has no RadVel |
| messier-8 | subcategory | OpenNGC type `Neb`, Hubble `-`: no mapping (D32) |
| messier-16 | subcategory | OpenNGC type `Neb`, Hubble `-`: no mapping (D32) |
| messier-17 | subcategory | OpenNGC type `Neb`, Hubble `-`: no mapping (D32) |
| messier-20 | subcategory | OpenNGC type `Neb`, Hubble `-`: no mapping (D32) |
| messier-24 | subcategory | OpenNGC type `*Ass`, Hubble `-`: no mapping (D32) |
| messier-24 | facts.radialVelocity | OpenNGC has no RadVel |
| messier-31 | facts.discoverer | Wikidata has no P61 |
| messier-40 | subcategory | OpenNGC type `**`, Hubble `-`: no mapping (D32) |
| messier-40 | facts.apparentSize | OpenNGC has no MajAx |
| messier-40 | facts.radialVelocity | OpenNGC has no RadVel |
| messier-42 | subcategory | OpenNGC type `Cl+N`, Hubble `-`: no mapping (D32) |
| messier-43 | facts.radialVelocity | OpenNGC has no RadVel |
| messier-45 | discovered | Wikidata has no P575 |
| messier-45 | facts.discoverer | Wikidata has no P61 |
| messier-49 | distanceLy | 2 values cite a published work (D28) |
| messier-63 | distanceLy | 2 values cite a published work (D28) |
| messier-67 | distanceLy | 2 values cite a published work (D28) |
| messier-73 | subcategory | OpenNGC type `Other`, Hubble `-`: no mapping (D32) |
| messier-73 | distanceLy | Wikidata has no distance |
| messier-73 | facts.apparentSize | OpenNGC has no MajAx |
| messier-73 | facts.radialVelocity | OpenNGC has no RadVel |
| messier-82 | subcategory | OpenNGC type `G`, Hubble `S?`: no mapping (D32) |
| messier-102 | discovered | Wikidata has no P575 |
| messier-102 | facts.discoverer | Wikidata has no P61 |
| messier-105 | distanceLy | 2 values cite a published work (D28) |

## Disagreements

The owner's value stays (D27). Shown: name differs; position more than 60″ apart; no Wikidata magnitude within 0.5 of OpenNGC's V; NGC/IC number differs.

| Object | Field | Owner | Other |
|---|---|---|---|
| messier-8 | ra/dec | OpenNGC: 18h 03m 41.27s, -24° 22′ 48.6″ | Wikidata: 270.9042°, -24.3867° (63″ apart) |
| messier-17 | name | OpenNGC: Checkmark Nebula | Wikidata: Omega Nebula |
| messier-17 | magnitude | OpenNGC V: 7 | Wikidata: 6 |
| messier-20 | magnitude | OpenNGC V: 8.5 | Wikidata: 6.3 |
| messier-24 | name | OpenNGC: Small Sgr Star Cloud | Wikidata: Sagittarius Star Cloud |
| messier-24 | ra/dec | OpenNGC: 18h 16m 56.12s, -18° 30′ 52.4″ | Wikidata: 274.2°, -18.55° (172″ apart) |
| messier-27 | magnitude | OpenNGC V: 7.4 | Wikidata: 14.089, 13.749, 14.247, 14.405, 14.0334, 11.786, 10.609 |
| messier-29 | ra/dec | OpenNGC: 20h 23m 57.77s, +38° 30′ 27.6″ | Wikidata: 305.9833°, 38.5233° (60″ apart) |
| messier-34 | ra/dec | OpenNGC: 02h 42m 07.40s, +42° 44′ 46.1″ | Wikidata: 40.521°, 42.76167° (62″ apart) |
| messier-35 | ra/dec | OpenNGC: 06h 09m 05.06s, +24° 20′ 19.1″ | Wikidata: 92.225°, 24.33333° (152″ apart) |
| messier-44 | name | OpenNGC: Beehive | Wikidata: Beehive Cluster |
| messier-45 | ra/dec | OpenNGC: 03h 47m 28.6s, +24° 06′ 19″ | Wikidata: 56.75°, 24.1167° (394″ apart) |
| messier-49 | magnitude | OpenNGC V: 8.28 | Wikidata: 12.17, 13.21, 6.273, 5.635, 5.396, 12.469, 10.413, 9.544, 9.074, 8.835 |
| messier-50 | ra/dec | OpenNGC: 07h 02m 40.47s, -08° 21′ 50.5″ | Wikidata: 105.6979°, -8.33778° (141″ apart) |
| messier-57 | magnitude | OpenNGC V: 8.8 | Wikidata: 15.769, 15.405, 15.901, 16.062, 15.608, 16.4 |
| messier-76 | magnitude | OpenNGC V: 10.1 | Wikidata: 17.48, 12.2, 17.3379, 17.69 |
| messier-84 | ra/dec | OpenNGC: 12h 25m 03.74s, +12° 53′ 13.1″ | Wikidata: 186.25°, 12.867° (90″ apart) |
| messier-86 | ra/dec | OpenNGC: 12h 26m 11.74s, +12° 56′ 46.4″ | Wikidata: 186.55°, 12.9° (166″ apart) |
| messier-88 | magnitude | OpenNGC V: 10.33 | Wikidata: 13.18, 14.33, 7.211, 6.56, 6.267 |
| messier-97 | magnitude | OpenNGC V: 9.9 | Wikidata: 15.777, 15.414, 15.96, 16.194, 15.7264, 16.71, 14.277, 10.934, 11.586, 13.01, 13.178 |
| messier-104 | magnitude | OpenNGC V: 8.59 | Wikidata: 8, 9.51, 9.55, 8.05, 5.886, 5.207, 4.962 |
