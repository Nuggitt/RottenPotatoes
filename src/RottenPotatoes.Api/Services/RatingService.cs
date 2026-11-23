using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

/// <summary>
/// Ratings oprettes med de stored procedures AddMovieRating/AddSeriesRating. De validerer
/// score, bruger, titel og dubletter og indsætter rækken. Parametrene i de interpolerede
/// SQL-strenge sendes som MySQL-parametre (ingen strengsammensætning -> ingen SQL injection).
/// </summary>
public class RatingService : IRatingService
{
    private readonly RottenPotatoesDbContext _context;

    public RatingService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    public Task<ServiceResult<RatingDto>> RateMovieAsync(ulong userId, uint movieId, decimal score, string? review, CancellationToken cancellationToken = default)
        => AddRatingAsync(
            $"SELECT Id FROM Movies WHERE Id = {movieId} FOR UPDATE",
            $"CALL AddMovieRating({userId}, {movieId}, {Round(score)})",
            review,
            cancellationToken);

    public Task<ServiceResult<RatingDto>> RateSeriesAsync(ulong userId, uint seriesId, decimal score, string? review, CancellationToken cancellationToken = default)
        => AddRatingAsync(
            $"SELECT Id FROM Series WHERE Id = {seriesId} FOR UPDATE",
            $"CALL AddSeriesRating({userId}, {seriesId}, {Round(score)})",
            review,
            cancellationToken);

    /// <summary>
    /// Man må kun skrive review på sin egen rating. Triggeren logger "Review" ved INSERT.
    /// Et samtidigt review på samme rating afvises af UQ_Reviews_RatingId (DbUpdateException -> Conflict).
    /// </summary>
    public async Task<ServiceResult<ReviewDto>> AddReviewAsync(ulong userId, ulong ratingId, string content, CancellationToken cancellationToken = default)
    {
        var rating = await _context.Ratings.FindAsync([ratingId], cancellationToken);

        if (rating is null)
        {
            return ServiceResult<ReviewDto>.NotFound();
        }

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
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
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

    /// <summary>
    /// Procedurernes SIGNAL SQLSTATE '45000'-fejl -> HTTP-udfald. "User does not exist" betyder,
    /// at brugeren i tokenet er slettet. "Score must be between ..." er ikke med: [Range] på
    /// CreateRatingDto afviser den med 400, før proceduren kaldes.
    /// </summary>
    private static readonly Dictionary<string, ServiceStatus> ProcedureErrors = new()
    {
        ["Movie does not exist"] = ServiceStatus.NotFound,
        ["Series does not exist"] = ServiceStatus.NotFound,
        ["User does not exist"] = ServiceStatus.NotFound,
        ["User has already rated this movie"] = ServiceStatus.Conflict,
        ["User has already rated this series"] = ServiceStatus.Conflict,
    };

    /// <summary>
    /// Én transaktion pr. rating:
    ///   1. Lås titlens række (SELECT ... FOR UPDATE). Låsen holdes til Commit/Rollback, så ratings
    ///      af samme titel kører én ad gangen. Uden den gav 100 samtidige ratings af samme film
    ///      31 deadlocks (MySQL-fejl 1213), og den gør procedurens dublet-tjek pålideligt. Den skal
    ///      være første statement, så snapshottet (REPEATABLE READ) først oprettes efter låsen.
    ///   2. Opret ratingen med proceduren (triggers opdaterer gennemsnit og logger aktivitet).
    ///      Kun ToListAsync, fordi First/Single ville lægge CALL i en subquery, hvilket MySQL ikke kan.
    ///   3. Gem reviewet i samme transaktion, hvis der er et.
    ///   4. Commit.
    /// Returneres der før Commit, eller kastes en exception, ruller "await using" transaktionen
    /// tilbage, så der aldrig gemmes en rating uden det review, brugeren sendte med.
    /// </summary>
    private async Task<ServiceResult<RatingDto>> AddRatingAsync(
        FormattableString lockTitle,
        FormattableString procedureCall,
        string? review,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Database.ExecuteSqlAsync(lockTitle, cancellationToken);

        ulong ratingId;

        try
        {
            ratingId = (await _context.Database.SqlQuery<RatingIdRow>(procedureCall).ToListAsync(cancellationToken)).Single().RatingId;
        }
        catch (MySqlException ex) when (ex.SqlState == "45000" && ProcedureErrors.ContainsKey(ex.Message))
        {
            return new ServiceResult<RatingDto>(ProcedureErrors[ex.Message]);
        }

        ReviewDto? reviewDto = null;

        if (review is not null)
        {
            var entity = new Review { RatingId = ratingId, Content = review.Trim() };
            _context.Reviews.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            reviewDto = new ReviewDto
            {
                Id = entity.Id,
                RatingId = entity.RatingId,
                Content = entity.Content,
                CreatedAt = entity.CreatedAt,
            };
        }

        var rating = await _context.Ratings
            .AsNoTracking()
            .Where(r => r.Id == ratingId)
            .Select(r => new RatingDto
            {
                Id = r.Id,
                UserId = r.UserId,
                MovieId = r.MovieId,
                SeriesId = r.SeriesId,
                Score = r.Score,
                CreatedAt = r.CreatedAt,
            })
            .SingleAsync(cancellationToken);

        rating.Review = reviewDto;

        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<RatingDto>.Ok(rating);
    }

    /// <summary>Kolonnen Ratings.Score er decimal(3,1): én decimal.</summary>
    private static decimal Round(decimal score) => Math.Round(score, 1, MidpointRounding.AwayFromZero);

    /// <summary>Resultatet af procedurernes "SELECT LAST_INSERT_ID() AS RatingId".</summary>
    private sealed class RatingIdRow
    {
        public ulong RatingId { get; set; }
    }
}
