using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Migrator.Interfaces;

/// <summary>
/// Læser data fra MySQL via EF Core (svarer til fetchDataFromMysql.ts i underviserens projekt).
/// Alt læses med AsNoTracking, da migratoren kun læser.
/// </summary>
public interface IMySqlDataReader
{
    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<List<Actor>> GetActorsAsync(CancellationToken cancellationToken = default);

    Task<List<Director>> GetDirectorsAsync(CancellationToken cancellationToken = default);

    Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default);

    Task<List<Award>> GetAwardsAsync(CancellationToken cancellationToken = default);

    Task<List<StreamingService>> GetStreamingServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Film inkl. genrer, skuespillere, instruktører, priser og streamingtjenester.</summary>
    Task<List<Movie>> GetMoviesWithRelationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Serier inkl. genrer, skuespillere, instruktører, priser og streamingtjenester.</summary>
    Task<List<Series>> GetSeriesWithRelationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gennemsnitlig score pr. film (afrundet til 2 decimaler). Film uden ratings er ikke med.</summary>
    Task<Dictionary<uint, decimal>> GetMovieAverageRatingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gennemsnitlig score pr. serie (afrundet til 2 decimaler). Serier uden ratings er ikke med.</summary>
    Task<Dictionary<uint, decimal>> GetSeriesAverageRatingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Ratings inkl. review, i bidder sorteret efter Id (der er 500.000 ratings).</summary>
    IAsyncEnumerable<List<Rating>> GetRatingsWithReviewsInBatchesAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<List<UserActivity>> GetUserActivitiesAsync(CancellationToken cancellationToken = default);

    Task<List<UserActivityArchive>> GetArchivedUserActivitiesAsync(CancellationToken cancellationToken = default);
}
