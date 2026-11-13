using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class UserActivity
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public DateTime ActivityAt { get; set; }

    public virtual User User { get; set; } = null!;
}
