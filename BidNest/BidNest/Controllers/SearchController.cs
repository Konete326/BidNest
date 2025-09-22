using BidNest.Models;
using BidNest.Services;
using BidNest.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;
        private readonly BidnestContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, BidnestContext context, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _context = context;
            _logger = logger;
        }

        // GET: /Search
        public async Task<IActionResult> Index(SearchViewModel model)
        {
            try
            {
                // Populate dropdown data
                await PopulateSearchDropdowns(model);

                // Perform search
                var searchResult = await _searchService.SearchItemsAsync(model);

                return View(searchResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in search index");
                TempData["ErrorMessage"] = "An error occurred while searching. Please try again.";
                return View(new SearchViewModel());
            }
        }

        // GET: /Search/Advanced
        public async Task<IActionResult> Advanced()
        {
            var model = new SearchViewModel();
            await PopulateSearchDropdowns(model);
            return View(model);
        }

        // POST: /Search/Advanced
        [HttpPost]
        public IActionResult Advanced(SearchViewModel model)
        {
            return RedirectToAction(nameof(Index), model);
        }

        // GET: /Search/Category/{id}
        public async Task<IActionResult> Category(int id, int page = 1, string? sortBy = null, string? sortOrder = null)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new SearchViewModel
            {
                CategoryId = id,
                Page = page,
                SortBy = sortBy ?? "EndDate",
                SortOrder = sortOrder ?? "asc"
            };

            await PopulateSearchDropdowns(model);
            var searchResult = await _searchService.SearchItemsAsync(model);

            ViewBag.CategoryName = category.Name;
            return View("Index", searchResult);
        }

        // AJAX: /Search/Quick
        [HttpGet]
        public async Task<IActionResult> Quick(string q)
        {
            try
            {
                var results = await _searchService.QuickSearchAsync(q, 8);
                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search for query: {Query}", q);
                return Json(new QuickSearchViewModel { Query = q });
            }
        }

        // AJAX: /Search/Suggestions
        [HttpGet]
        public async Task<IActionResult> Suggestions(string q)
        {
            try
            {
                var suggestions = await _searchService.GetSearchSuggestionsAsync(q, 10);
                return Json(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suggestions for query: {Query}", q);
                return Json(new List<string>());
            }
        }

        // AJAX: /Search/Stats
        [HttpGet]
        public async Task<IActionResult> Stats(SearchViewModel model)
        {
            try
            {
                var stats = await _searchService.GetSearchStatsAsync(model);
                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search stats");
                return Json(new SearchStatsViewModel());
            }
        }

        // GET: /Search/Categories
        public async Task<IActionResult> Categories()
        {
            try
            {
                var categoryStats = await _searchService.GetCategoryStatsAsync();
                return View(categoryStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                return View(new List<CategoryStatsViewModel>());
            }
        }

        // Helper method to populate dropdown data
        private async Task PopulateSearchDropdowns(SearchViewModel model)
        {
            // Categories
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            model.Categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Categories" }
            };
            model.Categories.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name,
                Selected = c.CategoryId == model.CategoryId
            }));

            // Status options
            model.StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Status" },
                new SelectListItem { Value = "A", Text = "Active Auctions", Selected = model.Status == "A" },
                new SelectListItem { Value = "E", Text = "Ending Soon", Selected = model.Status == "E" },
                new SelectListItem { Value = "S", Text = "Sold Items", Selected = model.Status == "S" }
            };

            // Sort options
            model.SortOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "EndDate", Text = "Ending Soon", Selected = model.SortBy == "EndDate" },
                new SelectListItem { Value = "Price", Text = "Price", Selected = model.SortBy == "Price" },
                new SelectListItem { Value = "Bids", Text = "Most Bids", Selected = model.SortBy == "Bids" },
                new SelectListItem { Value = "Title", Text = "Title", Selected = model.SortBy == "Title" },
                new SelectListItem { Value = "StartDate", Text = "Start Date", Selected = model.SortBy == "StartDate" },
                new SelectListItem { Value = "Created", Text = "Newest First", Selected = model.SortBy == "Created" }
            };
        }
    }
}
