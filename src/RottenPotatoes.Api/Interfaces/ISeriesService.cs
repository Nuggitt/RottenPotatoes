using RottenPotatoes.Api.DTOs;

namespace RottenPotatoes.Api.Interfaces;

/// <summary>CRUD, pagination, filtrering og sortering for Series.</summary>
public interface ISeriesService
{
    Task<PagedResult<SeriesDto>> GetSeriesAsync(SeriesQuery query, CancellationToken cancellationToken = default);

    Task<SeriesDto?> GetSeriesByIdAsync(uint id, CancellationToken cancellationToken = default);

    Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto, CancellationToken cancellationToken = default);

    Task<bool> UpdateSeriesAsync(uint id, UpdateSeriesDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteSeriesAsync(uint id, CancellationToken cancellationToken = default);
}
