using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class Rating
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public uint? MovieId { get; set; }

    public uint? SeriesId { get; set; }

    public decimal Score { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual Review? Review { get; set; }

    public virtual Series? Series { get; set; }

    public virtual User User { get; set; } = null!;
}
