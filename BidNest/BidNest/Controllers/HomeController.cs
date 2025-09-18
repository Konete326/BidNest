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
           
            var featuredItems = await _context.Items
                .Include(i => i.ItemImages)
                .Include(i => i.Seller)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderBy(i => i.EndDate)
                .Take(8)
                .ToListAsync();

            return View(featuredItems);
        }

        public async Task<IActionResult> Auctions()
        {
            var activeItems = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.ItemImages)
                .Where(i => i.Status == "A" && i.EndDate > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(activeItems);
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
