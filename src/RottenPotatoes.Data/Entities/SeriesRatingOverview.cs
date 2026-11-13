using System;
using System.Collections.Generic;

namespace RottenPotatoes.Data.Entities;

public partial class SeriesRatingOverview
{
    public uint? SeriesId { get; set; }

    public string? Title { get; set; }

    public decimal? AverageRating { get; set; }

    public long? NumberOfRatings { get; set; }
}
