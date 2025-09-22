using BidNest.Models;
using BidNest.Services;
using BidNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BidnestContext _context;
        private readonly IItemService _itemService;
        private readonly IImageService _imageService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(BidnestContext context, IItemService itemService, IImageService imageService, ILogger<AdminController> logger)
        {
            _context = context;
            _itemService = itemService;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => !u.IsBlocked),
                BlockedUsers = await _context.Users.CountAsync(u => u.IsBlocked),
                TotalItems = await _context.Items.CountAsync(),
                ActiveAuctions = await _context.Items.CountAsync(i => i.Status == "A"),
                TotalBids = await _context.Bids.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(c => c.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserBlock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.RoleId == 1) // Don't allow blocking admin users
            {
                TempData["ErrorMessage"] = "Cannot block admin users.";
                return RedirectToAction("Users");
            }

            user.IsBlocked = !user.IsBlocked;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsBlocked ? 
                $"User {user.Username} has been blocked." : 
                $"User {user.Username} has been unblocked.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.RoleId == 1) 
            {
                TempData["ErrorMessage"] = "Cannot delete admin users.";
                return RedirectToAction("Users");
            }

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.Username} has been deleted.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Cannot delete user. User may have associated data.";
            }

            return RedirectToAction("Users");
        }

        // Category Management Methods
        public async Task<IActionResult> Categories(string? search, bool showInactive = false)
        {
            var query = _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .AsQueryable();

            if (!showInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            var categories = await query
                .OrderBy(c => c.Parent != null ? c.Parent.Name : c.Name)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var model = new CategoryListViewModel
            {
                SearchTerm = search,
                ShowInactive = showInactive,
                Categories = new List<CategoryViewModel>()
            };

            foreach (var category in categories)
            {
                var viewModel = new CategoryViewModel
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Description = null,
                    ParentId = category.ParentId,
                    IsActive = category.IsActive,
                    ParentName = category.Parent?.Name,
                    ItemCount = await _context.Items.CountAsync(i => i.CategoryId == category.CategoryId),
                    SubCategoryCount = category.InverseParent?.Count ?? 0
                };
                model.Categories.Add(viewModel);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCategory(int? parentId = null)
        {
            var model = new CategoryViewModel
            {
                ParentId = parentId,
                IsActive = true
            };

            await PopulateParentCategoriesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = new Category
                    {
                        Name = model.Name,
                        ParentId = model.ParentId,
                        IsActive = model.IsActive
                    };

                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Category created successfully.";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error creating category: {ex.Message}");
                }
            }

            await PopulateParentCategoriesAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = null,
                ParentId = category.ParentId,
                IsActive = category.IsActive,
                ParentName = category.Parent?.Name,
                ItemCount = await _context.Items.CountAsync(i => i.CategoryId == category.CategoryId),
                SubCategoryCount = category.InverseParent?.Count ?? 0
            };

            await PopulateParentCategoriesAsync(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.Categories.FindAsync(model.CategoryId);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Name = model.Name;
                    category.ParentId = model.ParentId;
                    category.IsActive = model.IsActive;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Category updated successfully.";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error updating category: {ex.Message}");
                }
            }

            await PopulateParentCategoriesAsync(model.CategoryId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CategoryDetails(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = null,
                ParentId = category.ParentId,
                IsActive = category.IsActive,
                ParentName = category.Parent?.Name,
                ItemCount = await _context.Items.CountAsync(i => i.CategoryId == category.CategoryId),
                SubCategoryCount = category.InverseParent?.Count ?? 0
            };

            // Load subcategories
            foreach (var subCat in category.InverseParent)
            {
                var subModel = new CategoryViewModel
                {
                    CategoryId = subCat.CategoryId,
                    Name = subCat.Name,
                    IsActive = subCat.IsActive,
                    ItemCount = await _context.Items.CountAsync(i => i.CategoryId == subCat.CategoryId)
                };
                model.SubCategories.Add(subModel);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category != null)
                {
                    category.IsActive = !category.IsActive;
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Category {(category.IsActive ? "activated" : "deactivated")} successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating category status: {ex.Message}";
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryViewModel
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = null,
                ParentId = category.ParentId,
                IsActive = category.IsActive,
                ParentName = category.Parent?.Name,
                ItemCount = await _context.Items.CountAsync(i => i.CategoryId == category.CategoryId),
                SubCategoryCount = category.InverseParent?.Count ?? 0
            };

            // Check if category can be deleted
            var canDelete = model.ItemCount == 0 && model.SubCategoryCount == 0;
            ViewBag.CanDelete = canDelete;

            return View(model);
        }

        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.InverseParent)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                // Check if category has items or subcategories
                var hasItems = await _context.Items.AnyAsync(i => i.CategoryId == id);
                if (hasItems || category.InverseParent.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete category. It may have items or subcategories.";
                    return RedirectToAction(nameof(Categories));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting category: {ex.Message}";
            }

            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> MergeCategories()
        {
            var model = new CategoryMergeViewModel();
            await PopulateCategoriesForMergeAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MergeCategories(CategoryMergeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var success = await MergeCategoriesAsync(
                        model.SourceCategoryId, 
                        model.TargetCategoryId, 
                        model.DeleteSourceCategory);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Categories merged successfully.";
                        return RedirectToAction(nameof(Categories));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Failed to merge categories. Please check if both categories exist and are different.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error merging categories: {ex.Message}");
                }
            }

            await PopulateCategoriesForMergeAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CategoryHierarchy()
        {
            var categories = await GetCategoryHierarchyAsync();
            return View(categories);
        }

        private async Task<bool> MergeCategoriesAsync(int sourceCategoryId, int targetCategoryId, bool deleteSource = true)
        {
            if (sourceCategoryId == targetCategoryId)
                return false;

            var sourceCategory = await _context.Categories
                .Include(c => c.InverseParent)
                .FirstOrDefaultAsync(c => c.CategoryId == sourceCategoryId);

            var targetCategory = await _context.Categories.FindAsync(targetCategoryId);

            if (sourceCategory == null || targetCategory == null)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Move all items from source to target category
                var itemsToMove = await _context.Items
                    .Where(i => i.CategoryId == sourceCategoryId)
                    .ToListAsync();

                foreach (var item in itemsToMove)
                {
                    item.CategoryId = targetCategoryId;
                }

                // Move all subcategories from source to target category
                var subCategoriesToMove = sourceCategory.InverseParent.ToList();
                foreach (var subCategory in subCategoriesToMove)
                {
                    subCategory.ParentId = targetCategoryId;
                }

                await _context.SaveChangesAsync();

                // Delete source category if requested
                if (deleteSource)
                {
                    _context.Categories.Remove(sourceCategory);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        private async Task<List<CategoryViewModel>> GetCategoryHierarchyAsync(int? parentId = null)
        {
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .Include(c => c.InverseParent)
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var result = new List<CategoryViewModel>();
            foreach (var category in categories)
            {
                var viewModel = new CategoryViewModel
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Description = null,
                    ParentId = category.ParentId,
                    IsActive = category.IsActive,
                    ParentName = category.Parent?.Name,
                    ItemCount = await _context.Items.CountAsync(i => i.CategoryId == category.CategoryId),
                    SubCategoryCount = category.InverseParent?.Count ?? 0,
                    SubCategories = await GetCategoryHierarchyAsync(category.CategoryId)
                };
                result.Add(viewModel);
            }

            return result;
        }

        private async Task PopulateCategoriesForMergeAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
        }

        private async Task PopulateParentCategoriesAsync(int? excludeId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            if (excludeId.HasValue)
            {
                categories = categories.Where(c => c.CategoryId != excludeId.Value).ToList();
            }

            ViewBag.ParentCategories = new SelectList(categories, "CategoryId", "Name");
        }

        // Item Management Methods
        public async Task<IActionResult> Items(string? search, int? categoryId, string? status, string? sortBy, int page = 1)
        {
            var model = await _itemService.GetItemsAsync(search, categoryId, status, sortBy, false, page, 20);
            
            // Populate categories for filter dropdown
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            model.Categories = categories.Select(c => new CategoryViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateItem()
        {
            var model = new ItemCreateViewModel();
            await PopulateCategoriesForItemAsync();
            await PopulateSellersAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(ItemCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Use current user as seller if not specified
                    var sellerId = model.SellerId ?? GetCurrentUserId();
                    
                    var item = await _itemService.CreateItemAsync(model, sellerId);
                    TempData["SuccessMessage"] = "Item created successfully.";
                    return RedirectToAction(nameof(Items));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error creating item: {ex.Message}");
                }
            }

            await PopulateCategoriesForItemAsync();
            await PopulateSellersAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditItem(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var model = new ItemEditViewModel
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Description = item.Description,
                CategoryId = item.CategoryId,
                MinimumBid = item.MinimumBid,
                BuyNowPrice = null, // Not in database schema
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                IsFeatured = false, // Not in database schema
                SellerId = item.SellerId,
                Images = item.Images,
                CurrentBid = item.CurrentBid,
                BidCount = item.BidCount,
                CreatedAt = item.CreatedAt,
                UpdatedAt = null // Not in database schema
            };

            await PopulateCategoriesForItemAsync();
            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(ItemEditViewModel model)
        {
            _logger.LogInformation("=== ADMIN EDIT ITEM POST METHOD CALLED ===");
            _logger.LogInformation("EditItem POST called for ItemId: {ItemId}", model?.ItemId ?? 0);

            if (model == null)
            {
                _logger.LogError("Model is null in EditItem POST");
                TempData["ErrorMessage"] = "No data received. Please try again.";
                return RedirectToAction(nameof(Items));
            }

            _logger.LogInformation("Received model data - ItemId: {ItemId}, Name: {Name}, Description: {Description}, CategoryId: {CategoryId}, MinimumBid: {MinimumBid}",
                model.ItemId, model.Name ?? "NULL", model.Description ?? "NULL", model.CategoryId, model.MinimumBid);

            // Enforce server-side validation
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("EditItem ModelState invalid for ItemId {ItemId}. Errors: {Errors}", model.ItemId, errors);
                TempData["ErrorMessage"] = "Please correct the highlighted errors and try again.";
                await PopulateCategoriesForItemAsync();
                return View(model);
            }

            try
            {
                _logger.LogInformation("Attempting to update item {ItemId}", model.ItemId);
                await _itemService.UpdateItemAsync(model);

                _logger.LogInformation("Item {ItemId} updated successfully", model.ItemId);
                TempData["SuccessMessage"] = "Item updated successfully.";

                // Redirect to Item Details for better UX
                return RedirectToAction(nameof(ItemDetails), new { id = model.ItemId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {ItemId}", model.ItemId);
                ModelState.AddModelError(string.Empty, $"Error updating item: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the item.";

                await PopulateCategoriesForItemAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ItemDetails(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            ViewBag.CanDelete = item.BidCount == 0;
            return View(item);
        }

        [HttpPost, ActionName("DeleteItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItemConfirmed(int id)
        {
            try
            {
                var success = await _itemService.DeleteItemAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Item deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete item.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting item: {ex.Message}";
            }

            return RedirectToAction(nameof(Items));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItemStatus(int id, string status)
        {
            try
            {
                var success = await _itemService.UpdateItemStatusAsync(id, status);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Item status updated to {status}.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update item status.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating item status: {ex.Message}";
            }

            return RedirectToAction(nameof(Items));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            try
            {
                var item = await _itemService.GetItemByIdAsync(id);
                if (item != null)
                {
                    var success = await _itemService.SetFeaturedStatusAsync(id, !item.IsFeatured);
                    if (success)
                    {
                        TempData["SuccessMessage"] = $"Item {(item.IsFeatured ? "removed from" : "added to")} featured items.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating featured status: {ex.Message}";
            }

            return RedirectToAction(nameof(Items));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendAuction(int id, int days)
        {
            try
            {
                var success = await _itemService.ExtendAuctionAsync(id, days);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Auction extended by {days} day(s).";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to extend auction.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error extending auction: {ex.Message}";
            }

            return RedirectToAction(nameof(ItemDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItemImage(int itemId, int imageId)
        {
            try
            {
                var success = await _itemService.RemoveItemImageAsync(itemId, imageId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Image removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to remove image.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error removing image: {ex.Message}";
            }

            return RedirectToAction(nameof(EditItem), new { id = itemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryImage(int itemId, int imageId)
        {
            try
            {
                var success = await _itemService.SetPrimaryImageAsync(itemId, imageId);
                if (success)
                {
                    TempData["SuccessMessage"] = "Primary image updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update primary image.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating primary image: {ex.Message}";
            }

            return RedirectToAction(nameof(EditItem), new { id = itemId });
        }

        private async Task PopulateCategoriesForItemAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name");
        }

        private async Task PopulateSellersAsync()
        {
            var sellers = await _context.Users
                .Where(u => !u.IsBlocked)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.Sellers = new SelectList(sellers, "UserId", "FullName");
        }

        // GET: Admin/Bids
        public async Task<IActionResult> Bids(string search = "", int page = 1, int pageSize = 20)
        {
            var query = _context.Bids
                .Include(b => b.Item)
                .Include(b => b.Bidder)
                .AsQueryable();

            // Search functionality
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => 
                    b.Item.Title.Contains(search) ||
                    b.Bidder.FullName.Contains(search) ||
                    b.Bidder.Email.Contains(search));
            }

            // Get total count for pagination
            var totalBids = await query.CountAsync();

            // Apply pagination
            var bids = await query
                .OrderByDescending(b => b.BidTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BidNest.ViewModels.BidViewModel
                {
                    BidId = b.BidId,
                    ItemId = b.ItemId,
                    BidderId = b.BidderId,
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    IsWinning = b.IsWinning,
                    BidderName = b.Bidder.FullName,
                    ItemName = b.Item.Title
                })
                .ToListAsync();

            var viewModel = new BidNest.ViewModels.AdminBidsViewModel
            {
                Bids = bids,
                SearchTerm = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalBids = totalBids,
                TotalPages = (int)Math.Ceiling((double)totalBids / pageSize)
            };

            return View(viewModel);
        }

        // GET: Admin/PendingApprovals
        public async Task<IActionResult> PendingApprovals(int page = 1, int pageSize = 10)
        {
            var totalPending = await _context.Items.CountAsync(i => i.Status == "P");
            
            var pendingItems = await _context.Items
                .Where(i => i.Status == "P")
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .OrderBy(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemApprovalViewModel
                {
                    ItemId = i.ItemId,
                    Title = i.Title,
                    Description = i.Description ?? "",
                    CategoryName = i.Category != null ? i.Category.Name : "Uncategorized",
                    MinimumBid = i.MinBid,
                    StartDate = i.StartDate,
                    EndDate = i.EndDate,
                    SellerName = i.Seller.FullName,
                    SellerEmail = i.Seller.Email,
                    SubmittedAt = i.CreatedAt,
                    ImageUrls = i.ItemImages.OrderByDescending(img => img.IsPrimary).Select(img => img.Url).ToList()
                })
                .ToListAsync();

            var viewModel = new PendingApprovalViewModel
            {
                PendingItems = pendingItems,
                TotalPending = totalPending,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // POST: Admin/ApproveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveItem(int itemId, string? adminNotes)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null || item.Status != "P")
            {
                return Json(new { success = false, message = "Item not found or not pending approval." });
            }

            try
            {
                item.Status = "A"; // Set to Active
                item.StartDate = DateTime.UtcNow; // Start the auction now
                
                // Log the approval
                _logger.LogInformation("Item {ItemId} approved by admin {AdminId}", itemId, GetCurrentUserId());
                
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item approved successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving item {ItemId}", itemId);
                return Json(new { success = false, message = "Error approving item. Please try again." });
            }
        }

        // POST: Admin/RejectItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectItem(int itemId, string rejectionReason, string? adminNotes)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null || item.Status != "P")
            {
                return Json(new { success = false, message = "Item not found or not pending approval." });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                return Json(new { success = false, message = "Rejection reason is required." });
            }

            try
            {
                item.Status = "R"; // Set to Rejected
                
                // Log the rejection
                _logger.LogInformation("Item {ItemId} rejected by admin {AdminId}. Reason: {Reason}", 
                    itemId, GetCurrentUserId(), rejectionReason);
                
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Item rejected successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting item {ItemId}", itemId);
                return Json(new { success = false, message = "Error rejecting item. Please try again." });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 1; // Default to admin user
        }
    }
}
