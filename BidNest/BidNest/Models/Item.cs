using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

[Index("CategoryId", Name = "IX_Items_CategoryId")]
public partial class Item
{
    [Key]
    public int ItemId { get; set; }

    public int SellerId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? CategoryId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MinBid { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal BidIncrement { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [StringLength(1)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? CurrentPrice { get; set; }

    public int? CurrentBidId { get; set; }

    [InverseProperty("Item")]
    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Items")]
    public virtual Category? Category { get; set; }

    [InverseProperty("Item")]
    public virtual ICollection<ItemDocument> ItemDocuments { get; set; } = new List<ItemDocument>();

    [InverseProperty("Item")]
    public virtual ICollection<ItemImage> ItemImages { get; set; } = new List<ItemImage>();

    [ForeignKey("SellerId")]
    [InverseProperty("Items")]
    public virtual User Seller { get; set; } = null!;

    [InverseProperty("Item")]
    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
