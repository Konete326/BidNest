using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidNest.Models
{
    public class ContactMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = null!;

        public bool NewsletterSubscription { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New"; // New, Read, Replied, Archived

        [StringLength(2000)]
        public string? AdminReply { get; set; }

        public DateTime? RepliedAt { get; set; }

        public int? RepliedByUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("RepliedByUserId")]
        public virtual User? RepliedByUser { get; set; }
    }
}
