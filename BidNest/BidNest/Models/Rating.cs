using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class Rating
{
    public int RatingId { get; set; }

    public int RaterId { get; set; }

    public int RatedUserId { get; set; }

    public int Score { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User RatedUser { get; set; } = null!;

    public virtual User Rater { get; set; } = null!;
}
