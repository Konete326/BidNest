using Microsoft.EntityFrameworkCore;
using BidNest.Models;

namespace BidNest.Services
{
    public interface IItemStatusService
    {
        Task ProcessExpiredItemsAsync();
        Task ProcessEndedAuctionsAsync();
        Task AutoApproveQualifiedItemsAsync();
        Task SendStatusNotificationsAsync();
    }

    public class ItemStatusService : IItemStatusService
    {
        private readonly BidnestContext _context;
        private readonly ILogger<ItemStatusService> _logger;

        public ItemStatusService(BidnestContext context, ILogger<ItemStatusService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessExpiredItemsAsync()
        {
            try
            {
                var expiredItems = await _context.Items
                    .Where(i => i.Status == "A" && i.EndDate <= DateTime.UtcNow)
                    .ToListAsync();

                foreach (var item in expiredItems)
                {
                    // Check if item has bids
                    var hasBids = await _context.Bids.AnyAsync(b => b.ItemId == item.ItemId);
                    
                    if (hasBids)
                    {
                        // Item sold - find winning bid
                        var winningBid = await _context.Bids
                            .Where(b => b.ItemId == item.ItemId)
                            .OrderByDescending(b => b.Amount)
                            .ThenBy(b => b.BidTime) // Earlier bid wins in case of tie
                            .FirstOrDefaultAsync();

                        if (winningBid != null)
                        {
                            item.Status = "S"; // Sold
                            item.CurrentBidId = winningBid.BidId;
                            item.CurrentPrice = winningBid.Amount;
                            
                            _logger.LogInformation("Item {ItemId} marked as sold to bidder {BidderId} for ${Amount}", 
                                item.ItemId, winningBid.BidderId, winningBid.Amount);
                        }
                    }
                    else
                    {
                        // No bids - mark as expired
                        item.Status = "E"; // Expired
                        _logger.LogInformation("Item {ItemId} expired with no bids", item.ItemId);
                    }
                }

                if (expiredItems.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Processed {Count} expired items", expiredItems.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired items");
            }
        }

        public async Task ProcessEndedAuctionsAsync()
        {
            try
            {
                // Find auctions that ended in the last hour
                var recentlyEnded = await _context.Items
                    .Where(i => i.Status == "S" && 
                               i.EndDate <= DateTime.UtcNow && 
                               i.EndDate >= DateTime.UtcNow.AddHours(-1))
                    .Include(i => i.Seller)
                    .Include(i => i.Bids)
                    .ThenInclude(b => b.Bidder)
                    .ToListAsync();

                foreach (var item in recentlyEnded)
                {
                    // TODO: Send notifications to seller and winning bidder
                    // TODO: Create transaction record
                    // TODO: Update user statistics
                    
                    _logger.LogInformation("Auction {ItemId} ended - processing completion", item.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ended auctions");
            }
        }

        public async Task AutoApproveQualifiedItemsAsync()
        {
            try
            {
                // Auto-approve items from trusted sellers or that meet certain criteria
                var autoApprovalCandidates = await _context.Items
                    .Where(i => i.Status == "P" && 
                               i.CreatedAt <= DateTime.UtcNow.AddHours(-1)) // At least 1 hour old
                    .Include(i => i.Seller)
                    .Include(i => i.ItemImages)
                    .ToListAsync();

                foreach (var item in autoApprovalCandidates)
                {
                    bool shouldAutoApprove = false;

                    // Check if seller is trusted (has successful sales)
                    var sellerStats = await GetSellerStatsAsync(item.SellerId);
                    if (sellerStats.SuccessfulSales >= 5 && sellerStats.SuccessRate >= 0.95)
                    {
                        shouldAutoApprove = true;
                    }

                    // Check item quality criteria
                    if (HasGoodQuality(item))
                    {
                        shouldAutoApprove = true;
                    }

                    if (shouldAutoApprove)
                    {
                        item.Status = "A"; // Approved
                        item.StartDate = DateTime.UtcNow;
                        
                        _logger.LogInformation("Auto-approved item {ItemId} from trusted seller {SellerId}", 
                            item.ItemId, item.SellerId);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-approval process");
            }
        }

        public async Task SendStatusNotificationsAsync()
        {
            try
            {
                // TODO: Implement notification system
                // - Send email to sellers when items are approved/rejected
                // - Send notifications to bidders when auctions end
                // - Send reminders for ending auctions
                
                await Task.CompletedTask;
                _logger.LogInformation("Status notifications processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending status notifications");
            }
        }

        private async Task<(int SuccessfulSales, double SuccessRate)> GetSellerStatsAsync(int sellerId)
        {
            var totalItems = await _context.Items.CountAsync(i => i.SellerId == sellerId && i.Status != "P");
            var successfulSales = await _context.Items.CountAsync(i => i.SellerId == sellerId && i.Status == "S");
            
            var successRate = totalItems > 0 ? (double)successfulSales / totalItems : 0;
            
            return (successfulSales, successRate);
        }

        private bool HasGoodQuality(Item item)
        {
            // Basic quality checks
            if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Length < 10)
                return false;
                
            if (string.IsNullOrWhiteSpace(item.Description) || item.Description.Length < 50)
                return false;
                
            if (!item.ItemImages.Any())
                return false;
                
            if (item.MinBid <= 0 || item.MinBid > 10000) // Reasonable price range
                return false;
                
            return true;
        }
    }
}
