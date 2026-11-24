using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>
/// Collection "series". Design: database/mongodb/design/series.json.
/// </summary>
public class SeriesDocument
{
    /// <summary>_id = Series.Id</summary>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    public string? Language { get; set; }

    public string? AgeRating { get; set; }

    /// <summary>Antal sæsoner (Series.Seasons).</summary>
    public int? Seasons { get; set; }

    /// <summary>Antal afsnit i alt (Series.Episodes). Hedder episodeCount, så det er tydeligt, at feltet er et antal.</summary>
    public int? EpisodeCount { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<NamedReference> Genres { get; set; } = [];

    public List<NamedReference> Actors { get; set; } = [];

    public List<NamedReference> Directors { get; set; } = [];

    public List<NamedReference> Awards { get; set; } = [];

    public List<NamedReference> StreamingServices { get; set; } = [];
}
