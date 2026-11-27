# cosmic-importer

Builds the data for the Cosmic Origins website and imports it into BGAPI
(`POST /api/cosmic/import`, client `cosmic-importer`). It runs on one laptop, about once a
year. Moved from `BGAPI/Tools/BGAPI.Cosmic.Importer/` on 2026-11-27
([BaiGanio/baiganio-platform#4](https://github.com/BaiGanio/baiganio-platform/issues/4)).

The contract it follows: the BGAPI brief, `BGAPI/Docs/briefs/cosmic-origins.md`. The decisions
behind it (D1-D60): BGAPI lab X01.

## The pipeline

How and why: [`docs/APPROACH.md`](docs/APPROACH.md). Sources and credits:
[`docs/SOURCES.md`](docs/SOURCES.md). What is on disk: [`docs/INVENTORY.md`](docs/INVENTORY.md).

| Stage | Command (`dotnet run --project src -- <command>`) |
| --- | --- |
| 2. Extract facts (offline) | *(no command)* → `out/messier.json`, `docs/report.md` |
| 3. Search images | `measure-search`, `fetch-details` |
| 3. Build candidates (offline) | `build-candidates` → `out/image-candidates.json`, `docs/images-report.md` |
| 3. Model check | `agent` (local Ollama, rules in `docs/image-rules.md`) |
| 4. Human check | `review`, then http://localhost:5199 → `docs/picks.json` |
| 5. Send to BGAPI | `send [--dry-run] [--api URL] [--token-url URL]` |

Tests: `dotnet test --solution cosmic-importer.sln`.

## Not in git

`downloads/` (about 300 MB) and `out/` are caches. They can be downloaded or built again.
The secret for a non-local Keycloak comes from `COSMIC_IMPORTER_SECRET`; the local dev secret is
public (brief).

Image hosts: `docs/image-hosts.json`. It must match the brief and BGAPI `Cosmic:ImageHosts`.
