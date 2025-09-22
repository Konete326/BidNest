using BidNest.Models;
using BidNest.ViewModels;

namespace BidNest.Services
{
    public interface IAuctionService
    {
        
        Task<(bool Success, string Message, BidViewModel? Bid)> PlaceBidAsync(int itemId, int bidderId, decimal bidAmount);
        Task<PlaceBidViewModel?> GetBidFormDataAsync(int itemId);
        Task<bool> ValidateBidAsync(int itemId, decimal bidAmount, int bidderId);
        
        
        Task<BidHistoryViewModel> GetBidHistoryAsync(int itemId, int page = 1, int pageSize = 20);
        Task<List<BidViewModel>> GetRecentBidsAsync(int itemId, int count = 10);
        Task<BidViewModel?> GetHighestBidAsync(int itemId);
        Task<List<BidViewModel>> GetUserBidsAsync(int userId, int page = 1, int pageSize = 20);
        Task<MyBidsViewModel> GetMyBidsAsync(int userId, int page = 1, int pageSize = 20);
        

        Task<AuctionStatusViewModel?> GetAuctionStatusAsync(int itemId);
        Task<List<AuctionStatusViewModel>> GetActiveAuctionsAsync(int count = 50);
        Task<List<AuctionStatusViewModel>> GetEndingAuctionsAsync(int hours = 24);
        Task UpdateAuctionStatusAsync(int itemId, string status);
        
        
        Task<(bool HasWinner, BidViewModel? WinningBid)> DetermineWinnerAsync(int itemId);
        Task ProcessAuctionEndAsync(int itemId);
        Task<List<AuctionSummaryViewModel>> GetCompletedAuctionsAsync(DateTime? fromDate = null, int count = 100);
        
        
        Task NotifyBidPlacedAsync(int itemId, BidViewModel bid);
        Task NotifyAuctionEndingAsync(int itemId, TimeSpan timeRemaining);
        Task NotifyAuctionEndedAsync(int itemId, BidViewModel? winningBid);
        
        
        Task<List<Item>> GetAuctionsEndingSoonAsync(int minutes = 30);
        Task ProcessEndingAuctionsAsync();
        Task ExtendAuctionAsync(int itemId, TimeSpan extension);
        
        
        Task<Dictionary<string, object>> GetAuctionStatisticsAsync();
        Task<List<BidViewModel>> GetTopBiddersAsync(int count = 10);
        Task<decimal> GetAverageBidAmountAsync(int itemId);
        
       
        bool IsValidBidAmount(decimal currentHighest, decimal newBid, decimal increment);
        bool IsAuctionActive(DateTime endTime);
        decimal CalculateNextMinimumBid(decimal currentHighest, decimal minimumBid, decimal increment);
        
        
        Task<bool> SetAutoBidAsync(int itemId, int bidderId, decimal maxAmount);
        Task ProcessAutoBidsAsync(int itemId, decimal newBidAmount);
    }
}
