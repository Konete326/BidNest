using BidNest.Models;
using BidNest.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Services
{
    public class SearchService : ISearchService
    {
        private readonly BidnestContext _context;
        private readonly ILogger<SearchService> _logger;

        public SearchService(BidnestContext context, ILogger<SearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SearchViewModel> SearchItemsAsync(SearchViewModel searchModel)
        {
            try
            {
                var query = _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.Seller)
                    .Include(i => i.ItemImages)
                    .Include(i => i.Bids)
                    .AsQueryable();

                // Apply filters
                query = ApplyFilters(query, searchModel);

                // Get total count for pagination
                var totalResults = await query.CountAsync();

                // Apply sorting
                query = ApplySorting(query, searchModel.SortBy, searchModel.SortOrder);

                // Apply pagination
                var results = await query
                    .Skip((searchModel.Page - 1) * searchModel.PageSize)
                    .Take(searchModel.PageSize)
                    .Select(i => new SearchResultItemViewModel
                    {
                        ItemId = i.ItemId,
                        Title = i.Title,
                        Description = i.Description,
                        CurrentPrice = i.CurrentPrice ?? i.MinBid,
                        MinBid = i.MinBid,
                        BidCount = i.Bids.Count,
                        StartDate = i.StartDate,
                        EndDate = i.EndDate,
                        Status = i.Status,
                        CategoryName = i.Category != null ? i.Category.Name : null,
                        SellerName = i.Seller != null ? i.Seller.FullName : "Unknown",
                        PrimaryImageUrl = i.ItemImages.FirstOrDefault(img => img.IsPrimary) != null 
                            ? i.ItemImages.FirstOrDefault(img => img.IsPrimary)!.Url 
                            : i.ItemImages.FirstOrDefault() != null 
                                ? i.ItemImages.FirstOrDefault()!.Url 
                                : null
                    })
                    .ToListAsync();

                searchModel.Results = results;
                searchModel.TotalResults = totalResults;
                searchModel.TotalPages = (int)Math.Ceiling((double)totalResults / searchModel.PageSize);

                // Get search statistics
                searchModel.Stats = await GetSearchStatsAsync(searchModel);

                return searchModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search with query: {Query}", searchModel.Query);
                return searchModel;
            }
        }

        public async Task<QuickSearchViewModel> QuickSearchAsync(string query, int maxResults = 5)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new QuickSearchViewModel { Query = query };
                }

                var searchQuery = _context.Items
                    .Include(i => i.Category)
                    .Include(i => i.ItemImages)
                    .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                    .Where(i => i.Title.Contains(query) || i.Description.Contains(query))
                    .OrderBy(i => i.EndDate);

                var totalResults = await searchQuery.CountAsync();

                var results = await searchQuery
                    .Take(maxResults)
                    .Select(i => new QuickSearchResultViewModel
                    {
                        ItemId = i.ItemId,
                        Title = i.Title,
                        CurrentPrice = i.CurrentPrice ?? i.MinBid,
                        CategoryName = i.Category != null ? i.Category.Name : null,
                        ImageUrl = i.ItemImages.FirstOrDefault(img => img.IsPrimary) != null 
                            ? i.ItemImages.FirstOrDefault(img => img.IsPrimary)!.Url 
                            : i.ItemImages.FirstOrDefault() != null 
                                ? i.ItemImages.FirstOrDefault()!.Url 
                                : null,
                        IsActive = i.Status == "A" && i.EndDate > DateTime.UtcNow,
                        TimeRemaining = i.EndDate > DateTime.UtcNow 
                            ? GetTimeRemainingDisplay(i.EndDate - DateTime.UtcNow)
                            : "Ended"
                    })
                    .ToListAsync();

                return new QuickSearchViewModel
                {
                    Query = query,
                    Results = results,
                    TotalResults = totalResults,
                    HasMore = totalResults > maxResults
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing quick search with query: {Query}", query);
                return new QuickSearchViewModel { Query = query };
            }
        }

        public async Task<SearchStatsViewModel> GetSearchStatsAsync(SearchViewModel searchModel)
        {
            try
            {
                var query = _context.Items.AsQueryable();
                query = ApplyFilters(query, searchModel);

                var stats = new SearchStatsViewModel
                {
                    TotalItems = await query.CountAsync(),
                    ActiveAuctions = await query.CountAsync(i => i.Status == "A" && i.EndDate > DateTime.UtcNow),
                    EndingSoon = await query.CountAsync(i => i.Status == "A" && i.EndDate > DateTime.UtcNow && i.EndDate <= DateTime.UtcNow.AddHours(24))
                };

                var bidsQuery = query.SelectMany(i => i.Bids);
                var bidAmounts = await bidsQuery.Select(b => b.Amount).ToListAsync();

                if (bidAmounts.Any())
                {
                    stats.TotalBids = bidAmounts.Count;
                    stats.AverageBid = bidAmounts.Average();
                    stats.HighestBid = bidAmounts.Max();
                    stats.LowestBid = bidAmounts.Min();
                }

                // Category statistics
                stats.CategoryStats = await query
                    .Where(i => i.CategoryId.HasValue && i.Category != null)
                    .GroupBy(i => new { i.CategoryId, CategoryName = i.Category!.Name })
                    .Select(g => new CategoryStatsViewModel
                    {
                        CategoryId = g.Key.CategoryId!.Value,
                        CategoryName = g.Key.CategoryName,
                        ItemCount = g.Count(),
                        AveragePrice = g.Average(i => i.CurrentPrice ?? i.MinBid)
                    })
                    .OrderByDescending(c => c.ItemCount)
                    .Take(10)
                    .ToListAsync();

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search statistics");
                return new SearchStatsViewModel();
            }
        }

        public async Task<List<string>> GetSearchSuggestionsAsync(string query, int maxResults = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return new List<string>();
                }

                var suggestions = await _context.Items
                    .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                    .Where(i => i.Title.Contains(query))
                    .Select(i => i.Title)
                    .Distinct()
                    .Take(maxResults)
                    .ToListAsync();

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
                return new List<string>();
            }
        }

        public async Task<List<CategoryStatsViewModel>> GetCategoryStatsAsync()
        {
            try
            {
                return await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new CategoryStatsViewModel
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.Name,
                        ItemCount = c.Items.Count(i => i.Status == "A" && i.EndDate > DateTime.UtcNow),
                        AveragePrice = c.Items
                            .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                            .Average(i => (decimal?)(i.CurrentPrice ?? i.MinBid)) ?? 0
                    })
                    .Where(c => c.ItemCount > 0)
                    .OrderByDescending(c => c.ItemCount)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category statistics");
                return new List<CategoryStatsViewModel>();
            }
        }

        private IQueryable<Item> ApplyFilters(IQueryable<Item> query, SearchViewModel searchModel)
        {
            // Keyword search
            if (!string.IsNullOrWhiteSpace(searchModel.Query))
            {
                var searchTerm = searchModel.Query.ToLower();
                query = query.Where(i => 
                    i.Title.ToLower().Contains(searchTerm) || 
                    i.Description.ToLower().Contains(searchTerm) ||
                    (i.Category != null && i.Category.Name.ToLower().Contains(searchTerm)));
            }

            // Category filter
            if (searchModel.CategoryId.HasValue)
            {
                query = query.Where(i => i.CategoryId == searchModel.CategoryId.Value);
            }

            // Price range filter
            if (searchModel.MinPrice.HasValue)
            {
                query = query.Where(i => (i.CurrentPrice ?? i.MinBid) >= searchModel.MinPrice.Value);
            }

            if (searchModel.MaxPrice.HasValue)
            {
                query = query.Where(i => (i.CurrentPrice ?? i.MinBid) <= searchModel.MaxPrice.Value);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(searchModel.Status))
            {
                if (searchModel.Status == "A") // Active
                {
                    query = query.Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow);
                }
                else if (searchModel.Status == "E") // Ending soon
                {
                    query = query.Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow && i.EndDate <= DateTime.UtcNow.AddHours(24));
                }
                else
                {
                    query = query.Where(i => i.Status == searchModel.Status);
                }
            }

            // Date range filters
            if (searchModel.StartDateFrom.HasValue)
            {
                query = query.Where(i => i.StartDate >= searchModel.StartDateFrom.Value);
            }

            if (searchModel.StartDateTo.HasValue)
            {
                query = query.Where(i => i.StartDate <= searchModel.StartDateTo.Value);
            }

            if (searchModel.EndDateFrom.HasValue)
            {
                query = query.Where(i => i.EndDate >= searchModel.EndDateFrom.Value);
            }

            if (searchModel.EndDateTo.HasValue)
            {
                query = query.Where(i => i.EndDate <= searchModel.EndDateTo.Value);
            }

            // Has bids filter
            if (searchModel.HasBids.HasValue)
            {
                if (searchModel.HasBids.Value)
                {
                    query = query.Where(i => i.Bids.Any());
                }
                else
                {
                    query = query.Where(i => !i.Bids.Any());
                }
            }

            return query;
        }

        private IQueryable<Item> ApplySorting(IQueryable<Item> query, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "title" => isDescending ? query.OrderByDescending(i => i.Title) : query.OrderBy(i => i.Title),
                "price" => isDescending ? query.OrderByDescending(i => i.CurrentPrice ?? i.MinBid) : query.OrderBy(i => i.CurrentPrice ?? i.MinBid),
                "bids" => isDescending ? query.OrderByDescending(i => i.Bids.Count) : query.OrderBy(i => i.Bids.Count),
                "startdate" => isDescending ? query.OrderByDescending(i => i.StartDate) : query.OrderBy(i => i.StartDate),
                "enddate" => isDescending ? query.OrderByDescending(i => i.EndDate) : query.OrderBy(i => i.EndDate),
                "created" => isDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
                _ => query.OrderBy(i => i.EndDate) // Default sort by end date ascending
            };
        }

        private string GetTimeRemainingDisplay(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
            if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            return $"{timeSpan.Minutes}m";
        }
    }
}
