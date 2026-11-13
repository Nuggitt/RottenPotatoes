namespace RottenPotatoes.Api.DTOs;

// Eksponér aldrig entities direkte fra API'et – map dem til DTO'er.

/// <summary>Film som den returneres fra API'et.</summary>
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
}

// TODO: Felter til Create/Update. Udelad AverageRating, CreatedAt og UpdatedAt – dem sætter MySQL.

/// <summary>Body til POST /api/movies.</summary>
public class CreateMovieDto
{
}

/// <summary>Body til PUT /api/movies/{id}.</summary>
public class UpdateMovieDto
{
}
