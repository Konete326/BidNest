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
            var item = await _context.Items.FindAsync(model.ItemId);
            if (item == null)
                throw new ArgumentException("Item not found");

            item.Title = model.Name;
            item.Description = model.Description;
            item.CategoryId = model.CategoryId;
            item.MinBid = model.MinimumBid;

            // Handle status change
            if (!string.IsNullOrEmpty(model.NewStatus) && model.NewStatus != item.Status)
            {
                item.Status = model.NewStatus;
            }

            // Handle auction extension
            if (model.ExtendDays.HasValue && model.ExtendDays > 0)
            {
                item.EndDate = item.EndDate.AddDays(model.ExtendDays.Value);
            }

            await _context.SaveChangesAsync();

            // Handle image management
            if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
            {
                foreach (var imageId in model.ImagesToDelete)
                {
                    await RemoveItemImageAsync(model.ItemId, imageId);
                }
            }

            if (model.NewImages != null && model.NewImages.Any())
            {
                var imagePaths = await _imageService.UploadImagesAsync(model.NewImages);
                await AddItemImagesAsync(model.ItemId, imagePaths);
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

        public async Task<List<ItemViewModel>> GetFeaturedItemsAsync(int count = 8)
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
                BuyNowPrice = null, // Not in current schema
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                IsFeatured = false, // Not in current schema
                SellerId = item.SellerId,
                SellerName = item.Seller?.FullName,
                CurrentBid = currentBid > 0 ? currentBid : null,
                BidCount = bidCount,
                CreatedAt = item.CreatedAt,
                UpdatedAt = null, // Not in current schema
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
    }
}
