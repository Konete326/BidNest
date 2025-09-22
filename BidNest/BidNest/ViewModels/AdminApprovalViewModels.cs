using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    public class PendingApprovalViewModel
    {
        public List<ItemApprovalViewModel> PendingItems { get; set; } = new();
        public int TotalPending { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => (int)Math.Ceiling((double)TotalPending / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ItemApprovalViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal MinimumBid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerEmail { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public string PrimaryImageUrl => ImageUrls.FirstOrDefault() ?? "";
        
        // Approval fields
        public string? RejectionReason { get; set; }
        public bool RequiresChanges { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class BulkApprovalViewModel
    {
        public List<int> ItemIds { get; set; } = new();
        public string Action { get; set; } = "approve"; // approve, reject, request_changes
        public string? RejectionReason { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class ApprovalStatsViewModel
    {
        public int TotalPending { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedToday { get; set; }
        public int AverageApprovalTime { get; set; } // in hours
        public List<CategoryApprovalStats> CategoryStats { get; set; } = new();
    }

    public class CategoryApprovalStats
    {
        public string CategoryName { get; set; } = string.Empty;
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class ItemApprovalDetailViewModel
    {
        public ItemApprovalViewModel Item { get; set; } = new();
        public List<string> ValidationIssues { get; set; } = new();
        public bool HasPolicyViolations { get; set; }
        public List<string> PolicyViolations { get; set; } = new();
        public List<SimilarItemViewModel> SimilarItems { get; set; } = new();
    }

    public class SimilarItemViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
    }
}
