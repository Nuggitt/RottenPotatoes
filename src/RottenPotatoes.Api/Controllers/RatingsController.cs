using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Api.Security;
using RottenPotatoes.Api.Services;

namespace RottenPotatoes.Api.Controllers;

/// <summary>Ratings og reviews. Kræver login (User eller Admin); brugeren tages fra tokenet.</summary>
[ApiController]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    /// <summary>Giver en film en rating (0–10). Én rating pr. bruger pr. film.</summary>
    [HttpPost("api/movies/{movieId}/ratings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RatingDto>> RateMovie(uint movieId, CreateRatingDto dto, CancellationToken cancellationToken)
        => ToActionResult(await _ratingService.RateMovieAsync(User.GetUserId(), movieId, dto.Score, cancellationToken),
            conflictTitle: "Du har allerede ratet denne film.");

    /// <summary>Giver en serie en rating (0–10). Én rating pr. bruger pr. serie.</summary>
    [HttpPost("api/series/{seriesId}/ratings")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RatingDto>> RateSeries(uint seriesId, CreateRatingDto dto, CancellationToken cancellationToken)
        => ToActionResult(await _ratingService.RateSeriesAsync(User.GetUserId(), seriesId, dto.Score, cancellationToken),
            conflictTitle: "Du har allerede ratet denne serie.");

    /// <summary>Skriver et review på din egen rating. Ét review pr. rating.</summary>
    [HttpPost("api/ratings/{ratingId}/review")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> AddReview(ulong ratingId, CreateReviewDto dto, CancellationToken cancellationToken)
        => ToActionResult(await _ratingService.AddReviewAsync(User.GetUserId(), ratingId, dto.Content, cancellationToken),
            conflictTitle: "Ratingen har allerede et review.");

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result, string conflictTitle) => result.Status switch
    {
        ServiceStatus.Ok => StatusCode(StatusCodes.Status201Created, result.Value),
        ServiceStatus.NotFound => NotFound(),
        ServiceStatus.Forbidden => Forbid(),
        ServiceStatus.Conflict => Problem(statusCode: StatusCodes.Status409Conflict, title: conflictTitle),
        _ => throw new InvalidOperationException($"Ukendt status: {result.Status}"),
    };
}
