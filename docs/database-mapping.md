# Database-mapping: MySQL → MongoDB og Neo4j

Kilder:

- **MySQL:** `rotten_potatoes_full_dump.sql` (Mandatory 1), 22 tabeller og 2 views.
- **MongoDB:** eksisterende design i `database/mongodb/design/*.json` (eksporteret fra databasen `RottenPotatoes`).
- **Neo4j:** eksisterende design i `database/neo4j/design/constraints.cypher` (eksporteret fra databasen `rottenpotatoes`). Designet indeholder kun labels og id-constraints.

Markeringer:

- **TODO:** beslutning mangler i jeres design.
- **Ikke i design:** MySQL-data, som designet ikke har med. Det kan være et bevidst fravalg; skriv begrundelsen.

---

## 1. MySQL-tabeller

| Tabel | Kolonner | Nøgler / relationer |
|---|---|---|
| Movies | Id, Title, Duration, ReleaseDate, Resume, Language, AgeRating, CreatedAt, UpdatedAt, AverageRating | PK Id |
| Series | Id, Title, ReleaseDate, Resume, Language, AgeRating, Seasons, Episodes, CreatedAt, UpdatedAt | PK Id |
| Actors | Id, Name, DateOfBirth, Biography | PK Id |
| Directors | Id, Name, DateOfBirth, Biography | PK Id |
| Genres | Id, Name | PK Id, UQ Name |
| Awards | Id, Name | PK Id, UQ Name |
| StreamingServices | Id, Name | PK Id, UQ Name |
| Users | Id, Username, Email, PasswordHash, CreatedAt, UpdatedAt | PK Id, UQ Username, UQ Email |
| Ratings | Id, UserId, MovieId, SeriesId, Score, CreatedAt, UpdatedAt | FK Users, Movies, Series. Præcis én af MovieId/SeriesId |
| Reviews | Id, RatingId, Content, CreatedAt, UpdatedAt | FK Ratings (1:1, UQ RatingId) |
| UserActivity | Id, UserId, ActivityType, ActivityAt | FK Users |
| UserActivityArchive | Id, UserId, ActivityType, ActivityAt, ArchivedAt | ingen FK (fyldes af eventet ArchiveOldUserActivity) |
| MovieActors / SeriesActors | MovieId/SeriesId, ActorId | junction (M:N) |
| MovieDirectors / SeriesDirectors | MovieId/SeriesId, DirectorId | junction (M:N) |
| MovieGenres / SeriesGenres | MovieId/SeriesId, GenreId | junction (M:N) |
| MovieAwards / SeriesAwards | MovieId/SeriesId, AwardId | junction (M:N) |
| MovieStreamingServices / SeriesStreamingServices | MovieId/SeriesId, StreamingServiceId | junction (M:N) |

Views (afledte data, migreres normalt ikke): `MovieRatingOverview`, `SeriesRatingOverview`.

---

## 2. MongoDB-collections

### movies

| Dokumentfelt | Type | MySQL-kilde |
|---|---|---|
| `_id` | int | Movies.Id |
| `title` | string | Movies.Title |
| `duration` | int | Movies.Duration |
| `releaseDate` | date | Movies.ReleaseDate |
| `resume` | string | Movies.Resume |
| `language` | string | Movies.Language |
| `ageRating` | string | Movies.AgeRating |
| `averageRating` | decimal | Movies.AverageRating |
| `createdAt` | date | Movies.CreatedAt |
| `updatedAt` | date | Movies.UpdatedAt |
| `genres[]` {id, name} | embedded | MovieGenres ⋈ Genres |
| `actors[]` {id, name, dateOfBirth, biography} | embedded | MovieActors ⋈ Actors |
| `directors[]` {id, name, dateOfBirth, biography} | embedded | MovieDirectors ⋈ Directors |
| `awards[]` {id, name} | embedded | MovieAwards ⋈ Awards |
| `streamingServices[]` {id, name} | embedded | MovieStreamingServices ⋈ StreamingServices |

### series

| Dokumentfelt | Type | MySQL-kilde |
|---|---|---|
| `_id` | int | Series.Id |
| `title`, `releaseDate`, `resume`, `language`, `ageRating` | | Series.Title, ReleaseDate, Resume, Language, AgeRating |
| `averageRating` | decimal | **TODO:** Series har ingen AverageRating-kolonne. Beregnes den med `GetAverageSeriesRating()` / `SeriesRatingOverview`? |
| `createdAt`, `updatedAt` | date | Series.CreatedAt, UpdatedAt |
| `genres[]` {id, name} | embedded | SeriesGenres ⋈ Genres |
| `actors[]` {id, name} | embedded | SeriesActors ⋈ Actors (DateOfBirth/Biography er ikke med, modsat movies) |
| `directors[]` {id, name} | embedded | SeriesDirectors ⋈ Directors |
| `awards[]` {id, name} | embedded | SeriesAwards ⋈ Awards |
| `streamingServices[]` {id, name} | embedded | SeriesStreamingServices ⋈ StreamingServices |
| `episodes[]` {id, seasonNumber, episodeNumber, title, duration, releaseDate} | embedded | **TODO:** findes ikke i MySQL (ingen Episodes-tabel) |
| | | **Ikke i design:** Series.Seasons, Series.Episodes (antal) |

### ratings

| Dokumentfelt | Type | MySQL-kilde |
|---|---|---|
| `_id` | int | Ratings.Id |
| `userId` | int | Ratings.UserId (reference til users) |
| `target.type` | string | Afledt: MovieId ≠ NULL → film, SeriesId ≠ NULL → serie. **TODO:** fastlæg værdierne (fx `"movie"`/`"series"`) |
| `target.id` | int | Ratings.MovieId eller Ratings.SeriesId |
| `score` | decimal | Ratings.Score |
| `createdAt` | date | Ratings.CreatedAt |
| `review` {id, content, createdAt} | embedded (valgfri) | Reviews (Reviews.RatingId = Ratings.Id) |
| | | **Ikke i design:** Ratings.UpdatedAt, Reviews.UpdatedAt |

### users

| Dokumentfelt | Type | MySQL-kilde |
|---|---|---|
| `_id` | int | Users.Id |
| `username` | string | Users.Username |
| `email` | string | Users.Email |
| `passwordHash` | string | Users.PasswordHash |
| `createdAt` | date | Users.CreatedAt |
| `updatedAt` | date | Users.UpdatedAt |

### userActivities

| Dokumentfelt | Type | MySQL-kilde |
|---|---|---|
| `_id` | int | UserActivity.Id / UserActivityArchive.Id |
| `userId` | int | UserId |
| `activityType` | string | ActivityType |
| `createdAt` | date | ActivityAt |
| `archivedAt` | date | UserActivityArchive.ArchivedAt (**TODO:** null for aktive rækker?) |
| `target` {type, id} | embedded | **TODO:** findes ikke i MySQL |
| `details.description` | string | **TODO:** findes ikke i MySQL |

**TODO:** Indexes ud over `_id` (fx `ratings.userId`, `ratings.target.id`, `movies.title`). Designet har kun `_id`-indexet.

---

## 3. Neo4j

Community Edition har kun databasen `neo4j`. Designet blev lavet i databasen `rottenpotatoes` (Enterprise).

### Nodes

| Label | MySQL-kilde | Properties | Constraint |
|---|---|---|---|
| Movie | Movies | `id` ← Id. **TODO:** øvrige properties | `movie_id_unique` |
| Series | Series | `id` ← Id. **TODO** | `series_id_unique` |
| User | Users | `id` ← Id. **TODO** (PasswordHash med eller ej?) | `user_id_unique` |
| Actor | Actors | `id` ← Id. **TODO** | `actor_id_unique` |
| Director | Directors | `id` ← Id. **TODO** | `director_id_unique` |
| Genre | Genres | `id` ← Id. **TODO** | `genre_id_unique` |
| Award | Awards | `id` ← Id. **TODO** | `award_id_unique` |
| StreamingService | StreamingServices | `id` ← Id. **TODO** | `streaming_service_id_unique` |
| Rating | Ratings | `id` ← Id. **TODO** | `rating_id_unique` |
| Review | Reviews | `id` ← Id. **TODO** | `review_id_unique` |
| UserActivity | UserActivity (+ Archive?) | `id` ← Id. **TODO** | `user_activity_id_unique` |

### Relationships

**TODO:** Designet indeholder ingen relationships. Hver MySQL-relation nedenfor skal have en type og en retning, eller fravælges eksplicit:

| MySQL-relation | Fra-label | Til-label | Relationship-type | Properties |
|---|---|---|---|---|
| MovieActors / SeriesActors | TODO | TODO | TODO | TODO |
| MovieDirectors / SeriesDirectors | TODO | TODO | TODO | TODO |
| MovieGenres / SeriesGenres | TODO | TODO | TODO | TODO |
| MovieAwards / SeriesAwards | TODO | TODO | TODO | TODO |
| MovieStreamingServices / SeriesStreamingServices | TODO | TODO | TODO | TODO |
| Ratings.UserId → Users | TODO | TODO | TODO | TODO |
| Ratings.MovieId / SeriesId → Movies / Series | TODO | TODO | TODO | TODO |
| Reviews.RatingId → Ratings | TODO | TODO | TODO | TODO |
| UserActivity.UserId → Users | TODO | TODO | TODO | TODO |
