# MongoDB-design (eksisterende)

Eksporteret 2026-11-13 med `mongoexport` fra databasen `RottenPotatoes` (containeren `mongo`, port 27018).
Hver fil indeholder ét skabelondokument, hvor værdierne er typenavne (`"int"`, `"string"`, `"date"`, `"decimal"`).

| Fil | Collection |
|---|---|
| `movies.json` | movies |
| `series.json` | series |
| `ratings.json` | ratings |
| `users.json` | users |
| `userActivities.json` | userActivities |

Filerne er dokumentation. De indsættes ikke automatisk i MongoDB-containeren.
Mapping til MySQL: `docs/database-mapping.md`.
