using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class ItemImage
{
    public int ImageId { get; set; }

    public int ItemId { get; set; }

    public string Url { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;
}
