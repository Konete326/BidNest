using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

[Table("Watchlist")]
[Index("UserId", "ItemId", Name = "UQ_Watch", IsUnique = true)]
public partial class Watchlist
{
    [Key]
    public int WatchId { get; set; }

    public int UserId { get; set; }

    public int ItemId { get; set; }

    public DateTime AddedAt { get; set; }

    [ForeignKey("ItemId")]
    [InverseProperty("Watchlists")]
    public virtual Item Item { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Watchlists")]
    public virtual User User { get; set; } = null!;
}
