using BidNest.Models;
using BidNest.ViewModels;

namespace BidNest.Services
{
    public interface IItemService
    {
        Task<ItemListViewModel> GetItemsAsync(string? searchTerm = null, int? categoryId = null, 
            string? status = null, string? sortBy = null, bool featuredOnly = false, 
            int page = 1, int pageSize = 20);
        
        Task<ItemViewModel?> GetItemByIdAsync(int itemId);
        Task<Item> CreateItemAsync(ItemCreateViewModel model, int sellerId);
        Task<Item> UpdateItemAsync(ItemEditViewModel model);
        Task<bool> DeleteItemAsync(int itemId);
        Task<bool> UpdateItemStatusAsync(int itemId, string status);
        Task<bool> ExtendAuctionAsync(int itemId, int days);
        Task<bool> SetFeaturedStatusAsync(int itemId, bool isFeatured);
        
        // Image management
        Task<bool> AddItemImagesAsync(int itemId, List<string> imagePaths);
        Task<bool> RemoveItemImageAsync(int itemId, int imageId);
        Task<bool> SetPrimaryImageAsync(int itemId, int imageId);
        Task<List<ItemImageViewModel>> GetItemImagesAsync(int itemId);
        
        // Status management
        Task UpdateExpiredItemsAsync();
        Task<Dictionary<string, int>> GetItemStatisticsAsync();
        
        // Search and filtering
        Task<List<ItemViewModel>> SearchItemsAsync(string searchTerm);
        Task<List<ItemViewModel>> GetFeaturedItemsAsync(int count = 8);
        Task<List<ItemViewModel>> GetRecentItemsAsync(int count = 10);
        Task<List<ItemViewModel>> GetItemsByCategoryAsync(int categoryId, int count = 20);
        
        // Seller-specific methods
        Task<List<ItemViewModel>> GetSellerActiveItemsAsync(int sellerId);
        Task<List<ItemViewModel>> GetSellerPendingItemsAsync(int sellerId);
        Task<List<ItemViewModel>> GetSellerSoldItemsAsync(int sellerId);
        Task<decimal> GetSellerTotalEarningsAsync(int sellerId);
        Task<List<SellerBidViewModel>> GetSellerRecentBidsAsync(int sellerId, int count);
        Task<List<ItemViewModel>> GetSellerItemsAsync(int sellerId, string status, int page, int pageSize);
        Task<int> GetSellerItemsCountAsync(int sellerId, string status);
        Task<int> GetSellerTotalListingsAsync(int sellerId);
        Task<int> GetSellerActiveListingsCountAsync(int sellerId);
        Task<int> GetSellerSoldItemsCountAsync(int sellerId);
        Task<decimal> GetSellerAverageSellingPriceAsync(int sellerId);
        Task<List<MonthlyEarningsViewModel>> GetSellerMonthlyEarningsAsync(int sellerId, int months);
        Task<List<CategoryStatsViewModel>> GetSellerTopCategoriesAsync(int sellerId, int count);
        Task<List<ItemViewModel>> GetSellerRecentSalesAsync(int sellerId, int count);
    }
}
