using BidNest.Models;
using BidNest.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace BidNest.Services
{
    public class AuctionService : IAuctionService
    {
        private readonly BidnestContext _context;
        private readonly ILogger<AuctionService> _logger;
        private readonly IHubContext<AuctionHub>? _hubContext;

        public AuctionService(BidnestContext context, ILogger<AuctionService> logger, IHubContext<AuctionHub>? hubContext = null)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<(bool Success, string Message, BidViewModel? Bid)> PlaceBidAsync(int itemId, int bidderId, decimal bidAmount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                
                var item = await _context.Items
                    .Include(i => i.Bids)
                    .FirstOrDefaultAsync(i => i.ItemId == itemId);

                if (item == null)
                    return (false, "Item not found.", null);

                
                if (!IsAuctionActive(item.EndDate))
                    return (false, "This auction has ended.", null);

                if (item.Status != "A")
                    return (false, "This auction is not active.", null);

                
                if (item.SellerId == bidderId)
                    return (false, "You cannot bid on your own item.", null);

                var currentHighestBid = await GetCurrentHighestBidAmount(itemId);
                var nextMinimumBid = CalculateNextMinimumBid(currentHighestBid, item.MinBid, item.BidIncrement);

                
                if (bidAmount < nextMinimumBid)
                    return (false, $"Bid must be at least ${nextMinimumBid:N2}.", null);

                
                var userHighestBid = await _context.Bids
                    .Where(b => b.ItemId == itemId && b.BidderId == bidderId)
                    .OrderByDescending(b => b.Amount)
                    .FirstOrDefaultAsync();

                if (userHighestBid != null && userHighestBid.Amount >= bidAmount)
                    return (false, $"You already have a higher bid of ${userHighestBid.Amount:N2}.", null);

                
                var newBid = new Bid
                {
                    ItemId = itemId,
                    BidderId = bidderId,
                    Amount = bidAmount,
                    BidTime = DateTime.UtcNow,
                    IsWinning = true 
                };

                _context.Bids.Add(newBid);

                
                var previousBids = await _context.Bids
                    .Where(b => b.ItemId == itemId && b.IsWinning)
                    .ToListAsync();

                foreach (var bid in previousBids)
                {
                    bid.IsWinning = false;
                }

                
                item.CurrentPrice = bidAmount;
                
                // Save changes first to get the BidId
                await _context.SaveChangesAsync();
                
                // Now update the CurrentBidId with the actual BidId
                item.CurrentBidId = newBid.BidId;
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();

                
                var bidder = await _context.Users.FindAsync(bidderId);
                var bidViewModel = new BidViewModel
                {
                    BidId = newBid.BidId,
                    ItemId = newBid.ItemId,
                    BidderId = newBid.BidderId,
                    Amount = newBid.Amount,
                    BidTime = newBid.BidTime,
                    IsWinning = newBid.IsWinning,
                    BidderName = bidder?.FullName,
                    ItemName = item.Title
                };

                
                await NotifyBidPlacedAsync(itemId, bidViewModel);

                _logger.LogInformation("Bid placed: ${Amount} on item {ItemId} by user {BidderId}", 
                    bidAmount, itemId, bidderId);

                return (true, "Bid placed successfully!", bidViewModel);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing bid for item {ItemId} by user {BidderId}", itemId, bidderId);
                return (false, "An error occurred while placing your bid. Please try again.", null);
            }
        }

        public async Task<PlaceBidViewModel?> GetBidFormDataAsync(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.ItemImages)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);

            if (item == null) return null;

            var currentHighestBid = await GetCurrentHighestBidAmount(itemId);
            var primaryImage = item.ItemImages.FirstOrDefault(i => i.IsPrimary);

            return new PlaceBidViewModel
            {
                ItemId = itemId,
                ItemName = item.Title,
                CurrentHighestBid = currentHighestBid,
                MinimumBid = item.MinBid,
                BidIncrement = item.BidIncrement,
                AuctionEndTime = item.EndDate,
                ItemImageUrl = primaryImage?.Url
            };
        }

        public async Task<bool> ValidateBidAsync(int itemId, decimal bidAmount, int bidderId)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null || !IsAuctionActive(item.EndDate) || item.Status != "A")
                return false;

            if (item.SellerId == bidderId)
                return false;

            var currentHighestBid = await GetCurrentHighestBidAmount(itemId);
            var nextMinimumBid = CalculateNextMinimumBid(currentHighestBid, item.MinBid, item.BidIncrement);

            return bidAmount >= nextMinimumBid;
        }

        public async Task<BidHistoryViewModel> GetBidHistoryAsync(int itemId, int page = 1, int pageSize = 20)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null)
                return new BidHistoryViewModel { ItemId = itemId };

            var totalBids = await _context.Bids.CountAsync(b => b.ItemId == itemId);
            
            var bids = await _context.Bids
                .Include(b => b.Bidder)
                .Where(b => b.ItemId == itemId)
                .OrderByDescending(b => b.BidTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BidViewModel
                {
                    BidId = b.BidId,
                    ItemId = b.ItemId,
                    BidderId = b.BidderId,
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    IsWinning = b.IsWinning,
                    BidderName = b.Bidder.FullName
                })
                .ToListAsync();

            var highestBid = bids.FirstOrDefault();
            
            return new BidHistoryViewModel
            {
                ItemId = itemId,
                ItemName = item.Title,
                Bids = bids,
                TotalBids = totalBids,
                HighestBid = highestBid?.Amount,
                CurrentWinnerName = highestBid?.BidderName,
                AuctionEndTime = item.EndDate,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<List<BidViewModel>> GetRecentBidsAsync(int itemId, int count = 10)
        {
            return await _context.Bids
                .Include(b => b.Bidder)
                .Where(b => b.ItemId == itemId)
                .OrderByDescending(b => b.BidTime)
                .Take(count)
                .Select(b => new BidViewModel
                {
                    BidId = b.BidId,
                    ItemId = b.ItemId,
                    BidderId = b.BidderId,
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    IsWinning = b.IsWinning,
                    BidderName = b.Bidder.FullName
                })
                .ToListAsync();
        }

        public async Task<BidViewModel?> GetHighestBidAsync(int itemId)
        {
            var bid = await _context.Bids
                .Include(b => b.Bidder)
                .Where(b => b.ItemId == itemId)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            if (bid == null) return null;

            return new BidViewModel
            {
                BidId = bid.BidId,
                ItemId = bid.ItemId,
                BidderId = bid.BidderId,
                Amount = bid.Amount,
                BidTime = bid.BidTime,
                IsWinning = bid.IsWinning,
                BidderName = bid.Bidder.FullName
            };
        }

        public async Task<List<BidViewModel>> GetUserBidsAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _context.Bids
                .Include(b => b.Item)
                .Include(b => b.Bidder)
                .Where(b => b.BidderId == userId)
                .OrderByDescending(b => b.BidTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BidViewModel
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
        }

        public async Task<MyBidsViewModel> GetMyBidsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var allBids = await GetUserBidsAsync(userId, page, pageSize);
            var totalBids = await _context.Bids.CountAsync(b => b.BidderId == userId);
            
            var winningBids = allBids.Where(b => b.IsWinning).ToList();
            var outbidBids = allBids.Where(b => !b.IsWinning).ToList();
            var totalBidAmount = await _context.Bids
                .Where(b => b.BidderId == userId)
                .SumAsync(b => b.Amount);

            return new MyBidsViewModel
            {
                MyBids = allBids,
                WinningBids = winningBids,
                OutbidBids = outbidBids,
                TotalBids = totalBids,
                WinningCount = winningBids.Count,
                TotalBidAmount = totalBidAmount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<AuctionStatusViewModel?> GetAuctionStatusAsync(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);

            if (item == null) return null;

            var highestBid = await GetHighestBidAsync(itemId);
            var primaryImage = item.ItemImages.FirstOrDefault(i => i.IsPrimary);

            return new AuctionStatusViewModel
            {
                ItemId = itemId,
                ItemName = item.Title,
                Status = item.Status,
                StartTime = item.StartDate,
                EndTime = item.EndDate,
                CurrentHighestBid = highestBid?.Amount,
                MinimumBid = item.MinBid,
                TotalBids = item.Bids.Count,
                CurrentWinnerName = highestBid?.BidderName,
                ItemImageUrl = primaryImage?.Url
            };
        }

        public async Task<List<AuctionStatusViewModel>> GetActiveAuctionsAsync(int count = 50)
        {
            var items = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderBy(i => i.EndDate)
                .Take(count)
                .ToListAsync();

            var result = new List<AuctionStatusViewModel>();
            foreach (var item in items)
            {
                var status = await GetAuctionStatusAsync(item.ItemId);
                if (status != null)
                    result.Add(status);
            }

            return result;
        }

        public async Task<List<AuctionStatusViewModel>> GetEndingAuctionsAsync(int hours = 24)
        {
            var cutoffTime = DateTime.UtcNow.AddHours(hours);
            
            var items = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .Where(i => i.Status == "A" && i.EndDate <= cutoffTime && i.EndDate > DateTime.UtcNow)
                .OrderBy(i => i.EndDate)
                .ToListAsync();

            var result = new List<AuctionStatusViewModel>();
            foreach (var item in items)
            {
                var status = await GetAuctionStatusAsync(item.ItemId);
                if (status != null)
                    result.Add(status);
            }

            return result;
        }

        public async Task UpdateAuctionStatusAsync(int itemId, string status)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item != null)
            {
                item.Status = status;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Auction status updated: Item {ItemId} set to {Status}", itemId, status);
            }
        }

        public async Task<(bool HasWinner, BidViewModel? WinningBid)> DetermineWinnerAsync(int itemId)
        {
            var winningBid = await GetHighestBidAsync(itemId);
            return (winningBid != null, winningBid);
        }

        public async Task ProcessAuctionEndAsync(int itemId)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null || item.Status != "A") return;

            var (hasWinner, winningBid) = await DetermineWinnerAsync(itemId);
            
            if (hasWinner && winningBid != null)
            {
                await UpdateAuctionStatusAsync(itemId, "S"); 
                await NotifyAuctionEndedAsync(itemId, winningBid);
                _logger.LogInformation("Auction ended with winner: Item {ItemId}, Winner: {BidderId}, Amount: ${Amount}", 
                    itemId, winningBid.BidderId, winningBid.Amount);
            }
            else
            {
                await UpdateAuctionStatusAsync(itemId, "E"); // Expired
                await NotifyAuctionEndedAsync(itemId, null);
                _logger.LogInformation("Auction ended without winner: Item {ItemId}", itemId);
            }
        }

        public async Task<List<AuctionSummaryViewModel>> GetCompletedAuctionsAsync(DateTime? fromDate = null, int count = 100)
        {
            var query = _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Bids)
                .ThenInclude(b => b.Bidder)
                .Include(i => i.Seller)
                .Where(i => i.Status == "S" || i.Status == "E");

            if (fromDate.HasValue)
                query = query.Where(i => i.EndDate >= fromDate.Value);

            var items = await query
                .OrderByDescending(i => i.EndDate)
                .Take(count)
                .ToListAsync();

            var result = new List<AuctionSummaryViewModel>();
            foreach (var item in items)
            {
                var winningBid = item.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                var primaryImage = item.ItemImages.FirstOrDefault(i => i.IsPrimary);

                result.Add(new AuctionSummaryViewModel
                {
                    ItemId = item.ItemId,
                    ItemName = item.Title,
                    ItemDescription = item.Description ?? string.Empty,
                    ItemImageUrl = primaryImage?.Url,
                    StartingBid = item.MinBid,
                    FinalBid = winningBid?.Amount,
                    TotalBids = item.Bids.Count,
                    AuctionStartTime = item.StartDate,
                    AuctionEndTime = item.EndDate,
                    WinnerName = winningBid?.Bidder?.FullName,
                    WinnerEmail = winningBid?.Bidder?.Email,
                    SellerName = item.Seller?.FullName
                });
            }

            return result;
        }

        
        public async Task NotifyBidPlacedAsync(int itemId, BidViewModel bid)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"auction_{itemId}")
                    .SendAsync("BidPlaced", bid);
            }
        }

        public async Task NotifyAuctionEndingAsync(int itemId, TimeSpan timeRemaining)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"auction_{itemId}")
                    .SendAsync("AuctionEnding", new { ItemId = itemId, TimeRemaining = timeRemaining });
            }
        }

        public async Task NotifyAuctionEndedAsync(int itemId, BidViewModel? winningBid)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.Group($"auction_{itemId}")
                    .SendAsync("AuctionEnded", new { ItemId = itemId, WinningBid = winningBid });
            }
        }

        
        public async Task<List<Item>> GetAuctionsEndingSoonAsync(int minutes = 30)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(minutes);
            return await _context.Items
                .Where(i => i.Status == "A" && i.EndDate <= cutoffTime && i.EndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task ProcessEndingAuctionsAsync()
        {
            var endedAuctions = await _context.Items
                .Where(i => i.Status == "A" && i.EndDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var auction in endedAuctions)
            {
                await ProcessAuctionEndAsync(auction.ItemId);
            }

            _logger.LogInformation("Processed {Count} ended auctions", endedAuctions.Count);
        }

        public async Task ExtendAuctionAsync(int itemId, TimeSpan extension)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item != null && item.Status == "A")
            {
                item.EndDate = item.EndDate.Add(extension);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Auction extended: Item {ItemId} by {Extension}", itemId, extension);
            }
        }

        
        public async Task<Dictionary<string, object>> GetAuctionStatisticsAsync()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalAuctions"] = await _context.Items.CountAsync(),
                ["ActiveAuctions"] = await _context.Items.CountAsync(i => i.Status == "A" && i.EndDate > DateTime.UtcNow),
                ["CompletedAuctions"] = await _context.Items.CountAsync(i => i.Status == "S" || i.Status == "E"),
                ["TotalBids"] = await _context.Bids.CountAsync(),
                ["TotalBidders"] = await _context.Bids.Select(b => b.BidderId).Distinct().CountAsync(),
                ["AverageBidsPerAuction"] = await _context.Items
                    .Where(i => i.Bids.Any())
                    .AverageAsync(i => i.Bids.Count),
                ["TotalBidValue"] = await _context.Bids.SumAsync(b => b.Amount)
            };

            return stats;
        }

        public async Task<List<BidViewModel>> GetTopBiddersAsync(int count = 10)
        {
            return await _context.Bids
                .Include(b => b.Bidder)
                .GroupBy(b => b.BidderId)
                .OrderByDescending(g => g.Sum(b => b.Amount))
                .Take(count)
                .SelectMany(g => g.OrderByDescending(b => b.Amount).Take(1))
                .Select(b => new BidViewModel
                {
                    BidId = b.BidId,
                    BidderId = b.BidderId,
                    Amount = b.Amount,
                    BidderName = b.Bidder.FullName
                })
                .ToListAsync();
        }

        public async Task<decimal> GetAverageBidAmountAsync(int itemId)
        {
            var bids = await _context.Bids.Where(b => b.ItemId == itemId).ToListAsync();
            return bids.Any() ? bids.Average(b => b.Amount) : 0;
        }

        
        public bool IsValidBidAmount(decimal currentHighest, decimal newBid, decimal increment)
        {
            var nextMinimum = currentHighest + increment;
            return newBid >= nextMinimum;
        }

        public bool IsAuctionActive(DateTime endTime)
        {
            return endTime > DateTime.UtcNow;
        }

        public decimal CalculateNextMinimumBid(decimal currentHighest, decimal minimumBid, decimal increment)
        {
            return currentHighest > 0 ? currentHighest + increment : minimumBid;
        }

        private async Task<decimal> GetCurrentHighestBidAmount(int itemId)
        {
            var highestBid = await _context.Bids
                .Where(b => b.ItemId == itemId)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            return highestBid?.Amount ?? 0;
        }

        
        public async Task<bool> SetAutoBidAsync(int itemId, int bidderId, decimal maxAmount)
        {
            
            await Task.CompletedTask;
            return false;
        }

        public async Task ProcessAutoBidsAsync(int itemId, decimal newBidAmount)
        {
            
            await Task.CompletedTask;
        }
    }
}
