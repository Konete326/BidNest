using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class Watchlist
{
    public int WatchId { get; set; }

    public int UserId { get; set; }

    public int ItemId { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
