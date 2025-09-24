using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

[Index("BidderId", Name = "IX_Bids_BidderId")]
[Index("ItemId", "Amount", Name = "IX_Bids_ItemId_Amount", IsDescending = new[] { false, true })]
public partial class Bid
{
    [Key]
    public int BidId { get; set; }

    public int ItemId { get; set; }

    public int BidderId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    public DateTime BidTime { get; set; }

    public bool IsWinning { get; set; }

    [ForeignKey("BidderId")]
    [InverseProperty("Bids")]
    public virtual User Bidder { get; set; } = null!;

    [ForeignKey("ItemId")]
    [InverseProperty("Bids")]
    public virtual Item Item { get; set; } = null!;
}
