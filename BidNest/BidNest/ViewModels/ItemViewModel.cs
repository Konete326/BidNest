using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class ItemViewModel
    {
        public int ItemId { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Minimum bid is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Minimum bid must be between $0.01 and $999,999.99")]
        [Display(Name = "Minimum Bid")]
        public decimal MinimumBid { get; set; }

        [Display(Name = "Buy Now Price")]
        [Range(0.01, 999999.99, ErrorMessage = "Buy now price must be between $0.01 and $999,999.99")]
        public decimal? BuyNowPrice { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Status")]
        public string Status { get; set; } = "A"; // A=Active, S=Sold, E=Expired, P=Pending

        [Display(Name = "Featured Item")]
        public bool IsFeatured { get; set; }

        // Navigation properties for display
        public string? CategoryName { get; set; }
        public string? SellerName { get; set; }
        public int SellerId { get; set; }
        public decimal? CurrentBid { get; set; }
        public int BidCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Image management
        public List<ItemImageViewModel> Images { get; set; } = new();
        public List<IFormFile>? NewImages { get; set; }
        public List<int>? ImagesToDelete { get; set; }

        // Validation
        public bool IsActive => Status == "A";
        public bool IsSold => Status == "S";
        public bool IsExpired => Status == "E" || EndDate < DateTime.Now;
        public bool IsPending => Status == "P";

        public string StatusDisplay => Status switch
        {
            "A" => "Active",
            "S" => "Sold",
            "E" => "Expired",
            "P" => "Pending",
            _ => "Unknown"
        };

        public string StatusBadgeClass => Status switch
        {
            "A" => "bg-label-success",
            "S" => "bg-label-primary",
            "E" => "bg-label-danger",
            "P" => "bg-label-warning",
            _ => "bg-label-secondary"
        };
    }

    public class ItemImageViewModel
    {
        public int ImageId { get; set; }
        public int ItemId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ItemListViewModel
    {
        public List<ItemViewModel> Items { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public bool ShowFeaturedOnly { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        // Filter options
        public string? CategoryName { get; set; }
        public List<CategoryViewModel> Categories { get; set; } = new();
    }

    public class ItemCreateViewModel
    {
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Minimum bid is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Minimum bid must be between $0.01 and $999,999.99")]
        [Display(Name = "Minimum Bid")]
        public decimal MinimumBid { get; set; }

        [Display(Name = "Buy Now Price")]
        [Range(0.01, 999999.99, ErrorMessage = "Buy now price must be between $0.01 and $999,999.99")]
        public decimal? BuyNowPrice { get; set; }

        [Required(ErrorMessage = "Auction duration is required")]
        [Range(1, 30, ErrorMessage = "Auction duration must be between 1 and 30 days")]
        [Display(Name = "Auction Duration (Days)")]
        public int AuctionDurationDays { get; set; } = 7;

        [Display(Name = "Featured Item")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Item Images")]
        public List<IFormFile>? Images { get; set; }

        // For admin creation
        public int? SellerId { get; set; }
    }

    public class ItemEditViewModel : ItemViewModel
    {
        [Display(Name = "Extend Auction")]
        public int? ExtendDays { get; set; }

        [Display(Name = "Change Status")]
        public string? NewStatus { get; set; }

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }
    }
}
