using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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

    // Ratings oprettes med de stored procedures AddMovieRating/AddSeriesRating. De validerer
    // score, bruger, titel og dubletter og indsætter rækken. Parametrene i de interpolerede
    // SQL-strenge sendes som MySQL-parametre (ingen strengsammensætning -> ingen SQL injection).
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

    // Procedurernes SIGNAL SQLSTATE '45000'-fejl -> HTTP-udfald. "Score must be between ..."
    // er ikke med: [Range] på CreateRatingDto afviser den med 400, før proceduren kaldes.
    private static readonly Dictionary<string, ServiceStatus> ProcedureErrors = new()
    {
        ["Movie does not exist"] = ServiceStatus.NotFound,
        ["Series does not exist"] = ServiceStatus.NotFound,
        ["User does not exist"] = ServiceStatus.NotFound, // brugeren i tokenet er slettet
        ["User has already rated this movie"] = ServiceStatus.Conflict,
        ["User has already rated this series"] = ServiceStatus.Conflict,
    };

    // Én transaktion pr. rating:
    //   1. Lås titlens række (SELECT ... FOR UPDATE).
    //   2. Opret ratingen med proceduren. Triggers: AI_Ratings_UpdateMovieAverage og AI_Ratings_LogUserActivity.
    //   3. Gem reviewet, hvis der er et. Trigger: AI_Reviews_LogUserActivity.
    //   4. Commit.
    // Returneres der før Commit, eller kastes en exception, ruller "await using" transaktionen
    // tilbage, så der aldrig gemmes en rating uden det review, brugeren sendte med.
    private async Task<ServiceResult<RatingDto>> AddRatingAsync(
        FormattableString lockTitle,
        FormattableString procedureCall,
        string? review,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Låsen holdes til Commit/Rollback, så ratings af samme titel kører én ad gangen.
        // Uden den gav 100 samtidige ratings af samme film 31 deadlocks (MySQL-fejl 1213):
        // triggeren låser filmens række, og AVG i GetAverageMovieRating venter på de andre
        // transaktioners nye ratings. Låsen gør også procedurens dublet-tjek pålideligt.
        // Den skal tages før procedurens første SELECT, så transaktionens snapshot (REPEATABLE
        // READ) først oprettes, når låsen er fået, og ratings committet imens er med.
        await _context.Database.ExecuteSqlAsync(lockTitle, cancellationToken);

        ulong ratingId;

        try
        {
            // ToListAsync uden andre LINQ-operatorer, så EF sender CALL uændret. First/Single
            // ville lægge den ind i en subquery, og CALL kan ikke stå i en subquery.
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
            await _context.SaveChangesAsync(cancellationToken); // samme transaktion som ratingen

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

    // Resultatet af procedurernes "SELECT LAST_INSERT_ID() AS RatingId".
    private sealed class RatingIdRow
    {
        public ulong RatingId { get; set; }
    }

    // Kolonnen er decimal(3,1): én decimal.
    private static decimal Round(decimal score) => Math.Round(score, 1, MidpointRounding.AwayFromZero);
}
