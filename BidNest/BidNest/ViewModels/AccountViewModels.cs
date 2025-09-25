using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class UserBidsViewModel
    {
        public List<MyBidViewModel> Bids { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalBids { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class MyBidViewModel
    {
        public int BidId { get; set; }
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string ItemImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal MyBidAmount { get; set; }
        public decimal CurrentHighestBid { get; set; }
        public decimal MinimumBid { get; set; }
        public DateTime BidTime { get; set; }
        public string ItemStatus { get; set; } = string.Empty;
        public DateTime ItemEndDate { get; set; }
        public bool IsWinning { get; set; }
        public bool IsItemActive { get; set; }

        public string StatusDisplay => ItemStatus switch
        {
            "A" => "Active",
            "S" => "Sold",
            "E" => "Expired",
            "P" => "Pending",
            _ => "Unknown"
        };

        public string StatusBadgeClass => ItemStatus switch
        {
            "A" => "bg-success",
            "S" => "bg-primary",
            "E" => "bg-secondary",
            "P" => "bg-warning",
            _ => "bg-light"
        };

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - BidTime;
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                return BidTime.ToString("MMM dd, yyyy");
            }
        }

        public string TimeRemaining
        {
            get
            {
                if (!IsItemActive) return "Ended";
                
                var timeLeft = ItemEndDate - DateTime.UtcNow;
                if (timeLeft.TotalSeconds <= 0) return "Ended";
                
                if (timeLeft.TotalDays >= 1)
                    return $"{(int)timeLeft.TotalDays}d {timeLeft.Hours}h";
                if (timeLeft.TotalHours >= 1)
                    return $"{timeLeft.Hours}h {timeLeft.Minutes}m";
                return $"{timeLeft.Minutes}m {timeLeft.Seconds}s";
            }
        }
    }

    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }

    public class WatchlistViewModel
    {
        public List<ItemViewModel> Items { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        [Display(Name = "Bio")]
        public string? Bio { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
