using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class Actor
{
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Biography { get; set; }

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual ICollection<Series> Series { get; set; } = new List<Series>();
}
