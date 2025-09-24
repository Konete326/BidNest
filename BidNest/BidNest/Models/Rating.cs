using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

public partial class Rating
{
    [Key]
    public int RatingId { get; set; }

    public int RaterId { get; set; }

    public int RatedUserId { get; set; }

    public int Score { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("RatedUserId")]
    [InverseProperty("RatingRatedUsers")]
    public virtual User RatedUser { get; set; } = null!;

    [ForeignKey("RaterId")]
    [InverseProperty("RatingRaters")]
    public virtual User Rater { get; set; } = null!;
}
