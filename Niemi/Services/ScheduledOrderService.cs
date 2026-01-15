using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class ScheduledOrderService : BackgroundService, IScheduledOrderService
{
    private readonly ILogger<ScheduledOrderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private bool _isEnabled;
    private int _scheduledHour;
    private TimeZoneInfo _timeZone;

    public ScheduledOrderService(
        ILogger<ScheduledOrderService> logger, 
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        
        // Read configuration
        _isEnabled = _configuration.GetValue<bool>("ScheduledOrderService:Enabled", false);
        _scheduledHour = _configuration.GetValue<int>("ScheduledOrderService:ScheduledHour", 8);
        var timeZoneId = _configuration.GetValue<string>("ScheduledOrderService:TimeZone", "Central European Standard Time");
        
        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            _logger.LogWarning("TimeZone '{TimeZone}' not found, using UTC", timeZoneId);
            _timeZone = TimeZoneInfo.Utc;
        }
        
        _logger.LogInformation("ScheduledOrderService configured: Enabled={Enabled}, ScheduledTime={Hour}:00 {TimeZone}", 
            _isEnabled, _scheduledHour, _timeZone.Id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("ScheduledOrderService is DISABLED in configuration. No scheduled processing will occur.");
            return;
        }

        _logger.LogInformation("ScheduledOrderService is ENABLED. Will run daily at {Hour}:00 {TimeZone}", 
            _scheduledHour, _timeZone.Id);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate time until next scheduled run
            var delay = CalculateDelayUntilNextRun();
            
            _logger.LogInformation("Next scheduled run in {Hours} hours {Minutes} minutes at {NextRun}", 
                (int)delay.TotalHours, delay.Minutes, 
                TimeZoneInfo.ConvertTime(DateTime.UtcNow.Add(delay), _timeZone).ToString("yyyy-MM-dd HH:mm:ss"));

            // Wait until the scheduled time
            await Task.Delay(delay, stoppingToken);

            // Run the processing
            try
            {
                await ProcessDailyOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing daily orders");
            }
        }
    }

    private TimeSpan CalculateDelayUntilNextRun()
    {
        var nowUtc = DateTime.UtcNow;
        var nowInTimeZone = TimeZoneInfo.ConvertTime(nowUtc, _timeZone);
        
        // Calculate next scheduled time
        var nextRun = nowInTimeZone.Date.AddHours(_scheduledHour);
        
        // If we've already passed today's scheduled time, schedule for tomorrow
        if (nowInTimeZone >= nextRun)
        {
            nextRun = nextRun.AddDays(1);
        }
        
        // Convert back to UTC for the delay calculation
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, _timeZone);
        var delay = nextRunUtc - nowUtc;
        
        // Ensure minimum delay of 1 minute
        if (delay.TotalMinutes < 1)
        {
            delay = TimeSpan.FromMinutes(1);
        }
        
        return delay;
    }

    public async Task ProcessDailyOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var ordhuvOptimizedService = scope.ServiceProvider.GetRequiredService<IOrdhuvOptimizedService>();
        var ruleIoService = scope.ServiceProvider.GetRequiredService<IRuleIoService>();

        // Get previous day's date range
        var yesterday = DateTime.Today.AddDays(-1);
        var startOfDay = yesterday.Date; // 00:00:00
        var endOfDay = yesterday.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999

        _logger.LogInformation("Processing orders for {Date} (from {StartTime} to {EndTime})", 
            yesterday.ToString("yyyy-MM-dd"), startOfDay, endOfDay);

        try
        {
            // Fetch orders from the previous day with KON status and Private customer type
            var orders = await ordhuvOptimizedService.GetOrdersWithInvoicesByDateAsync(
                startOfDay,
                endOfDay,
                null, // environment
                null, // environments
                "KON", // orhStat - only KON orders
                "Private" // customerType - only Private customers
            );

            _logger.LogInformation("Found {OrderCount} orders for {Date}", orders.Count(), yesterday.ToString("yyyy-MM-dd"));

            // Filter orders that have either email or phone number
            var filteredOrders = orders.Where(order => 
                !string.IsNullOrEmpty(order.Customer?.KunEpostadress) || 
                !string.IsNullOrEmpty(order.Customer?.MobilePhone));

            _logger.LogInformation("Filtered to {FilteredCount} orders with email or phone (from {TotalCount} total) for {Date}", 
                filteredOrders.Count(), orders.Count(), yesterday.ToString("yyyy-MM-dd"));

            if (filteredOrders.Any())
            {
                // Transform orders to Rule.io format
                var ruleIoRequest = TransformOrdersToRuleIoFormat(filteredOrders);

                // Send to Rule.io
                _logger.LogInformation("Sending {OrderCount} orders to Rule.io for {Date}", 
                    filteredOrders.Count(), yesterday.ToString("yyyy-MM-dd"));
                
                var result = await ruleIoService.CreateSubscribersAsync(ruleIoRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully sent {OrderCount} orders to Rule.io for {Date}", 
                        filteredOrders.Count(), yesterday.ToString("yyyy-MM-dd"));
                }
                else
                {
                    _logger.LogError("Failed to send orders to Rule.io for {Date}: {Message}", 
                        yesterday.ToString("yyyy-MM-dd"), result.Message);
                }
            }
            else
            {
                _logger.LogInformation("No orders with email/phone found for {Date}", yesterday.ToString("yyyy-MM-dd"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing orders for {Date}", yesterday.ToString("yyyy-MM-dd"));
        }
    }

    // Helper function to transform orders to Rule.io format (same as in Program.cs)
    private static RuleIoSubscribersRequestDto TransformOrdersToRuleIoFormat(IEnumerable<OrdhuvDto> orders)
    {
        var subscribers = new List<RuleIoSubscriberDto>();
        
        foreach (var order in orders)
        {
            // Create subscriber data from order
            var subscriber = new RuleIoSubscriberDto
            {
                Email = order.Customer?.KunEpostadress ?? "",
                PhoneNumber = order.Customer?.MobilePhone ?? "",
                Language = "sv",
                Fields = new List<RuleIoFieldDto>
                {
                    new() { Key = "Kundinfo.Personnr", Value = order.Customer?.KunOrgn ?? "", Type = "text" },
                    new() { Key = "Namn.Förnamn", Value = order.Customer?.FirstName ?? "", Type = "text" },
                    new() { Key = "Namn.Efternamn", Value = order.Customer?.LastName ?? "", Type = "text" },
                    new() { Key = "Adress.Stad", Value = order.Customer?.City ?? "", Type = "text" },
                    new() { Key = "Datum.Födelsedag", Value = order.Customer?.BirthDate?.ToString("yyyy-MM-dd") ?? "", Type = "date" },
                    new() { Key = "Infoflex.Datum", Value = order.OrhDokd?.ToString("yyyy-MM-dd") ?? "", Type = "date" },
                    new() { Key = "Infoflex.Doknr", Value = order.OrhDokn.ToString(), Type = "text" },
                    new() { Key = "Infoflex.Pris", Value = order.OrhSummainkl?.ToString() ?? "", Type = "text" },
                    new() { Key = "Infoflex.Anlaggning", Value = GetFacilityInfo(order.Database ?? "")[0], Type = "text" },
                    new() { Key = "Infoflex.AnlaggningEpost", Value = GetFacilityInfo(order.Database ?? "")[1], Type = "text" },
                    new() { Key = "Infoflex.AnlaggningTfn", Value = GetFacilityInfo(order.Database ?? "")[2], Type = "text" },
                    new() { Key = "Infoflex.Fordonstyp", Value = order.Vehicle?.BilVehiclecat ?? "", Type = "text" },
                    new() { Key = "Infoflex.Marke", Value = order.Vehicle?.Fabrikat ?? "", Type = "text" },
                    new() { Key = "Infoflex.Mätarställning", Value = (order.OrhMils.HasValue && order.OrhMils.Value != 0) ? order.OrhMils.Value.ToString() : "", Type = "text" },
                    new() { Key = "Infoflex.Modell", Value = order.Vehicle?.BilBetekning ?? "", Type = "text" },
                    new() { Key = "Infoflex.Modellar", Value = order.Vehicle?.BilArsm.ToString() ?? "", Type = "text" },
                    new() { Key = "Infoflex.Regnr", Value = order.OrhRenr ?? "", Type = "text" },
                    new() { Key = "Infoflex.Jobbtyp", Value = order.Categories?.ToArray() ?? new string[0], Type = "multiple" },
                    new() { Key = "Infoflex.Skapad", Value = order.OrhCreatedAt?.ToString("yyyy-MM-dd") ?? "", Type = "date" },
                    new() { Key = "Infoflex.Stad", Value = order.Customer?.City ?? "", Type = "text" }
                }
            };
            
            subscribers.Add(subscriber);
        }
        
        // Create single request with all subscribers
        return new RuleIoSubscribersRequestDto
        {
            UpdateOnDuplicate = true,
            Tags = new List<string> { "Infoflex" },
            Subscribers = subscribers
        };
    }

    // Helper function to get facility info (same as in Program.cs)
    private static string[] GetFacilityInfo(string database)
    {
        return database.ToUpper() switch
        {
            "NIE2V" => new[] { "Spantgatan", "verkstad.spantgatan@niemibil.se", "0920-23 00 88" },
            "NIEM3" => new[] { "Umeå", "verkstad.umea@niemibil.se", "090-428 80" },
            "NIEM4" => new[] { "Skellefteå", "verkstad.skelleftea@niemibil.se", "0910 - 573 90" },
            "NIEM6" => new[] { "Uppsala", "verkstad.uppsala@niemibil.se", "018 69 68 00" }, 
            // "NIEM5" => new[] { "Kiruna", "kiruna@niemibil.se", "0980 - 642 00" }, //Försäljning - DISABLED
            // "NIEM7" => new[] { "Gävle", "gavle@niemibil.se", "026-16 19 00" }, //Försäljning - DISABLED
            // "NIEMI" => new[] { "Banvägen", "intresse@niemibil.se", "0920-26 00 87" }, //Försäljning - DISABLED
            _ => new[] { "NIEMI BIL", "noreply@niemibil.se", "0920-23 00 88" }
        };
    }
}
