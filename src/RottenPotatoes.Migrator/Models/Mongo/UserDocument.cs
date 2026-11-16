namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>Collection "users". Design: database/mongodb/design/users.json.</summary>
public class UserDocument
{
    /// <summary>_id = Users.Id</summary>
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
