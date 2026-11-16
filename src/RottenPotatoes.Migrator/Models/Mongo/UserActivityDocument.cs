namespace RottenPotatoes.Migrator.Models.Mongo;

/// <summary>
/// Collection "userActivities" = UserActivity + UserActivityArchive.
/// Design: database/mongodb/design/userActivities.json.
/// Designets target og details migreres ikke: de findes ikke i MySQL.
/// </summary>
public class UserActivityDocument
{
    /// <summary>_id = UserActivity.Id / UserActivityArchive.Id</summary>
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ActivityType { get; set; } = string.Empty;

    /// <summary>MySQL: ActivityAt.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Kun sat for arkiverede aktiviteter (UserActivityArchive.ArchivedAt), ellers null.</summary>
    public DateTime? ArchivedAt { get; set; }
}
