using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;
using RottenPotatoes.Migrator.Interfaces;

namespace RottenPotatoes.Migrator.Services;

public class MySqlDataReader : IMySqlDataReader
{
    private readonly RottenPotatoesDbContext _context;

    public MySqlDataReader(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    public Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
        => _context.Users.AsNoTracking().Include(u => u.Role).OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public Task<List<Actor>> GetActorsAsync(CancellationToken cancellationToken = default)
        => _context.Actors.AsNoTracking().OrderBy(a => a.Id).ToListAsync(cancellationToken);

    public Task<List<Director>> GetDirectorsAsync(CancellationToken cancellationToken = default)
        => _context.Directors.AsNoTracking().OrderBy(d => d.Id).ToListAsync(cancellationToken);

    public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken = default)
        => _context.Genres.AsNoTracking().OrderBy(g => g.Id).ToListAsync(cancellationToken);

    public Task<List<Award>> GetAwardsAsync(CancellationToken cancellationToken = default)
        => _context.Awards.AsNoTracking().OrderBy(a => a.Id).ToListAsync(cancellationToken);

    public Task<List<StreamingService>> GetStreamingServicesAsync(CancellationToken cancellationToken = default)
        => _context.StreamingServices.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public Task<List<Movie>> GetMoviesWithRelationsAsync(CancellationToken cancellationToken = default)
        => _context.Movies
            .AsNoTracking()
            .Include(m => m.Genres)
            .Include(m => m.Actors)
            .Include(m => m.Directors)
            .Include(m => m.Awards)
            .Include(m => m.StreamingServices)
            // Én SQL-query pr. Include i stedet for ét stort JOIN, der ganger rækkerne op.
            .AsSplitQuery()
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

    public Task<List<Series>> GetSeriesWithRelationsAsync(CancellationToken cancellationToken = default)
        => _context.Series
            .AsNoTracking()
            .Include(s => s.Genres)
            .Include(s => s.Actors)
            .Include(s => s.Directors)
            .Include(s => s.Awards)
            .Include(s => s.StreamingServices)
            .AsSplitQuery()
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

    public Task<Dictionary<uint, decimal>> GetMovieAverageRatingsAsync(CancellationToken cancellationToken = default)
        => _context.Ratings
            .Where(r => r.MovieId != null)
            .GroupBy(r => r.MovieId!.Value)
            .Select(g => new { Id = g.Key, Average = Math.Round(g.Average(r => r.Score), 2) })
            .ToDictionaryAsync(x => x.Id, x => x.Average, cancellationToken);

    public Task<Dictionary<uint, decimal>> GetSeriesAverageRatingsAsync(CancellationToken cancellationToken = default)
        => _context.Ratings
            .Where(r => r.SeriesId != null)
            .GroupBy(r => r.SeriesId!.Value)
            .Select(g => new { Id = g.Key, Average = Math.Round(g.Average(r => r.Score), 2) })
            .ToDictionaryAsync(x => x.Id, x => x.Average, cancellationToken);

    public async IAsyncEnumerable<List<Rating>> GetRatingsWithReviewsInBatchesAsync(
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Keyset-pagination: "giv mig de næste N efter sidste Id". Bliver ikke langsommere
        // langt nede i tabellen, som OFFSET ville.
        ulong lastId = 0;

        while (true)
        {
            var batch = await _context.Ratings
                .AsNoTracking()
                .Include(r => r.Review)
                .Where(r => r.Id > lastId)
                .OrderBy(r => r.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                yield break;
            }

            yield return batch;
            lastId = batch[^1].Id;
        }
    }

    public Task<List<UserActivity>> GetUserActivitiesAsync(CancellationToken cancellationToken = default)
        => _context.UserActivities.AsNoTracking().OrderBy(a => a.Id).ToListAsync(cancellationToken);

    public Task<List<UserActivityArchive>> GetArchivedUserActivitiesAsync(CancellationToken cancellationToken = default)
        => _context.UserActivityArchives.AsNoTracking().OrderBy(a => a.Id).ToListAsync(cancellationToken);
}
