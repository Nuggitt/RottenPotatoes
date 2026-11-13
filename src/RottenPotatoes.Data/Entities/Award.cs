using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class Award
{
    public ushort Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual ICollection<Series> Series { get; set; } = new List<Series>();
}
