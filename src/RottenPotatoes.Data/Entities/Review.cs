using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class Review
{
    public ulong Id { get; set; }

    public ulong RatingId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Rating Rating { get; set; } = null!;
}
