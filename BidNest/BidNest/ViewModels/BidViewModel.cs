using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class BidViewModel
    {
        public int BidId { get; set; }
        public int ItemId { get; set; }
        public int BidderId { get; set; }
        
        [Required(ErrorMessage = "Bid amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Bid amount must be between $0.01 and $999,999.99")]
        [Display(Name = "Bid Amount")]
        public decimal Amount { get; set; }
        
        public DateTime BidTime { get; set; }
        public bool IsWinning { get; set; }
        
        
        public string? BidderName { get; set; }
        public string? BidderEmail { get; set; }
        public string? ItemName { get; set; }
        public decimal? PreviousHighestBid { get; set; }
        public decimal MinimumBid { get; set; }
        public decimal BidIncrement { get; set; }
        
       
        public decimal NextMinimumBid => (PreviousHighestBid ?? MinimumBid) + BidIncrement;
        public bool IsValidBid => Amount >= NextMinimumBid;
        
        
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - BidTime;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
                return $"{(int)timeSpan.TotalDays}d ago";
            }
        }
    }

    public class PlaceBidViewModel
    {
        public int ItemId { get; set; }
        
        [Required(ErrorMessage = "Bid amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Bid amount must be between $0.01 and $999,999.99")]
        [Display(Name = "Your Bid Amount")]
        public decimal BidAmount { get; set; }
        
        
        public string ItemName { get; set; } = string.Empty;
        public decimal CurrentHighestBid { get; set; }
        public decimal MinimumBid { get; set; }
        public decimal BidIncrement { get; set; }
        public DateTime AuctionEndTime { get; set; }
        public string? ItemImageUrl { get; set; }
        
       
        public decimal NextMinimumBid => CurrentHighestBid > 0 ? CurrentHighestBid + BidIncrement : MinimumBid;
        public bool IsAuctionActive => AuctionEndTime > DateTime.UtcNow;
        public TimeSpan TimeRemaining => AuctionEndTime > DateTime.UtcNow ? AuctionEndTime - DateTime.UtcNow : TimeSpan.Zero;
        
       
        public List<decimal> SuggestedBids => new()
        {
            NextMinimumBid,
            NextMinimumBid + BidIncrement,
            NextMinimumBid + (BidIncrement * 2),
            NextMinimumBid + (BidIncrement * 5)
        };
    }

    public class BidHistoryViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public List<BidViewModel> Bids { get; set; } = new();
        public int TotalBids { get; set; }
        public decimal? HighestBid { get; set; }
        public string? CurrentWinnerName { get; set; }
        public DateTime AuctionEndTime { get; set; }
        public bool IsAuctionEnded => AuctionEndTime <= DateTime.UtcNow;
        
        
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalBids / PageSize);
    }

    public class AuctionStatusViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal? CurrentHighestBid { get; set; }
        public decimal MinimumBid { get; set; }
        public int TotalBids { get; set; }
        public string? CurrentWinnerName { get; set; }
        public string? ItemImageUrl { get; set; }
        
        // Debug properties
        public string DebugInfo => $"Status: {Status}, EndTime: {EndTime:yyyy-MM-dd HH:mm:ss} UTC, Now: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, TimeDiff: {(EndTime - DateTime.UtcNow).TotalMinutes:F1} minutes";
    
        public TimeSpan TimeRemaining => EndTime > DateTime.UtcNow ? EndTime - DateTime.UtcNow : TimeSpan.Zero;
        public bool IsActive => Status == "A" && EndTime > DateTime.UtcNow;
        public bool IsEnded => Status == "E" || Status == "S" || EndTime <= DateTime.UtcNow;
        public bool IsSold => Status == "S";
        
       
        public string StatusDisplay => Status switch
        {
            "A" when IsActive => "Active",
            "A" when IsEnded => "Ended",
            "E" => "Ended",
            "S" => "Sold",
            _ => "Unknown"
        };
        
        public string StatusBadgeClass => Status switch
        {
            "A" when IsActive => "bg-success",
            "A" when IsEnded => "bg-warning",
            "E" => "bg-secondary",
            "S" => "bg-primary",
            _ => "bg-light"
        };
        
        public string TimeRemainingDisplay
        {
            get
            {
                if (!IsActive) return "Auction Ended";
                
                var time = TimeRemaining;
                if (time.TotalDays >= 1)
                    return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
                if (time.TotalHours >= 1)
                    return $"{time.Hours}h {time.Minutes}m {time.Seconds}s";
                if (time.TotalMinutes >= 1)
                    return $"{time.Minutes}m {time.Seconds}s";
                return $"{time.Seconds}s";
            }
        }
    }

    public class MyBidsViewModel
    {
        public List<BidViewModel> MyBids { get; set; } = new();
        public List<BidViewModel> WinningBids { get; set; } = new();
        public List<BidViewModel> OutbidBids { get; set; } = new();
        public int TotalBids { get; set; }
        public int WinningCount { get; set; }
        public decimal TotalBidAmount { get; set; }
        
        
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalBids / PageSize);
    }

    public class AuctionSummaryViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public string? ItemImageUrl { get; set; }
        public decimal StartingBid { get; set; }
        public decimal? FinalBid { get; set; }
        public int TotalBids { get; set; }
        public DateTime AuctionStartTime { get; set; }
        public DateTime AuctionEndTime { get; set; }
        public string? WinnerName { get; set; }
        public string? WinnerEmail { get; set; }
        public string? SellerName { get; set; }
        public bool IsPaymentCompleted { get; set; }
        public DateTime? PaymentDate { get; set; }
        
        // Calculated properties
        public decimal ProfitAmount => (FinalBid ?? 0) - StartingBid;
        public double ProfitPercentage => StartingBid > 0 ? (double)(ProfitAmount / StartingBid) * 100 : 0;
        public TimeSpan AuctionDuration => AuctionEndTime - AuctionStartTime;
        
        public string AuctionDurationDisplay
        {
            get
            {
                if (AuctionDuration.TotalDays >= 1)
                    return $"{(int)AuctionDuration.TotalDays} day(s)";
                if (AuctionDuration.TotalHours >= 1)
                    return $"{(int)AuctionDuration.TotalHours} hour(s)";
                return $"{(int)AuctionDuration.TotalMinutes} minute(s)";
            }
        }
    }

    public class AdminBidsViewModel
    {
        public List<BidViewModel> Bids { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalBids { get; set; }
        public int TotalPages { get; set; }
        
        // Statistics
        public decimal TotalBidValue => Bids.Sum(b => b.Amount);
        public int WinningBidsCount => Bids.Count(b => b.IsWinning);
        public int UniqueBidders => Bids.Select(b => b.BidderId).Distinct().Count();
    }
}
