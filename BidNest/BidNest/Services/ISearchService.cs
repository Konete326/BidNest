using BidNest.ViewModels;

namespace BidNest.Services
{
    public interface ISearchService
    {
        Task<SearchViewModel> SearchItemsAsync(SearchViewModel searchModel);
        Task<QuickSearchViewModel> QuickSearchAsync(string query, int maxResults = 5);
        Task<SearchStatsViewModel> GetSearchStatsAsync(SearchViewModel searchModel);
        Task<List<string>> GetSearchSuggestionsAsync(string query, int maxResults = 10);
        Task<List<CategoryStatsViewModel>> GetCategoryStatsAsync();
    }
}
