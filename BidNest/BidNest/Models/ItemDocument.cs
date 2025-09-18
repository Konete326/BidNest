using System;
using System.Collections.Generic;

namespace BidNest.Models;

public partial class ItemDocument
{
    public int DocId { get; set; }

    public int ItemId { get; set; }

    public string FileName { get; set; } = null!;

    public string Url { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;
}
