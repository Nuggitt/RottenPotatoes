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

    public Task<MovieDto?> GetMovieByIdAsync(uint id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: hent film efter id.");

    public Task<MovieDto> CreateMovieAsync(CreateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opret film.");

    public Task<bool> UpdateMovieAsync(uint id, UpdateMovieDto dto, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: opdatér film.");

    public Task<bool> DeleteMovieAsync(uint id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("TODO: slet film.");
}
