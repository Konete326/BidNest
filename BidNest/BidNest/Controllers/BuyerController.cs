using BidNest.Models;
using BidNest.Services;
using BidNest.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BidNest.Controllers
{
    [Authorize]
    public class BuyerController : Controller
    {
        private readonly BidnestContext _context;
        private readonly ILogger<BuyerController> _logger;

        public BuyerController(BidnestContext context, ILogger<BuyerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Dashboard - Shows active auctions and user's bids
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            
            var viewModel = new BuyerDashboardViewModel
            {
                // Get user's active bids
                MyActiveBids = await _context.Bids
                    .Include(b => b.Item)
                        .ThenInclude(i => i.Category)
                    .Include(b => b.Item)
                        .ThenInclude(i => i.ItemImages)
                    .Where(b => b.BidderId == userId && b.Item.Status == "A")
                    .OrderByDescending(b => b.BidTime)
                    .Select(b => new BidItemViewModel
                    {
                        BidId = b.BidId,
                        ItemId = b.Item.ItemId,
                        ItemName = b.Item.Title,
                        ItemDescription = b.Item.Description,
                        CategoryName = b.Item.Category != null ? b.Item.Category.Name : "Uncategorized",
                        MyBidAmount = b.Amount,
                        CurrentPrice = b.Item.CurrentPrice ?? b.Item.MinBid,
                        EndDate = b.Item.EndDate,
                        IsWinning = b.Item.CurrentBidId == b.BidId,
                        ImageUrl = b.Item.ItemImages.FirstOrDefault() != null ? 
                                  b.Item.ItemImages.First().Url : "/images/no-image.jpg",
                        TimeRemaining = b.Item.EndDate - DateTime.UtcNow
                    })
                    .Take(10)
                    .ToListAsync(),

                // Get featured active auctions
                FeaturedAuctions = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemImages)
                    .Include(i => i.Seller)
                    .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                    .OrderBy(i => i.EndDate)
                    .Select(i => new AuctionItemViewModel
                    {
                        ItemId = i.ItemId,
                        Title = i.Title,
                        Description = i.Description,
                        CategoryName = i.Category != null ? i.Category.Name : "Uncategorized",
                        CurrentPrice = i.CurrentPrice ?? i.MinBid,
                        MinBid = i.MinBid,
                        EndDate = i.EndDate,
                        BidCount = i.Bids.Count(),
                        SellerName = i.Seller.FullName ?? i.Seller.Username,
                        ImageUrl = i.ItemImages.FirstOrDefault() != null ? 
                                  i.ItemImages.First().Url : "/images/no-image.jpg",
                        TimeRemaining = i.EndDate - DateTime.UtcNow
                    })
                    .Take(12)
                    .ToListAsync(),

                // Get watchlist items
                WatchlistItems = await _context.Watchlists
                    .Include(w => w.Item)
                        .ThenInclude(i => i.Category)
                    .Include(w => w.Item)
                        .ThenInclude(i => i.ItemImages)
                    .Where(w => w.UserId == userId && w.Item.Status == "A")
                    .Select(w => new AuctionItemViewModel
                    {
                        ItemId = w.Item.ItemId,
                        Title = w.Item.Title,
                        Description = w.Item.Description,
                        CategoryName = w.Item.Category != null ? w.Item.Category.Name : "Uncategorized",
                        CurrentPrice = w.Item.CurrentPrice ?? w.Item.MinBid,
                        MinBid = w.Item.MinBid,
                        EndDate = w.Item.EndDate,
                        BidCount = w.Item.Bids.Count(),
                        ImageUrl = w.Item.ItemImages.FirstOrDefault() != null ? 
                                  w.Item.ItemImages.First().Url : "/images/no-image.jpg",
                        TimeRemaining = w.Item.EndDate - DateTime.UtcNow
                    })
                    .ToListAsync(),

                // Get recently won auctions
                RecentlyWon = await _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemImages)
                    .Where(i => i.Status == "S" && 
                           i.CurrentBidId != null &&
                           i.Bids.Any(b => b.BidId == i.CurrentBidId && b.BidderId == userId))
                    .OrderByDescending(i => i.EndDate)
                    .Select(i => new WonAuctionViewModel
                    {
                        ItemId = i.ItemId,
                        ItemName = i.Title,
                        WinningBid = i.CurrentPrice ?? 0,
                        EndDate = i.EndDate,
                        PaymentStatus = "Pending", // This would come from a Payment table
                        ImageUrl = i.ItemImages.FirstOrDefault() != null ? 
                                  i.ItemImages.First().Url : "/images/no-image.jpg"
                    })
                    .Take(5)
                    .ToListAsync()
            };

            // Get categories for the filter
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(viewModel);
        }

        // Browse items with search and filter
        public async Task<IActionResult> Browse(string search, int? categoryId, string sortBy = "ending", int page = 1)
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Include(i => i.Bids)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => i.Title.Contains(search) || 
                                        (i.Description != null && i.Description.Contains(search)));
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == categoryId);
            }

            // Apply sorting
            query = sortBy switch
            {
                "price_low" => query.OrderBy(i => i.CurrentPrice ?? i.MinBid),
                "price_high" => query.OrderByDescending(i => i.CurrentPrice ?? i.MinBid),
                "newest" => query.OrderByDescending(i => i.CreatedAt),
                "popular" => query.OrderByDescending(i => i.Bids.Count()),
                _ => query.OrderBy(i => i.EndDate) // ending soon (default)
            };

            var pageSize = 12;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new AuctionItemViewModel
                {
                    ItemId = i.ItemId,
                    Title = i.Title,
                    Description = i.Description,
                    CategoryName = i.Category != null ? i.Category.Name : "Uncategorized",
                    CurrentPrice = i.CurrentPrice ?? i.MinBid,
                    MinBid = i.MinBid,
                    EndDate = i.EndDate,
                    BidCount = i.Bids.Count(),
                    SellerName = i.Seller.FullName ?? i.Seller.Username,
                    ImageUrl = i.ItemImages.FirstOrDefault() != null ? 
                              i.ItemImages.First().Url : "/images/no-image.jpg",
                    TimeRemaining = i.EndDate - DateTime.UtcNow
                })
                .ToListAsync();

            var viewModel = new BrowseViewModel
            {
                Items = items,
                CurrentPage = page,
                TotalPages = totalPages,
                SearchTerm = search,
                CategoryId = categoryId,
                SortBy = sortBy
            };

            // Get categories for the filter
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(viewModel);
        }

        // View item details
        public async Task<IActionResult> ItemDetails(int id)
        {
            var userId = GetCurrentUserId();

            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Include(i => i.Bids)
                    .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(i => i.ItemId == id);

            if (item == null)
            {
                return NotFound();
            }

            var viewModel = new ItemDetailsViewModel
            {
                ItemId = item.ItemId,
                Title = item.Title,
                Description = item.Description,
                CategoryName = item.Category?.Name ?? "Uncategorized",
                CurrentPrice = item.CurrentPrice ?? item.MinBid,
                MinBid = item.MinBid,
                BidIncrement = item.BidIncrement,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Status = item.Status,
                BidCount = item.Bids.Count(),
                SellerName = item.Seller.FullName ?? item.Seller.Username,
                SellerId = item.SellerId,
                Images = item.ItemImages.Select(img => img.Url).ToList(),
                TimeRemaining = item.EndDate - DateTime.UtcNow,
                IsInWatchlist = await _context.Watchlists
                    .AnyAsync(w => w.UserId == userId && w.ItemId == id),
                UserHighestBid = item.Bids
                    .Where(b => b.BidderId == userId)
                    .OrderByDescending(b => b.Amount)
                    .Select(b => b.Amount)
                    .FirstOrDefault(),
                IsWinning = item.CurrentBidId != null && 
                           item.Bids.Any(b => b.BidId == item.CurrentBidId && b.BidderId == userId),
                BidHistory = item.Bids
                    .OrderByDescending(b => b.BidTime)
                    .Take(10)
                    .Select(b => new BidHistoryItemViewModel
                    {
                        BidderName = b.BidderId == userId ? "You" : 
                                    (b.Bidder.FullName ?? b.Bidder.Username).Substring(0, 3) + "***",
                        Amount = b.Amount,
                        BidTime = b.BidTime,
                        IsYourBid = b.BidderId == userId
                    })
                    .ToList(),
                NextMinimumBid = (item.CurrentPrice ?? item.MinBid) + item.BidIncrement
            };

            return View(viewModel);
        }

        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBid(int itemId, decimal bidAmount)
        {
            var userId = GetCurrentUserId();

            // Check if user is blocked
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBlocked)
            {
                return Json(new { success = false, message = "Your account has been blocked or is invalid." });
            }

            // Start a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get item with lock to prevent concurrent bid issues

                var item = await _context.Items
                    .Include(i => i.Bids)
                    .FirstOrDefaultAsync(i => i.ItemId == itemId);

                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                // Validate auction is active
                if (item.Status != "A" || item.EndDate <= DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "This auction has ended." });
                }

                // Validate bid amount
                var currentPrice = item.CurrentPrice ?? item.MinBid;
                var minimumBid = currentPrice + item.BidIncrement;

                if (bidAmount < minimumBid)
                {
                    return Json(new { success = false, message = $"Bid must be at least ${minimumBid:F2}" });
                }

                // Check if user is the seller
                if (item.SellerId == userId)
                {
                    return Json(new { success = false, message = "You cannot bid on your own item." });
                }

                // Check if user is already the highest bidder
                if (item.CurrentBidId != null)
                {
                    var currentHighestBid = await _context.Bids
                        .FirstOrDefaultAsync(b => b.BidId == item.CurrentBidId);
                    
                    if (currentHighestBid?.BidderId == userId)
                    {
                        return Json(new { success = false, message = "You are already the highest bidder." });
                    }
                }

                // Create new bid
                var bid = new Bid
                {
                    ItemId = itemId,
                    BidderId = userId,
                    Amount = bidAmount,
                    BidTime = DateTime.UtcNow
                };

                _context.Bids.Add(bid);
                await _context.SaveChangesAsync();

                // Update item's current price and bid
                item.CurrentPrice = bidAmount;
                item.CurrentBidId = bid.BidId;
                await _context.SaveChangesAsync();

                // Create notification for the previous highest bidder
                if (item.CurrentBidId != null)
                {
                    var previousBid = await _context.Bids
                        .Where(b => b.ItemId == itemId && b.BidId != bid.BidId)
                        .OrderByDescending(b => b.Amount)
                        .FirstOrDefaultAsync();

                    if (previousBid != null)
                    {
                        var notification = new Notification
                        {
                            UserId = previousBid.BidderId,
                            Message = $"You have been outbid on '{item.Title}'",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        _context.Notifications.Add(notification);
                    }
                }

                // Create notification for seller
                var sellerNotification = new Notification
                {
                    UserId = item.SellerId,
                    Message = $"New bid of ${bidAmount:F2} on your item '{item.Title}'",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(sellerNotification);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new 
                { 
                    success = true, 
                    message = "Bid placed successfully!", 
                    newPrice = bidAmount,
                    nextMinimumBid = bidAmount + item.BidIncrement
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing bid for item {ItemId}", itemId);
                return Json(new { success = false, message = "An error occurred while placing your bid." });
            }
        }

        // Add/Remove from watchlist
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleWatchlist(int itemId)
        {
            var userId = GetCurrentUserId();

            var existingWatch = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ItemId == itemId);

            if (existingWatch != null)
            {
                _context.Watchlists.Remove(existingWatch);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isInWatchlist = false, message = "Removed from watchlist" });
            }
            else
            {
                var watchlist = new Watchlist
                {
                    UserId = userId,
                    ItemId = itemId,
                    AddedAt = DateTime.UtcNow
                };
                _context.Watchlists.Add(watchlist);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isInWatchlist = true, message = "Added to watchlist" });
            }
        }

        // Check if item is in watchlist
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IsInWatchlist(int itemId)
        {
            var userId = GetCurrentUserId();
            
            var isInWatchlist = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.ItemId == itemId);
                
            return Json(new { isInWatchlist = isInWatchlist });
        }

        // View my bids
        public async Task<IActionResult> MyBids(string filter = "active")
        {
            var userId = GetCurrentUserId();

            var query = _context.Bids
                .Include(b => b.Item)
                    .ThenInclude(i => i.Category)
                .Include(b => b.Item)
                    .ThenInclude(i => i.ItemImages)
                .Where(b => b.BidderId == userId);

            // Apply filter
            query = filter switch
            {
                "won" => query.Where(b => b.Item.Status == "S" && b.Item.CurrentBidId == b.BidId),
                "lost" => query.Where(b => b.Item.Status == "S" && b.Item.CurrentBidId != b.BidId),
                _ => query.Where(b => b.Item.Status == "A") // active (default)
            };

            var bids = await query
                .OrderByDescending(b => b.BidTime)
                .Select(b => new BidItemViewModel
                {
                    BidId = b.BidId,
                    ItemId = b.Item.ItemId,
                    ItemName = b.Item.Title,
                    ItemDescription = b.Item.Description,
                    CategoryName = b.Item.Category != null ? b.Item.Category.Name : "Uncategorized",
                    MyBidAmount = b.Amount,
                    CurrentPrice = b.Item.CurrentPrice ?? b.Item.MinBid,
                    EndDate = b.Item.EndDate,
                    Status = b.Item.Status,
                    IsWinning = b.Item.CurrentBidId == b.BidId,
                    ImageUrl = b.Item.ItemImages.FirstOrDefault() != null ? 
                              b.Item.ItemImages.First().Url : "/images/no-image.jpg",
                    TimeRemaining = b.Item.EndDate - DateTime.UtcNow,
                    BidTime = b.BidTime
                })
                .ToListAsync();

            ViewBag.Filter = filter;
            return View(bids);
        }

        // View won auctions
        public async Task<IActionResult> WonAuctions()
        {
            var userId = GetCurrentUserId();

            var wonItems = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Where(i => i.Status == "S" && 
                       i.CurrentBidId != null &&
                       i.Bids.Any(b => b.BidId == i.CurrentBidId && b.BidderId == userId))
                .OrderByDescending(i => i.EndDate)
                .Select(i => new WonAuctionViewModel
                {
                    ItemId = i.ItemId,
                    ItemName = i.Title,
                    Description = i.Description,
                    WinningBid = i.CurrentPrice ?? 0,
                    EndDate = i.EndDate,
                    SellerName = i.Seller.FullName ?? i.Seller.Username,
                    PaymentStatus = "Pending", // This would come from a Payment table
                    ImageUrl = i.ItemImages.FirstOrDefault() != null ? 
                              i.ItemImages.First().Url : "/images/no-image.jpg"
                })
                .ToListAsync();

            return View(wonItems);
        }

        // Payment process (placeholder)
        [HttpGet]
        public async Task<IActionResult> Payment(int itemId)
        {
            var userId = GetCurrentUserId();

            var item = await _context.Items
                .Include(i => i.Seller)
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync(i => i.ItemId == itemId && 
                                          i.Status == "S" &&
                                          i.CurrentBidId != null &&
                                          i.Bids.Any(b => b.BidId == i.CurrentBidId && b.BidderId == userId));

            if (item == null)
            {
                return NotFound();
            }

            var viewModel = new PaymentViewModel
            {
                ItemId = item.ItemId,
                ItemName = item.Title,
                Amount = item.CurrentPrice ?? 0,
                SellerName = item.Seller.FullName ?? item.Seller.Username,
                ImageUrl = item.ItemImages.FirstOrDefault()?.Url ?? "/images/no-image.jpg"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            // This is a placeholder for payment processing
            // In a real application, you would integrate with a payment gateway
            
            TempData["SuccessMessage"] = "Payment processed successfully!";
            return RedirectToAction(nameof(WonAuctions));
        }

        private int GetCurrentUserId()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return 0;
                
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
