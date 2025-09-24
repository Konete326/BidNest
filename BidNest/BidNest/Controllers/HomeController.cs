using BidNest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BidNest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BidnestContext _context;

        public HomeController(ILogger<HomeController> logger, BidnestContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Featured Items (ending soon)
            var featuredItems = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Include(i => i.Category)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderBy(i => i.EndDate)
                .Take(8)
                .ToListAsync();

            // Recent Auctions (newly listed)
            var recentAuctions = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Include(i => i.Category)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .Take(6)
                .ToListAsync();

            // Categories with item counts
            var categoriesWithCounts = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    c.CategoryId,
                    c.Name,
                    ItemCount = c.Items.Count(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                })
                .Where(c => c.ItemCount > 0)
                .OrderByDescending(c => c.ItemCount)
                .Take(8)
                .ToListAsync();

            ViewBag.RecentAuctions = recentAuctions;
            ViewBag.CategoriesWithCounts = categoriesWithCounts;

            return View(featuredItems);
        }

        public IActionResult Auctions()
        {
            // Redirect to the proper auction controller
            return RedirectToAction("Active", "Auction");
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
