using BidNest.Models;
using BidNest.Services;
using BidNest.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BidNest.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly BidnestContext _context;

        public AccountController(IAuthService authService, BidnestContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User? user = null;
            try
            {
                user = await _authService.AuthenticateAsync(model.Email, model.Password);
                if (user == null)
                {
                 
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    if (existingUser == null)
                    {
                        ModelState.AddModelError(string.Empty, "No user found with this email address.");
                    }
                    else if (existingUser.IsBlocked)
                    {
                        ModelState.AddModelError(string.Empty, "Your account has been blocked.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid password.");
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Login error: {ex.Message}");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("FullName", user.FullName ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (user.Role.Name == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _authService.IsEmailExistsAsync(model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            if (await _authService.IsUsernameExistsAsync(model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            try
            {
                await _authService.RegisterAsync(model.Username, model.Email, model.Password, model.FullName);
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyBids(int page = 1, int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var totalBids = await _context.Bids
                    .Where(b => b.BidderId == userId)
                    .CountAsync();

                var bids = await _context.Bids
                    .Where(b => b.BidderId == userId)
                    .Include(b => b.Item)
                        .ThenInclude(i => i.Category)
                    .Include(b => b.Item)
                        .ThenInclude(i => i.ItemImages)
                    .OrderByDescending(b => b.BidTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new MyBidViewModel
                    {
                        BidId = b.BidId,
                        ItemId = b.ItemId,
                        ItemTitle = b.Item.Title,
                        ItemImageUrl = b.Item.ItemImages.FirstOrDefault(img => img.IsPrimary) != null 
                            ? b.Item.ItemImages.FirstOrDefault(img => img.IsPrimary)!.Url 
                            : b.Item.ItemImages.FirstOrDefault() != null 
                                ? b.Item.ItemImages.FirstOrDefault()!.Url 
                                : "",
                        CategoryName = b.Item.Category != null ? b.Item.Category.Name : "Uncategorized",
                        MyBidAmount = b.Amount,
                        CurrentHighestBid = b.Item.CurrentPrice ?? b.Item.MinBid,
                        MinimumBid = b.Item.MinBid,
                        BidTime = b.BidTime,
                        ItemStatus = b.Item.Status,
                        ItemEndDate = b.Item.EndDate,
                        IsWinning = b.IsWinning,
                        IsItemActive = b.Item.Status == "A" && b.Item.EndDate > DateTime.UtcNow
                    })
                    .ToListAsync();

                var viewModel = new UserBidsViewModel
                {
                    Bids = bids,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalBids = totalBids,
                    TotalPages = (int)Math.Ceiling((double)totalBids / pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to load your bids. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var viewModel = new UserProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                JoinDate = user.CreatedAt,
                Role = user.Role.Name,
                IsBlocked = user.IsBlocked
            };

            // Get stats
            ViewBag.TotalBids = await _context.Bids.CountAsync(b => b.BidderId == userId);
            ViewBag.WonAuctions = await _context.Items
                .Where(i => i.Status == "S" && i.CurrentBidId != null)
                .Join(_context.Bids, i => i.CurrentBidId, b => b.BidId, (i, b) => b)
                .CountAsync(b => b.BidderId == userId);
            ViewBag.ActiveBids = await _context.Bids
                .Where(b => b.BidderId == userId && b.Item.Status == "A")
                .CountAsync();
            ViewBag.WatchlistCount = await _context.Watchlists.CountAsync(w => w.UserId == userId);

            // Get recent bids
            ViewBag.RecentBids = await _context.Bids
                .Where(b => b.BidderId == userId)
                .Include(b => b.Item)
                .OrderByDescending(b => b.BidTime)
                .Take(5)
                .Select(b => new
                {
                    ItemId = b.ItemId,
                    ItemTitle = b.Item.Title,
                    Amount = b.Amount,
                    BidTime = b.BidTime,
                    IsWinning = b.IsWinning
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var viewModel = new EditProfileViewModel
            {
                FullName = user.FullName ?? string.Empty,
                Username = user.Username,
                Email = user.Email
            };

            ViewBag.Role = user.Role.Name;
            ViewBag.JoinDate = user.CreatedAt.ToString("MMMM dd, yyyy");

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                
                if (user != null)
                {
                    ViewBag.Role = user.Role.Name;
                    ViewBag.JoinDate = user.CreatedAt.ToString("MMMM dd, yyyy");
                }
                return View(model);
            }

            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null)
            {
                return RedirectToAction("Login");
            }

            // Check if email is already taken by another user
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == model.Email && u.UserId != userId);
            
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already in use by another account.");
                ViewBag.Role = userToUpdate.Role?.Name ?? "User";
                ViewBag.JoinDate = userToUpdate.CreatedAt.ToString("MMMM dd, yyyy");
                return View(model);
            }

            // Update user information
            userToUpdate.FullName = model.FullName;
            userToUpdate.Email = model.Email;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your profile has been updated successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating your profile. Please try again.";
                return RedirectToAction("Profile");
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return View(model);
            }

            // Check if new password is same as current
            if (BCrypt.Net.BCrypt.Verify(model.NewPassword, user.PasswordHash))
            {
                ModelState.AddModelError("NewPassword", "New password cannot be the same as your current password.");
                return View(model);
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your password has been changed successfully!";
                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while changing your password. Please try again.";
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Watchlist(int page = 1, int pageSize = 12)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            // Get total count of watchlist items
            var totalItems = await _context.Watchlists
                .Where(w => w.UserId == userId)
                .CountAsync();

            // Get watchlist items with pagination
            var watchlistItems = await _context.Watchlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Item)
                    .ThenInclude(i => i.Category)
                .Include(w => w.Item)
                    .ThenInclude(i => i.ItemImages)
                .OrderByDescending(w => w.AddedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new ItemViewModel
                {
                    ItemId = w.Item.ItemId,
                    Name = w.Item.Title,
                    Description = w.Item.Description,
                    CurrentBid = w.Item.CurrentPrice,
                    MinimumBid = w.Item.MinBid,
                    EndDate = w.Item.EndDate,
                    Status = w.Item.Status,
                    CategoryName = w.Item.Category != null ? w.Item.Category.Name : "Uncategorized",
                    CategoryId = w.Item.CategoryId ?? 0,
                    SellerId = w.Item.SellerId,
                    CreatedAt = w.Item.CreatedAt
                })
                .ToListAsync();

            var viewModel = new WatchlistViewModel
            {
                Items = watchlistItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyMessages()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login");
            }

            var messages = await _context.ContactMessages
                .Include(m => m.RepliedByUser)
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(messages);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
