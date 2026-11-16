using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>
/// Collection "series". Design: database/mongodb/design/series.json.
/// Designets episodes[] (sæson/afsnit-detaljer) migreres ikke: der findes ingen Episodes-tabel i MySQL.
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
