using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>Collection "movies". Design: database/mongodb/design/movies.json.</summary>
public class MovieDocument
{
    /// <summary>_id = Movies.Id</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? Duration { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    public string? Language { get; set; }

    public string? AgeRating { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<NamedReference> Genres { get; set; } = [];

    public List<PersonReference> Actors { get; set; } = [];

    public List<PersonReference> Directors { get; set; } = [];

    public List<NamedReference> Awards { get; set; } = [];

    public List<NamedReference> StreamingServices { get; set; } = [];
}
