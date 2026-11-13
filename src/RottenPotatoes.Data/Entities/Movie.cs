using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class Movie
{
    public uint Id { get; set; }

    public string Title { get; set; } = null!;

    public ushort? Duration { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Resume { get; set; }

    public string? Language { get; set; }

    public string? AgeRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public decimal AverageRating { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<Actor> Actors { get; set; } = new List<Actor>();

    public virtual ICollection<Award> Awards { get; set; } = new List<Award>();

    public virtual ICollection<Director> Directors { get; set; } = new List<Director>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();

    public virtual ICollection<StreamingService> StreamingServices { get; set; } = new List<StreamingService>();
}
