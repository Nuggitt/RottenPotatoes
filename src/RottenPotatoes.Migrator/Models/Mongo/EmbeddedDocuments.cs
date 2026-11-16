using MongoDB.Bson.Serialization.Attributes;

namespace RottenPotatoes.Migrator.Models.Mongo;

// Indlejrede dokumenter. [BsonNoId] sørger for, at feltet hedder "id" og ikke "_id".

/// <summary>{ id, name } – bruges til genres, awards, streamingServices og series.actors/directors.</summary>
[BsonNoId]
public class NamedReference
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>{ id, name, dateOfBirth, biography } – bruges til movies.actors og movies.directors.</summary>
[BsonNoId]
public class PersonReference
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string? Biography { get; set; }
}

/// <summary>{ type, id } – hvad en rating handler om. type er "movie" eller "series".</summary>
[BsonNoId]
public class RatingTarget
{
    public string Type { get; set; } = string.Empty;

    public int Id { get; set; }
}

/// <summary>{ id, content, createdAt } – review indlejret i sin rating.</summary>
[BsonNoId]
public class EmbeddedReview
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
