using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

public class RatingService : IRatingService
{
    private readonly RottenPotatoesDbContext _context;

    public RatingService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<RatingDto>> RateMovieAsync(ulong userId, uint movieId, decimal score, CancellationToken cancellationToken = default)
    {
        if (!await _context.Movies.AnyAsync(m => m.Id == movieId, cancellationToken))
        {
            return ServiceResult<RatingDto>.NotFound();
        }

        if (await _context.Ratings.AnyAsync(r => r.UserId == userId && r.MovieId == movieId, cancellationToken))
        {
            return ServiceResult<RatingDto>.Conflict();
        }

        return await SaveRatingAsync(new Rating { UserId = userId, MovieId = movieId, Score = Round(score) }, cancellationToken);
    }

    public async Task<ServiceResult<RatingDto>> RateSeriesAsync(ulong userId, uint seriesId, decimal score, CancellationToken cancellationToken = default)
    {
        if (!await _context.Series.AnyAsync(s => s.Id == seriesId, cancellationToken))
        {
            return ServiceResult<RatingDto>.NotFound();
        }

        if (await _context.Ratings.AnyAsync(r => r.UserId == userId && r.SeriesId == seriesId, cancellationToken))
        {
            return ServiceResult<RatingDto>.Conflict();
        }

        return await SaveRatingAsync(new Rating { UserId = userId, SeriesId = seriesId, Score = Round(score) }, cancellationToken);
    }

    public async Task<ServiceResult<ReviewDto>> AddReviewAsync(ulong userId, ulong ratingId, string content, CancellationToken cancellationToken = default)
    {
        var rating = await _context.Ratings.FindAsync([ratingId], cancellationToken);

        if (rating is null)
        {
            return ServiceResult<ReviewDto>.NotFound();
        }

        // Man må kun skrive review på sin egen rating.
        if (rating.UserId != userId)
        {
            return ServiceResult<ReviewDto>.Forbidden();
        }

        if (await _context.Reviews.AnyAsync(r => r.RatingId == ratingId, cancellationToken))
        {
            return ServiceResult<ReviewDto>.Conflict();
        }

        var review = new Review { RatingId = ratingId, Content = content.Trim() };
        _context.Reviews.Add(review);

        try
        {
            await _context.SaveChangesAsync(cancellationToken); // trigger logger "Review"
        }
        catch (DbUpdateException)
        {
            // Samtidigt review på samme rating: UQ_Reviews_RatingId afgør det.
            return ServiceResult<ReviewDto>.Conflict();
        }

        return ServiceResult<ReviewDto>.Ok(new ReviewDto
        {
            Id = review.Id,
            RatingId = review.RatingId,
            Content = review.Content,
            CreatedAt = review.CreatedAt,
        });
    }

    private async Task<ServiceResult<RatingDto>> SaveRatingAsync(Rating rating, CancellationToken cancellationToken)
    {
        _context.Ratings.Add(rating);

        try
        {
            // Triggers: AI_Ratings_UpdateMovieAverage og AI_Ratings_LogUserActivity
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Samtidig rating af samme titel: UQ_Ratings_User_Movie/Series afgør det.
            return ServiceResult<RatingDto>.Conflict();
        }

        return ServiceResult<RatingDto>.Ok(new RatingDto
        {
            Id = rating.Id,
            UserId = rating.UserId,
            MovieId = rating.MovieId,
            SeriesId = rating.SeriesId,
            Score = rating.Score,
            CreatedAt = rating.CreatedAt,
        });
    }

    // Kolonnen er decimal(3,1): én decimal.
    private static decimal Round(decimal score) => Math.Round(score, 1, MidpointRounding.AwayFromZero);
}
