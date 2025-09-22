using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BidNest.Services;
using BidNest.ViewModels;
using System.Security.Claims;

namespace BidNest.Controllers
{
    public class AuctionController : Controller
    {
        private readonly IAuctionService _auctionService;
        private readonly IItemService _itemService;
        private readonly ILogger<AuctionController> _logger;

        public AuctionController(IAuctionService auctionService, IItemService itemService, ILogger<AuctionController> logger)
        {
            _auctionService = auctionService;
            _itemService = itemService;
            _logger = logger;
        }

        
        public async Task<IActionResult> Details(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            var auctionStatus = await _auctionService.GetAuctionStatusAsync(id);
            var bidHistory = await _auctionService.GetBidHistoryAsync(id, 1, 10);
            
            ViewBag.AuctionStatus = auctionStatus;
            ViewBag.BidHistory = bidHistory;
            
            return View(item);
        }

        // GET: /Auction/Bid/5
        [Authorize]
        public async Task<IActionResult> Bid(int id)
        {
            var bidFormData = await _auctionService.GetBidFormDataAsync(id);
            if (bidFormData == null)
            {
                TempData["ErrorMessage"] = "Auction not found.";
                return RedirectToAction("Details", new { id });
            }

            if (!bidFormData.IsAuctionActive)
            {
                TempData["ErrorMessage"] = "This auction has ended.";
                return RedirectToAction("Details", new { id });
            }

            return View(bidFormData);
        }

       
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bid(PlaceBidViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                TempData["ErrorMessage"] = "You must be logged in to place a bid.";
                return RedirectToAction("Login", "Account");
            }

            var result = await _auctionService.PlaceBidAsync(model.ItemId, userId, model.BidAmount);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("Details", new { id = model.ItemId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Message);
                
                // Refresh the model data
                var refreshedModel = await _auctionService.GetBidFormDataAsync(model.ItemId);
                if (refreshedModel != null)
                {
                    model.CurrentHighestBid = refreshedModel.CurrentHighestBid;
                    model.MinimumBid = refreshedModel.MinimumBid;
                    model.BidIncrement = refreshedModel.BidIncrement;
                }
                
                return View(model);
            }
        }

        // GET: /Auction/History/5
        public async Task<IActionResult> History(int id, int page = 1)
        {
            var history = await _auctionService.GetBidHistoryAsync(id, page, 20);
            if (history.ItemId == 0)
            {
                return NotFound();
            }

            return View(history);
        }

        // GET: /Auction/MyBids
        [Authorize]
        public async Task<IActionResult> MyBids(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var myBids = await _auctionService.GetMyBidsAsync(userId, page, 20);
            return View(myBids);
        }

        // GET: /Auction/Active
        public async Task<IActionResult> Active()
        {
            var activeAuctions = await _auctionService.GetActiveAuctionsAsync(50);
            return View(activeAuctions);
        }

        // GET: /Auction/Ending
        public async Task<IActionResult> Ending()
        {
            var endingAuctions = await _auctionService.GetEndingAuctionsAsync(24);
            return View(endingAuctions);
        }

        // GET: /Auction/Completed
        public async Task<IActionResult> Completed(int page = 1)
        {
            var completedAuctions = await _auctionService.GetCompletedAuctionsAsync(DateTime.UtcNow.AddDays(-30), 50);
            return View(completedAuctions);
        }

        // API endpoints for AJAX/SignalR
        [HttpGet]
        public async Task<IActionResult> GetAuctionStatus(int id)
        {
            var status = await _auctionService.GetAuctionStatusAsync(id);
            if (status == null)
            {
                return NotFound();
            }
            return Json(status);
        }

        [HttpGet]
        public async Task<IActionResult> GetBidHistory(int id, int page = 1)
        {
            var history = await _auctionService.GetBidHistoryAsync(id, page, 10);
            return Json(history);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentBids(int id, int count = 5)
        {
            var bids = await _auctionService.GetRecentBidsAsync(id, count);
            return Json(bids);
        }

        // POST: /Auction/PlaceBidAjax
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBidAjax(int itemId, decimal bidAmount)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "You must be logged in to place a bid." });
            }

            var result = await _auctionService.PlaceBidAsync(itemId, userId, bidAmount);
            
            return Json(new { 
                success = result.Success, 
                message = result.Message, 
                bid = result.Bid 
            });
        }

        // GET: /Auction/Statistics
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Statistics()
        {
            var stats = await _auctionService.GetAuctionStatisticsAsync();
            var topBidders = await _auctionService.GetTopBiddersAsync(10);
            
            ViewBag.TopBidders = topBidders;
            return View(stats);
        }

        // Helper method to validate bid amount
        [HttpPost]
        public async Task<IActionResult> ValidateBid(int itemId, decimal bidAmount)
        {
            var userId = GetCurrentUserId();
            var isValid = await _auctionService.ValidateBidAsync(itemId, bidAmount, userId);
            
            if (!isValid)
            {
                var bidFormData = await _auctionService.GetBidFormDataAsync(itemId);
                var nextMinimum = bidFormData?.NextMinimumBid ?? 0;
                return Json(new { 
                    valid = false, 
                    message = $"Bid must be at least ${nextMinimum:N2}" 
                });
            }
            
            return Json(new { valid = true });
        }

        // Quick bid suggestions
        [HttpGet]
        public async Task<IActionResult> GetQuickBids(int itemId)
        {
            var bidFormData = await _auctionService.GetBidFormDataAsync(itemId);
            if (bidFormData == null)
            {
                return Json(new { error = "Auction not found" });
            }

            return Json(new { 
                suggestions = bidFormData.SuggestedBids,
                nextMinimum = bidFormData.NextMinimumBid
            });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }
    }
}
