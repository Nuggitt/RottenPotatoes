namespace RottenPotatoes.Api.Security;

/// <summary>
/// Rollenavne og -id'er. Skal matche rækkerne i tabellen Roles
/// (database/mysql/changes/02_roles.sql).
/// </summary>
public static class RoleNames
{
    public const string User = "User";
    public const string Admin = "Admin";

    /// <summary>Roles.Id for User – gives til alle, der registrerer sig.</summary>
    public const byte UserRoleId = 1;
}
