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
    }
}
