namespace RottenPotatoes.Api.DTOs;

// TODO: Vælg felterne ud fra tabellen Movies (Id, Title, Duration, ReleaseDate, Resume,
// Language, AgeRating, AverageRating, CreatedAt, UpdatedAt) og de entities, som
// `make scaffold` genererer. Eksponér aldrig entities direkte fra API'et.

/// <summary>Film som den returneres fra API'et.</summary>
public class MovieDto
{
}

/// <summary>Body til POST /api/movies.</summary>
public class CreateMovieDto
{
}

/// <summary>Body til PUT /api/movies/{id}.</summary>
public class UpdateMovieDto
{
}
