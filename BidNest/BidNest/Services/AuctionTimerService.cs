using Microsoft.AspNetCore.SignalR;

namespace BidNest.Services
{
    public class AuctionTimerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuctionTimerService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public AuctionTimerService(IServiceProvider serviceProvider, ILogger<AuctionTimerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auction Timer Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAuctionTimers();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in auction timer service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
                }
            }

            _logger.LogInformation("Auction Timer Service stopped");
        }

        private async Task ProcessAuctionTimers()
        {
            using var scope = _serviceProvider.CreateScope();
            var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
            var hubContext = scope.ServiceProvider.GetService<IHubContext<AuctionHub>>();

            try
            {
                // Process ended auctions
                await auctionService.ProcessEndingAuctionsAsync();

                // Notify about auctions ending soon (within 30 minutes)
                var endingSoon = await auctionService.GetAuctionsEndingSoonAsync(30);
                foreach (var auction in endingSoon)
                {
                    var timeRemaining = auction.EndDate - DateTime.UtcNow;
                    
                    // Notify at specific intervals: 30min, 15min, 5min, 1min
                    if (ShouldNotifyAtInterval(timeRemaining))
                    {
                        await auctionService.NotifyAuctionEndingAsync(auction.ItemId, timeRemaining);
                        _logger.LogInformation("Notified auction ending soon: Item {ItemId}, Time remaining: {TimeRemaining}", 
                            auction.ItemId, timeRemaining);
                    }
                }

                // Update auction statuses for items that just ended
                var justEnded = await auctionService.GetAuctionsEndingSoonAsync(0); // Items that should have ended
                foreach (var auction in justEnded.Where(a => a.EndDate <= DateTime.UtcNow))
                {
                    await auctionService.ProcessAuctionEndAsync(auction.ItemId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing auction timers");
            }
        }

        private static bool ShouldNotifyAtInterval(TimeSpan timeRemaining)
        {
            var totalMinutes = timeRemaining.TotalMinutes;
            
            // Notify at these specific intervals (with 1-minute tolerance)
            var notificationIntervals = new[] { 30, 15, 5, 1 };
            
            return notificationIntervals.Any(interval => 
                Math.Abs(totalMinutes - interval) < 0.5); // Within 30 seconds of the interval
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auction Timer Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }

    // Extension methods for service registration
    public static class AuctionTimerServiceExtensions
    {
        public static IServiceCollection AddAuctionTimerService(this IServiceCollection services)
        {
            services.AddHostedService<AuctionTimerService>();
            return services;
        }
    }
}
