using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class BuyerDashboardViewModel
    {
        public List<BidItemViewModel> MyActiveBids { get; set; } = new();
        public List<AuctionItemViewModel> FeaturedAuctions { get; set; } = new();
        public List<AuctionItemViewModel> WatchlistItems { get; set; } = new();
        public List<WonAuctionViewModel> RecentlyWon { get; set; } = new();
    }

    public class BrowseViewModel
    {
        public List<AuctionItemViewModel> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string SortBy { get; set; } = "ending";
    }

    public class AuctionItemViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal MinBid { get; set; }
        public DateTime EndDate { get; set; }
        public int BidCount { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public TimeSpan TimeRemaining { get; set; }

        public string TimeRemainingDisplay
        {
            get
            {
                if (TimeRemaining.TotalDays >= 1)
                    return $"{(int)TimeRemaining.TotalDays}d {TimeRemaining.Hours}h";
                else if (TimeRemaining.TotalHours >= 1)
                    return $"{(int)TimeRemaining.TotalHours}h {TimeRemaining.Minutes}m";
                else if (TimeRemaining.TotalMinutes >= 1)
                    return $"{(int)TimeRemaining.TotalMinutes}m";
                else
                    return "Ending soon";
            }
        }

        public string StatusClass
        {
            get
            {
                if (TimeRemaining.TotalHours < 1)
                    return "text-danger";
                else if (TimeRemaining.TotalHours < 24)
                    return "text-warning";
                else
                    return "text-success";
            }
        }
    }

    public class ItemDetailsViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal MinBid { get; set; }
        public decimal BidIncrement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int BidCount { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public List<string> Images { get; set; } = new();
        public TimeSpan TimeRemaining { get; set; }
        public bool IsInWatchlist { get; set; }
        public decimal UserHighestBid { get; set; }
        public bool IsWinning { get; set; }
        public List<BidHistoryItemViewModel> BidHistory { get; set; } = new();
        public decimal NextMinimumBid { get; set; }

        public string TimeRemainingDisplay
        {
            get
            {
                if (TimeRemaining.TotalDays >= 1)
                    return $"{(int)TimeRemaining.TotalDays} days {TimeRemaining.Hours} hours";
                else if (TimeRemaining.TotalHours >= 1)
                    return $"{(int)TimeRemaining.TotalHours} hours {TimeRemaining.Minutes} minutes";
                else if (TimeRemaining.TotalMinutes >= 1)
                    return $"{(int)TimeRemaining.TotalMinutes} minutes";
                else
                    return "Ending soon";
            }
        }

        public bool CanBid => Status == "A" && TimeRemaining.TotalSeconds > 0;
    }

    public class BidHistoryItemViewModel
    {
        public string BidderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsYourBid { get; set; }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - BidTime;
                if (timeSpan.TotalMinutes < 1)
                    return "Just now";
                else if (timeSpan.TotalHours < 1)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                else if (timeSpan.TotalDays < 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                else
                    return $"{(int)timeSpan.TotalDays} days ago";
            }
        }
    }

    public class BidItemViewModel
    {
        public int BidId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ItemDescription { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal MyBidAmount { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsWinning { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public TimeSpan TimeRemaining { get; set; }
        public DateTime BidTime { get; set; }

        public string StatusDisplay
        {
            get
            {
                if (Status == "S" && IsWinning)
                    return "Won";
                else if (Status == "S" && !IsWinning)
                    return "Lost";
                else if (IsWinning)
                    return "Winning";
                else
                    return "Outbid";
            }
        }

        public string StatusClass
        {
            get
            {
                if (Status == "S" && IsWinning)
                    return "badge bg-success";
                else if (Status == "S" && !IsWinning)
                    return "badge bg-secondary";
                else if (IsWinning)
                    return "badge bg-primary";
                else
                    return "badge bg-warning";
            }
        }
    }

    public class WonAuctionViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal WinningBid { get; set; }
        public DateTime EndDate { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        public string PaymentStatusClass
        {
            get
            {
                return PaymentStatus switch
                {
                    "Paid" => "badge bg-success",
                    "Pending" => "badge bg-warning",
                    _ => "badge bg-secondary"
                };
            }
        }
    }

    public class PaymentViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // Payment details
        [Required(ErrorMessage = "Card holder name is required")]
        [Display(Name = "Card Holder Name")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required")]
        [Display(Name = "Card Number")]
        [CreditCard(ErrorMessage = "Invalid card number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [Display(Name = "Expiry Date (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Invalid expiry date format (MM/YY)")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [Display(Name = "CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Invalid CVV")]
        public string CVV { get; set; } = string.Empty;

        // Billing address
        [Required(ErrorMessage = "Billing address is required")]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Zip code is required")]
        [Display(Name = "Zip Code")]
        public string ZipCode { get; set; } = string.Empty;
    }

}
