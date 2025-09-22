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
}
