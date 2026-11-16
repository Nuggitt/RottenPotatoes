using RottenPotatoes.Data.Entities;
using RottenPotatoes.Migrator.Models.Mongo;

namespace RottenPotatoes.Migrator.Mappings;

/// <summary>
/// MySQL-entities -> MongoDB-dokumenter efter docs/database-mapping.md.
/// Id'er gemmes som int (som i designet); checked giver en fejl i stedet for et forkert tal,
/// hvis et id en dag bliver for stort.
/// </summary>
public static class MongoMappings
{
    public static UserDocument ToDocument(User user) => new()
    {
        Id = checked((int)user.Id),
        Username = user.Username,
        Email = user.Email,
        PasswordHash = user.PasswordHash,
        CreatedAt = Utc(user.CreatedAt),
        UpdatedAt = Utc(user.UpdatedAt),
    };

    public static MovieDocument ToDocument(Movie movie, decimal averageRating) => new()
    {
        Id = checked((int)movie.Id),
        Title = movie.Title,
        Duration = movie.Duration,
        ReleaseDate = Utc(movie.ReleaseDate),
        Resume = movie.Resume,
        Language = movie.Language,
        AgeRating = movie.AgeRating,
        AverageRating = averageRating,
        CreatedAt = Utc(movie.CreatedAt),
        UpdatedAt = Utc(movie.UpdatedAt),
        Genres = movie.Genres.OrderBy(g => g.Id).Select(g => Named(g.Id, g.Name)).ToList(),
        Actors = movie.Actors.OrderBy(a => a.Id).Select(a => Person(a.Id, a.Name, a.DateOfBirth, a.Biography)).ToList(),
        Directors = movie.Directors.OrderBy(d => d.Id).Select(d => Person(d.Id, d.Name, d.DateOfBirth, d.Biography)).ToList(),
        Awards = movie.Awards.OrderBy(a => a.Id).Select(a => Named(a.Id, a.Name)).ToList(),
        StreamingServices = movie.StreamingServices.OrderBy(s => s.Id).Select(s => Named(s.Id, s.Name)).ToList(),
    };

    public static SeriesDocument ToDocument(Series series, decimal averageRating) => new()
    {
        Id = checked((int)series.Id),
        Title = series.Title,
        ReleaseDate = Utc(series.ReleaseDate),
        Resume = series.Resume,
        Language = series.Language,
        AgeRating = series.AgeRating,
        AverageRating = averageRating,
        CreatedAt = Utc(series.CreatedAt),
        UpdatedAt = Utc(series.UpdatedAt),
        Genres = series.Genres.OrderBy(g => g.Id).Select(g => Named(g.Id, g.Name)).ToList(),
        // Designet har kun { id, name } for skuespillere og instruktører på serier.
        Actors = series.Actors.OrderBy(a => a.Id).Select(a => Named(a.Id, a.Name)).ToList(),
        Directors = series.Directors.OrderBy(d => d.Id).Select(d => Named(d.Id, d.Name)).ToList(),
        Awards = series.Awards.OrderBy(a => a.Id).Select(a => Named(a.Id, a.Name)).ToList(),
        StreamingServices = series.StreamingServices.OrderBy(s => s.Id).Select(s => Named(s.Id, s.Name)).ToList(),
    };

    public static RatingDocument ToDocument(Rating rating) => new()
    {
        Id = checked((int)rating.Id),
        UserId = checked((int)rating.UserId),
        // CK_Ratings_ExactlyOneTarget garanterer, at præcis én af MovieId/SeriesId er sat.
        Target = rating.MovieId is { } movieId
            ? new RatingTarget { Type = "movie", Id = checked((int)movieId) }
            : new RatingTarget { Type = "series", Id = checked((int)rating.SeriesId!.Value) },
        Score = rating.Score,
        CreatedAt = Utc(rating.CreatedAt),
        Review = rating.Review is null
            ? null
            : new EmbeddedReview
            {
                Id = checked((int)rating.Review.Id),
                Content = rating.Review.Content,
                CreatedAt = Utc(rating.Review.CreatedAt),
            },
    };

    public static UserActivityDocument ToDocument(UserActivity activity) => new()
    {
        Id = checked((int)activity.Id),
        UserId = checked((int)activity.UserId),
        ActivityType = activity.ActivityType,
        CreatedAt = Utc(activity.ActivityAt),
        ArchivedAt = null,
    };

    public static UserActivityDocument ToDocument(UserActivityArchive activity) => new()
    {
        Id = checked((int)activity.Id),
        UserId = checked((int)activity.UserId),
        ActivityType = activity.ActivityType,
        CreatedAt = Utc(activity.ActivityAt),
        ArchivedAt = Utc(activity.ArchivedAt),
    };

    private static NamedReference Named(uint id, string name) => new() { Id = checked((int)id), Name = name };

    private static PersonReference Person(uint id, string name, DateOnly? dateOfBirth, string? biography) => new()
    {
        Id = checked((int)id),
        Name = name,
        DateOfBirth = Utc(dateOfBirth),
        Biography = biography,
    };

    // MySQL-datetime har ingen tidszone; databasen kører i UTC, så værdierne markeres som UTC.
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? Utc(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
