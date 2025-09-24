using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

public partial class ItemDocument
{
    [Key]
    public int DocId { get; set; }

    public int ItemId { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [StringLength(1000)]
    public string Url { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [ForeignKey("ItemId")]
    [InverseProperty("ItemDocuments")]
    public virtual Item Item { get; set; } = null!;
}
