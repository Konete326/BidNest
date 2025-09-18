using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class Bid
{
    public int BidId { get; set; }

    public int ItemId { get; set; }

    public int BidderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime BidTime { get; set; }

    public bool IsWinning { get; set; }

    public virtual User Bidder { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
