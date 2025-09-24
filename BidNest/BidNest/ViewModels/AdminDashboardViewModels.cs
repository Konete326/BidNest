using System.ComponentModel.DataAnnotations;

namespace BidNest.ViewModels
{
    
    public class AdminDashboardViewModel
    {
        public AdminDashboardStatsViewModel Stats { get; set; } = new();
        public List<AdminRecentActivityViewModel> RecentActivities { get; set; } = new();
        public List<AuctionItemViewModel> EndingSoonAuctions { get; set; } = new();
        public List<AdminUserActivityViewModel> TopBidders { get; set; } = new();
        public List<AdminCategoryStatsViewModel> TopCategories { get; set; } = new();
        public AdminSystemHealthViewModel SystemHealth { get; set; } = new();
    }

    public class AdminDashboardStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BlockedUsers { get; set; }
        public int TotalItems { get; set; }
        public int ActiveAuctions { get; set; }
        public int CompletedAuctions { get; set; }
        public int TotalBids { get; set; }
        public int TotalCategories { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int NewUsersToday { get; set; }
        public int NewItemsToday { get; set; }
        public int BidsToday { get; set; }
    }

    public class AdminRecentActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty; // "bid", "user_registered", "item_created", "auction_ended"
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = string.Empty;
    }

    public class AdminSystemHealthViewModel
    {
        public int DatabaseConnections { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveSessions { get; set; }
        public DateTime LastBackup { get; set; }
        public bool IsHealthy { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    // User Management ViewModels
    public class AdminUserListViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public string? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }
    }

    public class AdminUserViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int ItemsCount { get; set; }
        public int BidsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalEarned { get; set; }
        public string StatusBadge => IsBlocked ? "badge bg-danger" : "badge bg-success";
        public string StatusText => IsBlocked ? "Blocked" : "Active";
    }

    public class AdminUserActivityViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public int BidsCount { get; set; }
        public int ItemsCount { get; set; }
        public int WonAuctions { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalEarned { get; set; }
        public DateTime LastActivity { get; set; }
        public List<AdminRecentActivityViewModel> RecentActivities { get; set; } = new();
    }

    public class AdminUserDetailsViewModel
    {
        public AdminUserViewModel User { get; set; } = new();
        public List<ItemViewModel> UserItems { get; set; } = new();
        public List<BidItemViewModel> UserBids { get; set; } = new();
        public List<AdminRecentActivityViewModel> ActivityLog { get; set; } = new();
        public AdminUserStatsViewModel Stats { get; set; } = new();
    }

    public class AdminUserStatsViewModel
    {
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int SoldItems { get; set; }
        public int TotalBids { get; set; }
        public int WonAuctions { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal AverageItemPrice { get; set; }
        public decimal AverageBidAmount { get; set; }
        public double SuccessRate { get; set; }
    }

    // Category Management ViewModels  
    public class AdminCategoryStatsViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int ActiveItemCount { get; set; }
        public int TotalBids { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageItemPrice { get; set; }
        public double PopularityScore { get; set; }
        public List<AdminMonthlyStatsViewModel> MonthlyStats { get; set; } = new();
    }

    // System Monitoring ViewModels
    public class SystemMonitoringViewModel
    {
        public List<AuctionMonitorViewModel> ActiveAuctions { get; set; } = new();
        public List<BiddingActivityViewModel> RecentBids { get; set; } = new();
        public SystemPerformanceViewModel Performance { get; set; } = new();
        public List<AlertViewModel> SystemAlerts { get; set; } = new();
    }

    public class AuctionMonitorViewModel
    {
        public int ItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal MinBid { get; set; }
        public int BidCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasSuspiciousActivity { get; set; }
        public List<string> Flags { get; set; } = new();
    }

    public class BiddingActivityViewModel
    {
        public int BidId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string BidderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsSuspicious { get; set; }
        public string? SuspiciousReason { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class SystemPerformanceViewModel
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public int QueuedJobs { get; set; }
        public double ResponseTime { get; set; }
        public int ErrorRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class AlertViewModel
    {
        public int AlertId { get; set; }
        public string Type { get; set; } = string.Empty; // "warning", "error", "info"
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string BadgeClass => Type switch
        {
            "error" => "badge bg-danger",
            "warning" => "badge bg-warning",
            "info" => "badge bg-info",
            _ => "badge bg-secondary"
        };
    }

    // Report Generation ViewModels
    public class ReportGenerationViewModel
    {
        public List<ReportTemplateViewModel> AvailableReports { get; set; } = new();
        public ReportParametersViewModel Parameters { get; set; } = new();
    }

    public class ReportTemplateViewModel
    {
        public string ReportId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> RequiredParameters { get; set; } = new();
        public string Icon { get; set; } = string.Empty;
    }

    public class ReportParametersViewModel
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int? CategoryId { get; set; }
        public int? UserId { get; set; }
        public string? Status { get; set; }
        public string Format { get; set; } = "pdf"; // pdf, excel, csv
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
    }

    public class ReportResultViewModel
    {
        public string ReportId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public ReportDataViewModel Data { get; set; } = new();
    }

    public class ReportDataViewModel
    {
        public Dictionary<string, object> Summary { get; set; } = new();
        public List<Dictionary<string, object>> Details { get; set; } = new();
        public List<ChartDataViewModel> Charts { get; set; } = new();
    }

    public class ChartDataViewModel
    {
        public string ChartType { get; set; } = string.Empty; // bar, line, pie, doughnut
        public string Title { get; set; } = string.Empty;
        public List<string> Labels { get; set; } = new();
        public List<decimal> Data { get; set; } = new();
        public List<string> BackgroundColors { get; set; } = new();
    }

    // Shared ViewModels
    public class AdminMonthlyStatsViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int BidCount { get; set; }
        public decimal TotalValue { get; set; }
        public int NewUsers { get; set; }
    }

    public class AdminPaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}
