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

            return View(viewModel);
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

            // For now, return empty watchlist - this would need a Watchlist table in the database
            var viewModel = new WatchlistViewModel
            {
                Items = new List<ItemViewModel>(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = 0,
                TotalPages = 0
            };

            return View(viewModel);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
