using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

public partial class ItemImage
{
    [Key]
    public int ImageId { get; set; }

    public int ItemId { get; set; }

    [StringLength(1000)]
    public string Url { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("ItemId")]
    [InverseProperty("ItemImages")]
    public virtual Item Item { get; set; } = null!;
}
