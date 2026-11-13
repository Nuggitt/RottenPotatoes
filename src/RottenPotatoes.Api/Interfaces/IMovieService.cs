using RottenPotatoes.Api.DTOs;

namespace RottenPotatoes.Api.Interfaces;

/// <summary>CRUD, pagination, filtrering og sortering for Movies.</summary>
public interface IMovieService
{
    Task<PagedResult<MovieDto>> GetMoviesAsync(PaginationQuery query, CancellationToken cancellationToken = default);

    Task<MovieDto?> GetMovieByIdAsync(uint id, CancellationToken cancellationToken = default);

    Task<MovieDto> CreateMovieAsync(CreateMovieDto dto, CancellationToken cancellationToken = default);

    Task<bool> UpdateMovieAsync(uint id, UpdateMovieDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteMovieAsync(uint id, CancellationToken cancellationToken = default);
}
