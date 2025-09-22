using BidNest.Models;
using BidNest.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Services
{
    public class ItemService : IItemService
    {
        private readonly BidnestContext _context;
        private readonly IImageService _imageService;
        private readonly ILogger<ItemService> _logger;

        public ItemService(BidnestContext context, IImageService imageService, ILogger<ItemService> logger)
        {
            _context = context;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<ItemListViewModel> GetItemsAsync(string? searchTerm = null, int? categoryId = null, 
            string? status = null, string? sortBy = null, bool featuredOnly = false, 
            int page = 1, int pageSize = 20)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(i => i.Title.Contains(searchTerm) || (i.Description != null && i.Description.Contains(searchTerm)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            
            query = sortBy switch
            {
                "name" => query.OrderBy(i => i.Title),
                "name_desc" => query.OrderByDescending(i => i.Title),
                "price" => query.OrderBy(i => i.MinBid),
                "price_desc" => query.OrderByDescending(i => i.MinBid),
                "end_date" => query.OrderBy(i => i.EndDate),
                "end_date_desc" => query.OrderByDescending(i => i.EndDate),
                _ => query.OrderByDescending(i => i.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var itemViewModels = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                itemViewModels.Add(viewModel);
            }

            return new ItemListViewModel
            {
                Items = itemViewModels,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Status = status,
                SortBy = sortBy,
                ShowFeaturedOnly = featuredOnly,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<ItemViewModel?> GetItemByIdAsync(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);

            return item != null ? await MapToViewModelAsync(item) : null;
        }

        public async Task<Item> CreateItemAsync(ItemCreateViewModel model, int sellerId)
        {
            var item = new Item
            {
                Title = model.Name,
                Description = model.Description,
                CategoryId = model.CategoryId,
                MinBid = model.MinimumBid,
                BidIncrement = 1.00m, // Default increment
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(model.AuctionDurationDays),
                Status = "A", // Active
                SellerId = sellerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Handle image uploads
            if (model.Images != null && model.Images.Any())
            {
                var imagePaths = await _imageService.UploadImagesAsync(model.Images);
                await AddItemImagesAsync(item.ItemId, imagePaths);
            }

            return item;
        }

        public async Task<Item> UpdateItemAsync(ItemEditViewModel model)
        {
            Console.WriteLine($"=== UpdateItemAsync called for ItemId: {model.ItemId} ===");
            
            var item = await _context.Items.FindAsync(model.ItemId);
            if (item == null)
                throw new ArgumentException("Item not found");

            Console.WriteLine($"Found item: {item.Title} (Current Status: {item.Status})");

            // Update basic properties that exist in the database
            var oldTitle = item.Title;
            var oldStatus = item.Status;
            
            item.Title = model.Name;
            item.Description = model.Description;
            item.CategoryId = model.CategoryId;
            item.MinBid = model.MinimumBid;

            Console.WriteLine($"Updated Title: {oldTitle} -> {item.Title}");
            Console.WriteLine($"Updated MinBid: {item.MinBid}");

            // Handle status change
            if (!string.IsNullOrEmpty(model.NewStatus) && model.NewStatus != item.Status)
            {
                item.Status = model.NewStatus;
                Console.WriteLine($"Status changed: {oldStatus} -> {item.Status}");
            }

            // Handle auction extension
            if (model.ExtendDays.HasValue && model.ExtendDays > 0)
            {
                var oldEndDate = item.EndDate;
                item.EndDate = item.EndDate.AddDays(model.ExtendDays.Value);
                Console.WriteLine($"Extended auction: {oldEndDate} -> {item.EndDate}");
            }

            // Note: BuyNowPrice, IsFeatured, and UpdatedAt are not in the current database schema
            // These properties are ignored during update

            Console.WriteLine("Saving changes to database...");
            await _context.SaveChangesAsync();
            Console.WriteLine("Changes saved successfully!");

            // Handle image management (do not break main update flow)
            if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
            {
                foreach (var imageId in model.ImagesToDelete)
                {
                    try
                    {
                        await RemoveItemImageAsync(model.ItemId, imageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove image {ImageId} for item {ItemId}", imageId, model.ItemId);
                    }
                }
            }

            if (model.NewImages != null && model.NewImages.Any())
            {
                try
                {
                    var imagePaths = await _imageService.UploadImagesAsync(model.NewImages);
                    await AddItemImagesAsync(model.ItemId, imagePaths);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add new images for item {ItemId}", model.ItemId);
                }
            }

            return item;
        }

        public async Task<bool> DeleteItemAsync(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);

            if (item == null)
                return false;

            // Check if item has bids
            if (item.Bids.Any())
            {
                // Don't delete items with bids, just mark as cancelled
                item.Status = "C"; // Cancelled
                await _context.SaveChangesAsync();
                return true;
            }

            // Delete associated images
            var imagePaths = item.ItemImages.Select(img => img.Url).ToList();
            await _imageService.DeleteImagesAsync(imagePaths);

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateItemStatusAsync(int itemId, string status)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return false;

            item.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExtendAuctionAsync(int itemId, int days)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return false;

            item.EndDate = item.EndDate.AddDays(days);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetFeaturedStatusAsync(int itemId, bool isFeatured)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return false;

            // Note: IsFeatured property doesn't exist in current model
            // This would need to be added to the database schema
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddItemImagesAsync(int itemId, List<string> imagePaths)
        {
            var existingImagesCount = await _context.ItemImages.CountAsync(img => img.ItemId == itemId);
            
            foreach (var (imagePath, index) in imagePaths.Select((path, i) => (path, i)))
            {
                var itemImage = new ItemImage
                {
                    ItemId = itemId,
                    Url = imagePath,
                    IsPrimary = existingImagesCount == 0 && index == 0, // First image is primary if no existing images
                    CreatedAt = DateTime.UtcNow
                };

                _context.ItemImages.Add(itemImage);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveItemImageAsync(int itemId, int imageId)
        {
            var image = await _context.ItemImages
                .FirstOrDefaultAsync(img => img.ImageId == imageId && img.ItemId == itemId);

            if (image == null)
                return false;

            // Delete physical file
            await _imageService.DeleteImageAsync(image.Url);

            _context.ItemImages.Remove(image);
            await _context.SaveChangesAsync();

            // If this was the primary image, set another as primary
            if (image.IsPrimary)
            {
                var nextImage = await _context.ItemImages
                    .Where(img => img.ItemId == itemId)
                    .FirstOrDefaultAsync();

                if (nextImage != null)
                {
                    nextImage.IsPrimary = true;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(int itemId, int imageId)
        {
            // Remove primary status from all images for this item
            var allImages = await _context.ItemImages
                .Where(img => img.ItemId == itemId)
                .ToListAsync();

            foreach (var img in allImages)
            {
                img.IsPrimary = img.ImageId == imageId;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ItemImageViewModel>> GetItemImagesAsync(int itemId)
        {
            var images = await _context.ItemImages
                .Where(img => img.ItemId == itemId)
                .ToListAsync();

            return images.Select(img => new ItemImageViewModel
            {
                ImageId = img.ImageId,
                ItemId = img.ItemId,
                ImagePath = img.Url,
                IsPrimary = img.IsPrimary,
                CreatedAt = img.CreatedAt
            }).ToList();
        }

        public async Task UpdateExpiredItemsAsync()
        {
            var expiredItems = await _context.Items
                .Where(i => i.Status == "A" && i.EndDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var item in expiredItems)
            {
                item.Status = "E"; // Expired
            }

            if (expiredItems.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} expired items", expiredItems.Count);
            }
        }

        public async Task<Dictionary<string, int>> GetItemStatisticsAsync()
        {
            return new Dictionary<string, int>
            {
                ["Total"] = await _context.Items.CountAsync(),
                ["Active"] = await _context.Items.CountAsync(i => i.Status == "A"),
                ["Sold"] = await _context.Items.CountAsync(i => i.Status == "S"),
                ["Expired"] = await _context.Items.CountAsync(i => i.Status == "E"),
                ["Featured"] = 0 // Feature not implemented in current schema
            };
        }

        public async Task<List<ItemViewModel>> SearchItemsAsync(string searchTerm)
        {
            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Where(i => i.Title.Contains(searchTerm) || (i.Description != null && i.Description.Contains(searchTerm)))
                .Where(i => i.Status == "A")
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }

            return result;
        }

        public async Task<List<ItemViewModel>> GetFeaturedItemsAsync(int count = 6)
        {
            var items = await _context.Items
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }

        public async Task<List<ItemViewModel>> GetRecentItemsAsync(int count = 10)
        {
            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Where(i => i.Status == "A")
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }

            return result;
        }

        public async Task<List<ItemViewModel>> GetItemsByCategoryAsync(int categoryId, int count = 20)
        {
            var items = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Where(i => i.CategoryId == categoryId && i.Status == "A")
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }

            return result;
        }

        private async Task<ItemViewModel> MapToViewModelAsync(Item item)
        {
            var currentBid = await _context.Bids
                .Where(b => b.ItemId == item.ItemId)
                .OrderByDescending(b => b.Amount)
                .Select(b => b.Amount)
                .FirstOrDefaultAsync();

            var bidCount = await _context.Bids.CountAsync(b => b.ItemId == item.ItemId);

            return new ItemViewModel
            {
                ItemId = item.ItemId,
                Name = item.Title,
                Description = item.Description ?? string.Empty,
                CategoryId = item.CategoryId ?? 0,
                CategoryName = item.Category?.Name,
                MinimumBid = item.MinBid,
                BuyNowPrice = null, // Not in current database schema - will be ignored
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                IsFeatured = false, // Not in current database schema - will be ignored
                SellerId = item.SellerId,
                SellerName = item.Seller?.FullName,
                CurrentBid = currentBid > 0 ? currentBid : null,
                BidCount = bidCount,
                CreatedAt = item.CreatedAt,
                UpdatedAt = null, // Not in current database schema - will be ignored
                Images = item.ItemImages?.Select(img => new ItemImageViewModel
                {
                    ImageId = img.ImageId,
                    ItemId = img.ItemId,
                    ImagePath = img.Url,
                    IsPrimary = img.IsPrimary,
                    CreatedAt = img.CreatedAt
                }).ToList() ?? new List<ItemImageViewModel>()
            };
        }

        // Seller-specific methods
        public async Task<List<ItemViewModel>> GetSellerActiveItemsAsync(int sellerId)
        {
            var items = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "A")
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }

        public async Task<List<ItemViewModel>> GetSellerPendingItemsAsync(int sellerId)
        {
            var items = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "P")
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }

        public async Task<List<ItemViewModel>> GetSellerSoldItemsAsync(int sellerId)
        {
            var items = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "S")
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .OrderByDescending(i => i.EndDate)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }

        public async Task<decimal> GetSellerTotalEarningsAsync(int sellerId)
        {
            return await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "S" && i.CurrentPrice.HasValue)
                .SumAsync(i => i.CurrentPrice.Value);
        }

        public async Task<List<SellerBidViewModel>> GetSellerRecentBidsAsync(int sellerId, int count)
        {
            var bids = await _context.Bids
                .Where(b => b.Item.SellerId == sellerId)
                .Include(b => b.Item)
                .ThenInclude(i => i.ItemImages)
                .Include(b => b.Bidder)
                .OrderByDescending(b => b.BidTime)
                .Take(count)
                .Select(b => new SellerBidViewModel
                {
                    BidId = b.BidId,
                    ItemId = b.ItemId,
                    ItemTitle = b.Item.Title,
                    BidderId = b.BidderId,
                    BidderName = b.Bidder.FullName,
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    IsWinning = b.Item.CurrentBidId == b.BidId,
                    ItemImageUrl = b.Item.ItemImages.FirstOrDefault(img => img.IsPrimary) != null
                        ? b.Item.ItemImages.FirstOrDefault(img => img.IsPrimary)!.Url
                        : b.Item.ItemImages.FirstOrDefault() != null
                            ? b.Item.ItemImages.FirstOrDefault()!.Url
                            : ""
                })
                .ToListAsync();

            return bids;
        }

        public async Task<List<ItemViewModel>> GetSellerItemsAsync(int sellerId, string status, int page, int pageSize)
        {
            var query = _context.Items
                .Where(i => i.SellerId == sellerId)
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .AsQueryable();

            if (status != "all")
            {
                query = status switch
                {
                    "active" => query.Where(i => i.Status == "A"),
                    "pending" => query.Where(i => i.Status == "P"),
                    "sold" => query.Where(i => i.Status == "S"),
                    "expired" => query.Where(i => i.Status == "E"),
                    _ => query
                };
            }

            var items = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }

        public async Task<int> GetSellerItemsCountAsync(int sellerId, string status)
        {
            var query = _context.Items.Where(i => i.SellerId == sellerId);

            if (status != "all")
            {
                query = status switch
                {
                    "active" => query.Where(i => i.Status == "A"),
                    "pending" => query.Where(i => i.Status == "P"),
                    "sold" => query.Where(i => i.Status == "S"),
                    "expired" => query.Where(i => i.Status == "E"),
                    _ => query
                };
            }

            return await query.CountAsync();
        }

        public async Task<int> GetSellerTotalListingsAsync(int sellerId)
        {
            return await _context.Items.CountAsync(i => i.SellerId == sellerId);
        }

        public async Task<int> GetSellerActiveListingsCountAsync(int sellerId)
        {
            return await _context.Items.CountAsync(i => i.SellerId == sellerId && i.Status == "A");
        }

        public async Task<int> GetSellerSoldItemsCountAsync(int sellerId)
        {
            return await _context.Items.CountAsync(i => i.SellerId == sellerId && i.Status == "S");
        }

        public async Task<decimal> GetSellerAverageSellingPriceAsync(int sellerId)
        {
            var soldItems = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "S" && i.CurrentPrice.HasValue)
                .ToListAsync();

            return soldItems.Any() ? soldItems.Average(i => i.CurrentPrice.Value) : 0;
        }

        public async Task<List<MonthlyEarningsViewModel>> GetSellerMonthlyEarningsAsync(int sellerId, int months)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);
            
            var earnings = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "S" && i.EndDate >= startDate && i.CurrentPrice.HasValue)
                .GroupBy(i => new { i.EndDate.Year, i.EndDate.Month })
                .Select(g => new MonthlyEarningsViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Earnings = g.Sum(i => i.CurrentPrice.Value),
                    ItemsSold = g.Count()
                })
                .OrderBy(e => e.Year)
                .ThenBy(e => e.Month)
                .ToListAsync();

            return earnings;
        }

        public async Task<List<CategoryStatsViewModel>> GetSellerTopCategoriesAsync(int sellerId, int count)
        {
            var categories = await _context.Items
                .Where(i => i.SellerId == sellerId && i.CategoryId.HasValue)
                .Include(i => i.Category)
                .GroupBy(i => new { i.CategoryId, i.Category!.Name })
                .Select(g => new CategoryStatsViewModel
                {
                    CategoryId = g.Key.CategoryId!.Value,
                    CategoryName = g.Key.Name,
                    ItemCount = g.Count(),
                    AveragePrice = g.Where(i => i.CurrentPrice.HasValue).Average(i => i.CurrentPrice.Value)
                })
                .OrderByDescending(c => c.ItemCount)
                .Take(count)
                .ToListAsync();

            return categories;
        }

        public async Task<List<ItemViewModel>> GetSellerRecentSalesAsync(int sellerId, int count)
        {
            var items = await _context.Items
                .Where(i => i.SellerId == sellerId && i.Status == "S")
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .OrderByDescending(i => i.EndDate)
                .Take(count)
                .ToListAsync();

            var result = new List<ItemViewModel>();
            foreach (var item in items)
            {
                var viewModel = await MapToViewModelAsync(item);
                result.Add(viewModel);
            }
            return result;
        }
    }
}
