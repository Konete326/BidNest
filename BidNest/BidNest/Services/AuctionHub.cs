using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BidNest.Services
{
    [Authorize]
    public class AuctionHub : Hub
    {
        private readonly IAuctionService _auctionService;
        private readonly ILogger<AuctionHub> _logger;

        public AuctionHub(IAuctionService auctionService, ILogger<AuctionHub> logger)
        {
            _auctionService = auctionService;
            _logger = logger;
        }

        public async Task JoinAuction(int itemId)
        {
            var groupName = $"auction_{itemId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} joined auction {ItemId}", userId, itemId);
            
            // Send current auction status to the newly joined user
            var auctionStatus = await _auctionService.GetAuctionStatusAsync(itemId);
            if (auctionStatus != null)
            {
                await Clients.Caller.SendAsync("AuctionStatus", auctionStatus);
            }

            // Send recent bids
            var recentBids = await _auctionService.GetRecentBidsAsync(itemId, 5);
            await Clients.Caller.SendAsync("RecentBids", recentBids);
        }

        public async Task LeaveAuction(int itemId)
        {
            var groupName = $"auction_{itemId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} left auction {ItemId}", userId, itemId);
        }

        public async Task PlaceBid(int itemId, decimal bidAmount)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    await Clients.Caller.SendAsync("BidError", "You must be logged in to place a bid.");
                    return;
                }

                var result = await _auctionService.PlaceBidAsync(itemId, userId, bidAmount);
                
                if (result.Success)
                {
                    // Bid was successful - notification will be sent via AuctionService
                    await Clients.Caller.SendAsync("BidSuccess", result.Message, result.Bid);
                }
                else
                {
                    // Bid failed
                    await Clients.Caller.SendAsync("BidError", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid via SignalR for item {ItemId}", itemId);
                await Clients.Caller.SendAsync("BidError", "An error occurred while placing your bid.");
            }
        }

        public async Task GetAuctionStatus(int itemId)
        {
            try
            {
                var status = await _auctionService.GetAuctionStatusAsync(itemId);
                await Clients.Caller.SendAsync("AuctionStatus", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auction status for item {ItemId}", itemId);
            }
        }

        public async Task GetBidHistory(int itemId, int page = 1)
        {
            try
            {
                var history = await _auctionService.GetBidHistoryAsync(itemId, page, 10);
                await Clients.Caller.SendAsync("BidHistory", history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bid history for item {ItemId}", itemId);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} connected to auction hub", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} disconnected from auction hub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }
    }

    // Extension methods for easier hub usage
    public static class AuctionHubExtensions
    {
        public static async Task NotifyBidPlaced(this IHubContext<AuctionHub> hubContext, int itemId, object bidData)
        {
            await hubContext.Clients.Group($"auction_{itemId}").SendAsync("BidPlaced", bidData);
        }

        public static async Task NotifyAuctionEnding(this IHubContext<AuctionHub> hubContext, int itemId, object data)
        {
            await hubContext.Clients.Group($"auction_{itemId}").SendAsync("AuctionEnding", data);
        }

        public static async Task NotifyAuctionEnded(this IHubContext<AuctionHub> hubContext, int itemId, object data)
        {
            await hubContext.Clients.Group($"auction_{itemId}").SendAsync("AuctionEnded", data);
        }

        public static async Task NotifyAuctionStatusUpdate(this IHubContext<AuctionHub> hubContext, int itemId, object status)
        {
            await hubContext.Clients.Group($"auction_{itemId}").SendAsync("AuctionStatus", status);
        }
    }
}
