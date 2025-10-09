using BidNest.Models;
using BidNest.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BidnestContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IItemStatusService, ItemStatusService>();
builder.Services.AddSignalR();
builder.Services.AddAuctionTimerService();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ✅ Database seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BidnestContext>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await DatabaseSeeder.SeedAsync(context, authService);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ✅ Serve static files (with 3D file support)
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".glb"] = "model/gltf-binary";
provider.Mappings[".gltf"] = "model/gltf+json";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Serve React landing page at "/"
// Redirect logged-in users to /home instead
app.MapGet("/", async context =>
{
    // If the user is authenticated, redirect them to /home
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/home");
        return;
    }

    // Otherwise, serve the public React landing page
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(
        Path.Combine(app.Environment.WebRootPath, "index.html")
    );
});

// ✅ MVC routes start from /home now
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ✅ SignalR hub
app.MapHub<BidNest.Services.AuctionHub>("/auctionHub");

app.Run();
