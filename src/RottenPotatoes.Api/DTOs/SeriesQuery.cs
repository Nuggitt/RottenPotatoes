namespace RottenPotatoes.Api.DTOs;

/// <summary>
/// Query-parametre til GET /api/series.
/// SortBy: title, releaseDate, seasons eller averageRating (ellers sorteres efter id).
/// </summary>
public class SeriesQuery : PaginationQuery
{
    /// <summary>Filtrér på en del af titlen (ikke forskel på store/små bogstaver).</summary>
    public string? Title { get; set; }

    /// <summary>Filtrér på genre-id (se tabellen Genres).</summary>
    public ushort? GenreId { get; set; }

    /// <summary>Filtrér på sprog, fx English.</summary>
    public string? Language { get; set; }
}
