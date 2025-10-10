using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BidNest.Models;
using BidNest.ViewModels;
using BidNest.Services;
using System.Security.Claims;

namespace BidNest.Controllers
{
    [Authorize]
    public class SellerController : Controller
    {
        private readonly BidnestContext _context;
        private readonly IItemService _itemService;
        private readonly IImageService _imageService;
        private readonly IItemStatusService _itemStatusService;
        private readonly ILogger<SellerController> _logger;

        public SellerController(
            BidnestContext context,
            IItemService itemService,
            IImageService imageService,
            IItemStatusService itemStatusService,
            ILogger<SellerController> logger)
        {
            _context = context;
            _itemService = itemService;
            _imageService = imageService;
            _itemStatusService = itemStatusService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        // GET: /Seller/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new SellerDashboardViewModel
            {
                SellerId = userId,
                ActiveListings = await _itemService.GetSellerActiveItemsAsync(userId),
                PendingListings = await _itemService.GetSellerPendingItemsAsync(userId),
                SoldItems = await _itemService.GetSellerSoldItemsAsync(userId),
                TotalEarnings = await _itemService.GetSellerTotalEarningsAsync(userId),
                RecentBids = await _itemService.GetSellerRecentBidsAsync(userId, 10)
            };

            return View(viewModel);
        }

        // GET: /Seller/Create
        public async Task<IActionResult> Create()
        {
            var model = new ItemCreateViewModel();
            await PopulateDropdowns(model);
            return View(model);
        }

        // POST: /Seller/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemCreateViewModel model)
        {
            // Additional business validation
            if (model.MinimumBid <= 0)
            {
                ModelState.AddModelError(nameof(model.MinimumBid), "Minimum bid must be greater than zero.");
            }

            if (model.AuctionDurationDays < 1 || model.AuctionDurationDays > 30)
            {
                ModelState.AddModelError(nameof(model.AuctionDurationDays), "Auction duration must be between 1 and 30 days.");
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length < 5)
            {
                ModelState.AddModelError(nameof(model.Name), "Item name must be at least 5 characters long.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is blocked
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBlocked)
            {
                TempData["ErrorMessage"] = "Your account has been blocked or is invalid.";
                return RedirectToAction("Dashboard");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create the item
                var item = new Item
                {
                    SellerId = userId,
                    Title = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    MinBid = model.MinimumBid,
                    BidIncrement = Math.Max(1.00m, model.MinimumBid * 0.05m), // 5% of minimum bid or $1, whichever is higher
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(model.AuctionDurationDays),
                    Status = "P", // Pending approval
                    CreatedAt = DateTime.UtcNow
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // Handle image uploads
                if (model.Images != null && model.Images.Any())
                {
                    var imageResults = await _imageService.SaveItemImagesAsync(item.ItemId, model.Images);
                    if (!imageResults.All(r => r.Success))
                    {
                        _logger.LogWarning("Some images failed to upload for item {ItemId}", item.ItemId);
                    }
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Your item has been submitted for review. It will be available for bidding once approved by our team.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating item");
                ModelState.AddModelError("", "An error occurred while creating your listing. Please try again.");
                await PopulateDropdowns(model);
                return View(model);
            }
        }

        // GET: /Seller/MyItems
        public async Task<IActionResult> MyItems(string status = "all", int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var items = await _itemService.GetSellerItemsAsync(userId, status, page, pageSize);
                var totalItems = await _itemService.GetSellerItemsCountAsync(userId, status);

                var viewModel = new SellerItemsViewModel
                {
                    Items = items,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                    StatusFilter = status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving seller items for user {UserId}", userId);
                TempData["ErrorMessage"] = "Unable to load your items. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: /Seller/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _itemService.GetItemByIdAsync(id);

                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(MyItems));
                }

                if (item.SellerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this item.";
                    return RedirectToAction(nameof(MyItems));
                }

                // Only allow editing of pending items
                if (item.Status != "P")
                {
                    TempData["ErrorMessage"] = "You can only edit items that are pending approval.";
                    return RedirectToAction(nameof(MyItems));
                }

                var model = new ItemEditViewModel
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                    CategoryId = item.CategoryId,
                    MinimumBid = item.MinimumBid,
                    Status = item.Status,
                    Images = item.Images
                };

                await PopulateDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item {ItemId} for editing", id);
                TempData["ErrorMessage"] = "Unable to load item for editing. Please try again.";
                return RedirectToAction(nameof(MyItems));
            }
        }

        // POST: /Seller/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ItemEditViewModel model)
        {
            if (id != model.ItemId)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var item = await _context.Items.FindAsync(id);

            if (item == null || item.SellerId != userId)
            {
                return NotFound();
            }

            if (item.Status != "P")
            {
                TempData["ErrorMessage"] = "You can only edit items that are pending approval.";
                return RedirectToAction(nameof(MyItems));
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                item.Title = model.Name;
                item.Description = model.Description;
                item.CategoryId = model.CategoryId;
                item.MinBid = model.MinimumBid;

                // Handle image deletions
                if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
                {
                    await _imageService.DeleteItemImagesAsync(model.ImagesToDelete);
                }

                // Handle new image uploads
                if (model.NewImages != null && model.NewImages.Any())
                {
                    await _imageService.SaveItemImagesAsync(item.ItemId, model.NewImages);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your item has been updated successfully.";
                return RedirectToAction(nameof(MyItems));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", id);
                ModelState.AddModelError("", "An error occurred while updating your listing. Please try again.");
                await PopulateDropdowns(model);
                return View(model);
            }
        }

        // POST: /Seller/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _context.Items.FindAsync(id);

                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(MyItems));
                }

                if (item.SellerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this item.";
                    return RedirectToAction(nameof(MyItems));
                }

                // Check if item has any bids
                var bidCount = await _context.Bids.CountAsync(b => b.ItemId == id);
                if (bidCount > 0)
                {
                    TempData["ErrorMessage"] = "Cannot delete an item that has received bids.";
                    return RedirectToAction(nameof(MyItems));
                }

                // Only allow deletion of pending items or items with no bids
                if (item.Status == "A" && item.CurrentBidId.HasValue)
                {
                    TempData["ErrorMessage"] = "You cannot delete an active auction that has bids.";
                    return RedirectToAction(nameof(MyItems));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Delete associated images with error handling
                    var images = await _context.ItemImages.Where(img => img.ItemId == id).ToListAsync();
                    foreach (var image in images)
                    {
                        try
                        {
                            await _imageService.DeleteImageAsync(image.Url);
                            _context.ItemImages.Remove(image);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error deleting image {ImageUrl} for item {ItemId}", image.Url, id);
                            // Continue with deletion even if some images fail
                        }
                    }

                    _context.Items.Remove(item);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Your item has been deleted successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting item {ItemId}", id);
                    TempData["ErrorMessage"] = "An error occurred while deleting your listing. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete method for item {ItemId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting your listing. Please try again.";
            }

            return RedirectToAction(nameof(MyItems));
        }

        // GET: /Seller/Analytics
        public async Task<IActionResult> Analytics()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Process any expired items first to ensure up-to-date sales data
                await _itemStatusService.ProcessExpiredItemsAsync();
                
                var viewModel = new SellerAnalyticsViewModel
                {
                    SellerId = userId,
                    TotalListings = await _itemService.GetSellerTotalListingsAsync(userId),
                    ActiveListings = await _itemService.GetSellerActiveListingsCountAsync(userId),
                    SoldItems = await _itemService.GetSellerSoldItemsCountAsync(userId),
                    TotalEarnings = await _itemService.GetSellerTotalEarningsAsync(userId),
                    AverageSellingPrice = await _itemService.GetSellerAverageSellingPriceAsync(userId),
                    MonthlyEarnings = await _itemService.GetSellerMonthlyEarningsAsync(userId, 12),
                    TopCategories = await _itemService.GetSellerTopCategoriesAsync(userId, 5),
                    RecentSales = await _itemService.GetSellerRecentSalesAsync(userId, 10)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics for user {UserId}", userId);
                TempData["ErrorMessage"] = "Unable to load analytics. Please try again.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: /Seller/ExtendAuction/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendAuction(int id, int hours = 24)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _context.Items.FindAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(MyItems));
                }

                if (item.SellerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to extend this auction.";
                    return RedirectToAction(nameof(MyItems));
                }

                // Extend the auction
                item.EndDate = DateTime.UtcNow.AddHours(hours);
                
                // If the item was ended, reactivate it
                if (item.Status == "E")
                {
                    item.Status = "A";
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Auction extended by {hours} hours.";
                return RedirectToAction("Details", "Auction", new { id = item.ItemId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending auction {ItemId}", id);
                TempData["ErrorMessage"] = "Unable to extend auction. Please try again.";
                return RedirectToAction(nameof(MyItems));
            }
        }

        // POST: /Seller/ProcessExpiredItems (for testing/manual processing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessExpiredItems()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                await _itemStatusService.ProcessExpiredItemsAsync();
                TempData["SuccessMessage"] = "Expired items processed successfully. Sales data updated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired items manually");
                TempData["ErrorMessage"] = "Error processing expired items. Please try again.";
            }

            return RedirectToAction("Analytics");
        }

        // POST: /Seller/ExpireAuction/5 (for testing - manually expire an auction)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpireAuction(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _context.Items.FindAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(MyItems));
                }

                if (item.SellerId != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to modify this auction.";
                    return RedirectToAction(nameof(MyItems));
                }

                // Set end date to past to trigger expiration processing
                item.EndDate = DateTime.UtcNow.AddMinutes(-1);
                await _context.SaveChangesAsync();

                // Process the expired item immediately
                await _itemStatusService.ProcessExpiredItemsAsync();

                TempData["SuccessMessage"] = "Auction expired and processed. Check your analytics for updated sales data.";
                return RedirectToAction("Analytics");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring auction {ItemId}", id);
                TempData["ErrorMessage"] = "Unable to expire auction. Please try again.";
                return RedirectToAction(nameof(MyItems));
            }
        }

        private async Task PopulateDropdowns<T>(T model) where T : class
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { c.CategoryId, c.Name })
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
        }
    }
}
