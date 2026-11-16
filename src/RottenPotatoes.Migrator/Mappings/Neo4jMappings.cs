using System.Globalization;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Migrator.Mappings;

/// <summary>
/// MySQL-entities -> rækker (maps), som sendes til Neo4j som parameter: UNWIND $rows AS row.
/// Design: docs/database-mapping.md, afsnit 3.
///
/// - Id'er sendes som long (Neo4j's heltalstype).
/// - Datoer sendes som ISO-tekst og laves om til Neo4j's date/datetime i Cypher.
/// - Fremmednøgler bliver ikke properties; de bruges kun til at oprette edges.
/// - Null-værdier gemmes ikke af Neo4j (propertyen findes bare ikke på noden).
/// </summary>
public static class Neo4jMappings
{
    public static Dictionary<string, object?> ToRow(Genre genre) => Named(genre.Id, genre.Name);

    public static Dictionary<string, object?> ToRow(Award award) => Named(award.Id, award.Name);

    public static Dictionary<string, object?> ToRow(StreamingService service) => Named(service.Id, service.Name);

    public static Dictionary<string, object?> ToRow(Actor actor) => Person(actor.Id, actor.Name, actor.DateOfBirth, actor.Biography);

    public static Dictionary<string, object?> ToRow(Director director) => Person(director.Id, director.Name, director.DateOfBirth, director.Biography);

    // Kun id og username: email og passwordHash kopieres ikke ind i grafen.
    public static Dictionary<string, object?> ToRow(User user) => new()
    {
        ["id"] = (long)user.Id,
        ["username"] = user.Username,
    };

    public static Dictionary<string, object?> ToRow(Movie movie) => new()
    {
        ["id"] = (long)movie.Id,
        ["title"] = movie.Title,
        ["duration"] = (long?)movie.Duration,
        ["releaseDate"] = IsoDate(movie.ReleaseDate),
        ["resume"] = movie.Resume,
        ["language"] = movie.Language,
        ["ageRating"] = movie.AgeRating,
    };

    public static Dictionary<string, object?> ToRow(Series series) => new()
    {
        ["id"] = (long)series.Id,
        ["title"] = series.Title,
        ["releaseDate"] = IsoDate(series.ReleaseDate),
        ["resume"] = series.Resume,
        ["language"] = series.Language,
        ["ageRating"] = series.AgeRating,
        ["seasons"] = (long?)series.Seasons,
        ["episodeCount"] = (long?)series.Episodes,
    };

    public static Dictionary<string, object?> ToRow(Rating rating) => new()
    {
        ["id"] = (long)rating.Id,
        ["score"] = (double)rating.Score, // Neo4j har ingen decimal-type
        ["createdAt"] = IsoDateTime(rating.CreatedAt),
        // Bruges kun til edges: (User)-[:GAVE]->(Rating)-[:RATES]->(Movie|Series)
        ["userId"] = (long)rating.UserId,
        ["movieId"] = (long?)rating.MovieId,
        ["seriesId"] = (long?)rating.SeriesId,
    };

    public static Dictionary<string, object?> ToRow(Review review) => new()
    {
        ["id"] = (long)review.Id,
        ["content"] = review.Content,
        ["createdAt"] = IsoDateTime(review.CreatedAt),
        ["ratingId"] = (long)review.RatingId, // kun til edgen (Rating)-[:HAS_REVIEW]->(Review)
    };

    public static Dictionary<string, object?> ToRow(UserActivity activity) => new()
    {
        ["id"] = (long)activity.Id,
        ["activityType"] = activity.ActivityType,
        ["createdAt"] = IsoDateTime(activity.ActivityAt),
        ["archivedAt"] = null,
        ["userId"] = (long)activity.UserId, // kun til edgen (User)-[:PERFORMED]->(UserActivity)
    };

    public static Dictionary<string, object?> ToRow(UserActivityArchive activity) => new()
    {
        ["id"] = (long)activity.Id,
        ["activityType"] = activity.ActivityType,
        ["createdAt"] = IsoDateTime(activity.ActivityAt),
        ["archivedAt"] = IsoDateTime(activity.ArchivedAt),
        ["userId"] = (long)activity.UserId,
    };

    /// <summary>{ from, to } – én edge mellem to noder ud fra deres id.</summary>
    public static Dictionary<string, object?> Edge(uint fromId, uint toId) => new()
    {
        ["from"] = (long)fromId,
        ["to"] = (long)toId,
    };

    private static Dictionary<string, object?> Named(uint id, string name) => new()
    {
        ["id"] = (long)id,
        ["name"] = name,
    };

    private static Dictionary<string, object?> Person(uint id, string name, DateOnly? dateOfBirth, string? biography) => new()
    {
        ["id"] = (long)id,
        ["name"] = name,
        ["dateOfBirth"] = IsoDate(dateOfBirth),
        ["biography"] = biography,
    };

    private static string? IsoDate(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // MySQL kører i UTC, så værdien markeres med Z (UTC) for Neo4j's datetime().
    private static string IsoDateTime(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
