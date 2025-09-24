using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Models;

[Index("Username", Name = "UQ__Users__536C85E42D2F2DBE", IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(512)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(200)]
    public string? FullName { get; set; }

    public bool IsBlocked { get; set; }

    public int RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Bidder")]
    public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();

    [InverseProperty("Seller")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("RatedUser")]
    public virtual ICollection<Rating> RatingRatedUsers { get; set; } = new List<Rating>();

    [InverseProperty("Rater")]
    public virtual ICollection<Rating> RatingRaters { get; set; } = new List<Rating>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
}
