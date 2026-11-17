using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Services;

namespace RottenPotatoes.Api.Interfaces;

/// <summary>
/// Ratings og reviews fra den indloggede bruger. Databasens triggers opdaterer
/// Movies.AverageRating og logger "Rating"/"Review" i UserActivity.
/// </summary>
public interface IRatingService
{
    /// <summary>NotFound: filmen findes ikke. Conflict: brugeren har allerede ratet filmen.</summary>
    Task<ServiceResult<RatingDto>> RateMovieAsync(ulong userId, uint movieId, decimal score, CancellationToken cancellationToken = default);

    /// <summary>NotFound: serien findes ikke. Conflict: brugeren har allerede ratet serien.</summary>
    Task<ServiceResult<RatingDto>> RateSeriesAsync(ulong userId, uint seriesId, decimal score, CancellationToken cancellationToken = default);

    /// <summary>NotFound: ratingen findes ikke. Forbidden: ikke brugerens egen rating. Conflict: har allerede et review.</summary>
    Task<ServiceResult<ReviewDto>> AddReviewAsync(ulong userId, ulong ratingId, string content, CancellationToken cancellationToken = default);
}
