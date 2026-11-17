using System.ComponentModel.DataAnnotations;

namespace RottenPotatoes.Api.DTOs;

/// <summary>Body til POST /api/movies/{id}/ratings og /api/series/{id}/ratings.</summary>
public class CreateRatingDto
{
    /// <summary>Score fra 0.0 til 10.0 med én decimal (CK_Ratings_Score).</summary>
    [Range(0.0, 10.0)]
    public decimal Score { get; set; }
}

/// <summary>Body til POST /api/ratings/{ratingId}/review.</summary>
public class CreateReviewDto
{
    /// <summary>Teksten må ikke være tom (CK_Reviews_Content). [Required] afviser også kun mellemrum.</summary>
    [Required]
    [StringLength(10_000)]
    public string Content { get; set; } = string.Empty;
}

public class RatingDto
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    /// <summary>Sat, hvis ratingen er af en film.</summary>
    public uint? MovieId { get; set; }

    /// <summary>Sat, hvis ratingen er af en serie.</summary>
    public uint? SeriesId { get; set; }

    public decimal Score { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ReviewDto
{
    public ulong Id { get; set; }

    public ulong RatingId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
