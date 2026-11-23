using System.ComponentModel.DataAnnotations;

namespace RottenPotatoes.Api.DTOs;

/// <summary>Serie som den returneres fra API'et. Entities eksponeres aldrig direkte.</summary>
public class SeriesDto
{
    public uint Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    public string? Language { get; set; }

    public string? AgeRating { get; set; }

    /// <summary>Antal sæsoner.</summary>
    public ushort? Seasons { get; set; }

    /// <summary>Antal afsnit i alt.</summary>
    public ushort? Episodes { get; set; }

    /// <summary>
    /// Gennemsnit af seriens ratings. Series har ingen AverageRating-kolonne,
    /// så værdien beregnes ud fra Ratings, når serien hentes (0 hvis ingen ratings).
    /// </summary>
    public decimal AverageRating { get; set; }

    public List<GenreDto> Genres { get; set; } = [];
}

/// <summary>
/// Body til POST /api/series. Kun de felter, brugeren må sætte: CreatedAt og UpdatedAt sætter MySQL.
/// Grænserne matcher kolonnerne og CHECK-constraints i Series.
/// </summary>
public class CreateSeriesDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    [StringLength(50)]
    public string? Language { get; set; }

    [StringLength(20)]
    public string? AgeRating { get; set; }

    /// <summary>Antal sæsoner (skal være over 0).</summary>
    [Range(1, ushort.MaxValue)]
    public ushort? Seasons { get; set; }

    /// <summary>Antal afsnit i alt (0 eller mere).</summary>
    public ushort? Episodes { get; set; }
}

/// <summary>Body til PUT /api/series/{id}. Samme felter som ved oprettelse (PUT erstatter hele serien).</summary>
public class UpdateSeriesDto : CreateSeriesDto
{
}
