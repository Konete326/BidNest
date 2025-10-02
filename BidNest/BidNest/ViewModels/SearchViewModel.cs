using Microsoft.AspNetCore.Mvc.Rendering;

namespace BidNest.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Status { get; set; } = "A"; // Active by default
        public string? SortBy { get; set; } = "EndDate"; // Default sort
        public string? SortOrder { get; set; } = "asc"; // asc or desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        
        // Search filters
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public bool? HasBids { get; set; }
        public string? Location { get; set; }

        // Results
        public List<SearchResultItemViewModel> Results { get; set; } = new();
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }

        // Dropdown data
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> SortOptions { get; set; } = new();

        // Search statistics
        public SearchStatsViewModel Stats { get; set; } = new();

        // Helper properties
        public bool HasFilters => !string.IsNullOrEmpty(Query) || 
                                 CategoryId.HasValue || 
                                 MinPrice.HasValue || 
                                 MaxPrice.HasValue || 
                                 !string.IsNullOrEmpty(Location) ||
                                 StartDateFrom.HasValue ||
                                 EndDateFrom.HasValue ||
                                 HasBids.HasValue;

        public string SearchSummary
        {
            get
            {
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(Query))
                    parts.Add($"'{Query}'");
                
                if (CategoryId.HasValue)
                {
                    var category = Categories.FirstOrDefault(c => c.Value == CategoryId.ToString());
                    if (category != null)
                        parts.Add($"in {category.Text}");
                }
                
                if (MinPrice.HasValue && MaxPrice.HasValue)
                    parts.Add($"${MinPrice:N0} - ${MaxPrice:N0}");
                else if (MinPrice.HasValue)
                    parts.Add($"over ${MinPrice:N0}");
                else if (MaxPrice.HasValue)
                    parts.Add($"under ${MaxPrice:N0}");

                return parts.Any() ? string.Join(", ", parts) : "All items";
            }
        }
    }

    public class SearchResultItemViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal MinBid { get; set; }
        public int BidCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? SellerName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public bool HasBids => BidCount > 0;
        
        // Calculated properties
        public TimeSpan TimeRemaining => EndDate > DateTime.UtcNow ? EndDate - DateTime.UtcNow : TimeSpan.Zero;
        public bool IsActive => Status == "A" && EndDate > DateTime.UtcNow;
        public bool IsEnding => IsActive && TimeRemaining.TotalHours <= 24;
        public bool IsEnded => Status == "E" || EndDate <= DateTime.UtcNow;

        public string TimeRemainingDisplay
        {
            get
            {
                if (IsEnded) return "Ended";
                if (!IsActive) return "Not Active";
                
                var time = TimeRemaining;
                if (time.TotalDays >= 1)
                    return $"{(int)time.TotalDays}d {time.Hours}h";
                if (time.TotalHours >= 1)
                    return $"{time.Hours}h {time.Minutes}m";
                return $"{time.Minutes}m {time.Seconds}s";
            }
        }

        public string StatusBadgeClass => Status switch
        {
            "A" when IsEnding => "bg-warning",
            "A" => "bg-success",
            "E" => "bg-secondary",
            "S" => "bg-info",
            _ => "bg-secondary"
        };

        public string StatusDisplay => Status switch
        {
            "A" when IsEnding => "Ending Soon",
            "A" => "Active",
            "E" => "Ended",
            "S" => "Sold",
            _ => "Unknown"
        };
    }

    public class SearchStatsViewModel
    {
        public int TotalItems { get; set; }
        public int ActiveAuctions { get; set; }
        public int EndingSoon { get; set; }
        public decimal AverageBid { get; set; }
        public decimal HighestBid { get; set; }
        public decimal LowestBid { get; set; }
        public int TotalBids { get; set; }
        public List<CategoryStatsViewModel> CategoryStats { get; set; } = new();
    }

    public class CategoryStatsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class QuickSearchViewModel
    {
        public string? Query { get; set; }
        public List<QuickSearchResultViewModel> Results { get; set; } = new();
        public bool HasMore { get; set; }
        public int TotalResults { get; set; }
    }

    public class QuickSearchResultViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string TimeRemaining { get; set; } = string.Empty;
    }
}
