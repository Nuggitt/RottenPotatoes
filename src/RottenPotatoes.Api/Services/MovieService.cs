using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;
using RottenPotatoes.Data.Entities;

namespace RottenPotatoes.Api.Services;

public class MovieService : IMovieService
{
    private readonly RottenPotatoesDbContext _context;

    public MovieService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    // EF Core oversætter LINQ til SQL med parametre (fx WHERE m.Id = @id) -> ingen SQL injection.
    // Brug kun FromSql/ExecuteSql med interpolerede parametre – aldrig strengsammensætning.

    // Movie -> MovieDto. Bruges i Select, så SQL kun henter de kolonner, DTO'en skal bruge.
    private static readonly Expression<Func<Movie, MovieDto>> ToDto = m => new MovieDto
    {
        Id = m.Id,
        Title = m.Title,
        Duration = m.Duration,
        ReleaseDate = m.ReleaseDate,
        Resume = m.Resume,
        Language = m.Language,
        AgeRating = m.AgeRating,
        AverageRating = m.AverageRating,
    };

    public async Task<PagedResult<MovieDto>> GetMoviesAsync(MovieQuery query, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: vi læser kun, så EF behøver ikke holde øje med ændringer.
        IQueryable<Movie> movies = _context.Movies.AsNoTracking();

        // 1. Filtrering – hvert filter bruges kun, hvis det er udfyldt.
        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            movies = movies.Where(m => m.Title.Contains(query.Title));
        }

        if (query.GenreId is not null)
        {
            movies = movies.Where(m => m.Genres.Any(g => g.Id == query.GenreId));
        }

        if (!string.IsNullOrWhiteSpace(query.Language))
        {
            movies = movies.Where(m => m.Language == query.Language);
        }

        // 2. Totalantal efter filtrering (før pagination).
        var totalCount = await movies.CountAsync(cancellationToken);

        // 3. Sortering – kun faste felter, så brugerens tekst aldrig kommer ind i SQL.
        IOrderedQueryable<Movie> Sort<TKey>(Expression<Func<Movie, TKey>> key) =>
            query.SortDescending ? movies.OrderByDescending(key) : movies.OrderBy(key);

        var sorted = query.SortBy?.ToLowerInvariant() switch
        {
            "title" => Sort(m => m.Title),
            "releasedate" => Sort(m => m.ReleaseDate),
            "duration" => Sort(m => m.Duration),
            "averagerating" => Sort(m => m.AverageRating),
            _ => Sort(m => m.Id),
        };

        // 4. Pagination. ThenBy(Id) giver en stabil rækkefølge, når flere film har samme værdi.
        var items = await sorted
            .ThenBy(m => m.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<MovieDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<MovieDto?> GetMovieByIdAsync(uint id, CancellationToken cancellationToken = default)
    {
        return await _context.Movies
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<MovieDto> CreateMovieAsync(CreateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opret film.");

    public Task<bool> UpdateMovieAsync(uint id, UpdateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opdatér film.");

    public Task<bool> DeleteMovieAsync(uint id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: slet film.");
}
