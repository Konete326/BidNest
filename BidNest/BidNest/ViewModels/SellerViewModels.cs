using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class SellerDashboardViewModel
    {
        public int SellerId { get; set; }
        public List<ItemViewModel> ActiveListings { get; set; } = new();
        public List<ItemViewModel> PendingListings { get; set; } = new();
        public List<ItemViewModel> SoldItems { get; set; } = new();
        public decimal TotalEarnings { get; set; }
        public List<SellerBidViewModel> RecentBids { get; set; } = new();

        // Quick stats
        public int TotalActiveListings => ActiveListings.Count;
        public int TotalPendingListings => PendingListings.Count;
        public int TotalSoldItems => SoldItems.Count;
        public decimal AverageSellingPrice => TotalSoldItems > 0 ? TotalEarnings / TotalSoldItems : 0;
    }

    public class SellerItemsViewModel
    {
        public List<ItemViewModel> Items { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string StatusFilter { get; set; } = "all";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class SellerAnalyticsViewModel
    {
        public int SellerId { get; set; }
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int SoldItems { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal AverageSellingPrice { get; set; }
        public List<MonthlyEarningsViewModel> MonthlyEarnings { get; set; } = new();
        public List<CategoryStatsViewModel> TopCategories { get; set; } = new();
        public List<ItemViewModel> RecentSales { get; set; } = new();

        // Calculated properties
        public decimal ConversionRate => TotalListings > 0 ? (decimal)SoldItems / TotalListings * 100 : 0;
        public int PendingListings => TotalListings - ActiveListings - SoldItems;
    }

    public class MonthlyEarningsViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Earnings { get; set; }
        public int ItemsSold { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class SellerBidViewModel
    {
        public int BidId { get; set; }
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public int BidderId { get; set; }
        public string BidderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsWinning { get; set; }
        public string ItemImageUrl { get; set; } = string.Empty;

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
                return BidTime.ToString("MMM dd");
            }
        }
    }

    public class SellerStatsViewModel
    {
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int SoldListings { get; set; }
        public int PendingListings { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal AverageSellingPrice { get; set; }
        public decimal ConversionRate { get; set; }
        public int TotalBids { get; set; }
        public int TotalWatchers { get; set; }
    }

    public class SellItemQuickViewModel
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Minimum bid is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Minimum bid must be between $0.01 and $999,999.99")]
        public decimal MinimumBid { get; set; }

        [Range(1, 30, ErrorMessage = "Auction duration must be between 1 and 30 days")]
        public int AuctionDurationDays { get; set; } = 7;

        public List<IFormFile>? Images { get; set; }
    }
}
