# MongoDB-design

Eksporteret 2026-11-13 med `mongoexport` fra databasen `RottenPotatoes` (containeren `mongo`, port 27018).
Hver fil indeholder ét skabelondokument, hvor værdierne er typenavne (`"int"`, `"string"`, `"date"`, `"decimal"`).

Designet er tilpasset MySQL-databasen, så hvert felt har en kilde i MySQL: `series.seasons` og
`series.episodeCount` er tilføjet (Series.Seasons og Series.Episodes), og felter uden data i MySQL er fjernet
(`series.episodes[]`, `userActivities.target` og `userActivities.details`).

| Fil | Collection |
|---|---|
| `movies.json` | movies |
| `series.json` | series |
| `ratings.json` | ratings |
| `users.json` | users |
| `userActivities.json` | userActivities |

Filerne er dokumentation. De indsættes ikke automatisk i MongoDB-containeren.
Mapping til MySQL: `docs/database-mapping.md`.
