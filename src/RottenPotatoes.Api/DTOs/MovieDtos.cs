using System.ComponentModel.DataAnnotations;

namespace RottenPotatoes.Api.DTOs;

/// <summary>Film som den returneres fra API'et. Entities eksponeres aldrig direkte.</summary>
public class MovieDto
{
    public uint Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Spilletid i minutter.</summary>
    public ushort? Duration { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    public string? Language { get; set; }

    public string? AgeRating { get; set; }

    /// <summary>Beregnes af triggeren AI_Ratings_UpdateMovieAverage.</summary>
    public decimal AverageRating { get; set; }

    public List<GenreDto> Genres { get; set; } = [];
}

/// <summary>Række fra GET /api/movies/top (resultatet af proceduren GetTopRatedMovies).</summary>
public class TopRatedMovieDto
{
    public uint MovieId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Beregnes af functionen GetAverageMovieRating (via viewet MovieRatingOverview).</summary>
    public decimal AverageRating { get; set; }

    public long NumberOfRatings { get; set; }
}

/// <summary>
/// Body til POST /api/movies. Kun de felter, brugeren må sætte: AverageRating (trigger),
/// CreatedAt og UpdatedAt sætter MySQL selv. Grænserne matcher kolonnerne i Movies.
/// </summary>
public class CreateMovieDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Spilletid i minutter (skal være over 0).</summary>
    [Range(1, ushort.MaxValue)]
    public ushort? Duration { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    [StringLength(50)]
    public string? Language { get; set; }

    [StringLength(20)]
    public string? AgeRating { get; set; }
}

/// <summary>Body til PUT /api/movies/{id}. Samme felter som ved oprettelse (PUT erstatter hele filmen).</summary>
public class UpdateMovieDto : CreateMovieDto
{
}
