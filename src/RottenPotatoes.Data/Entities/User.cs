using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class User
{
    public ulong Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public byte RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
}
