using Microsoft.EntityFrameworkCore;
using RottenPotatoes.Api.DTOs;
using RottenPotatoes.Api.Interfaces;
using RottenPotatoes.Data.Context;

namespace RottenPotatoes.Api.Services;

public class MovieService : IMovieService
{
    private readonly RottenPotatoesDbContext _context;

    public MovieService(RottenPotatoesDbContext context)
    {
        _context = context;
    }

    // TODO: Implementér med EF Core (LINQ giver parameteriserede queries -> beskytter mod SQL injection).
    // Brug kun FromSql/ExecuteSql med interpolerede parametre – aldrig strengsammensætning.

    public Task<PagedResult<MovieDto>> GetMoviesAsync(PaginationQuery query, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: pagination, filtrering og sortering af film.");

    public async Task<MovieDto?> GetMovieByIdAsync(uint id, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: vi læser kun, så EF behøver ikke holde øje med ændringer.
        // Select: projektion direkte til DTO, så SQL kun henter de kolonner, vi bruger.
        // id sendes som SQL-parameter (WHERE m.Id = @id) -> ingen SQL injection.
        return await _context.Movies
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MovieDto
            {
                Id = m.Id,
                Title = m.Title,
                Duration = m.Duration,
                ReleaseDate = m.ReleaseDate,
                Resume = m.Resume,
                Language = m.Language,
                AgeRating = m.AgeRating,
                AverageRating = m.AverageRating,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<MovieDto> CreateMovieAsync(CreateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opret film.");

    public Task<bool> UpdateMovieAsync(uint id, UpdateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opdatér film.");

    public Task<bool> DeleteMovieAsync(uint id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: slet film.");
}
