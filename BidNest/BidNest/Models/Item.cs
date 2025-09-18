using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class Item
{
    public int ItemId { get; set; }

    public int SellerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    public decimal MinBid { get; set; }

    public decimal BidIncrement { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public decimal? CurrentPrice { get; set; }

    public int? CurrentBidId { get; set; }

    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<ItemDocument> ItemDocuments { get; set; } = new List<ItemDocument>();

    public virtual ICollection<ItemImage> ItemImages { get; set; } = new List<ItemImage>();

    public virtual User Seller { get; set; } = null!;

    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
