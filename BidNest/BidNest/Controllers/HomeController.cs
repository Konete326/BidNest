using BidNest.Models;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string subject, string message, bool newsletter = false)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return View();
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                // Get current user
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    TempData["ErrorMessage"] = "Unable to identify user. Please log in again.";
                    return View();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please log in again.";
                    return View();
                }

                // Save the contact message to database
                var contactMessage = new ContactMessage
                {
                    UserId = userId,
                    Name = user.FullName,
                    Email = user.Email,
                    Subject = subject,
                    Message = message,
                    NewsletterSubscription = newsletter,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact form submitted by {Email} with subject: {Subject} - Message ID: {MessageId}", 
                    user.Email, subject, contactMessage.MessageId);

                // TODO: In production, you would also:
                // 1. Send email notification to support team
                // 2. Send confirmation email to user

                TempData["SuccessMessage"] = "Thank you for your message! We'll get back to you within 24 hours.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving contact message from user {UserIdClaim}", userIdClaim);
                TempData["ErrorMessage"] = "An error occurred while sending your message. Please try again later.";
                return View();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
