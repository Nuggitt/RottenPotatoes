using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>Collection "ratings". Design: database/mongodb/design/ratings.json.</summary>
public class RatingDocument
{
    /// <summary>_id = Ratings.Id</summary>
    public int Id { get; set; }

    /// <summary>Reference til users._id.</summary>
    public int UserId { get; set; }

    public RatingTarget Target { get; set; } = new();

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Score { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Kun med, hvis ratingen har et review.</summary>
    [BsonIgnoreIfNull]
    public EmbeddedReview? Review { get; set; }
}
