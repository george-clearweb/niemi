using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class ScheduledOrderService : BackgroundService, IScheduledOrderService
{
    private readonly ILogger<ScheduledOrderService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _period = TimeSpan.FromHours(24); // Run every 24 hours

    public ScheduledOrderService(ILogger<ScheduledOrderService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before starting to allow the application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDailyOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing daily orders");
            }

            // Wait for the next execution
            await Task.Delay(_period, stoppingToken);
        }
    }

    public async Task ProcessDailyOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var ordhuvOptimizedService = scope.ServiceProvider.GetRequiredService<IOrdhuvOptimizedService>();
        var httpBinService = scope.ServiceProvider.GetRequiredService<IHttpBinService>();

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

                // Send to HttpBin for testing
                var result = await httpBinService.SendToHttpBinAsync(ruleIoRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully processed {OrderCount} orders for {Date}", 
                        orders.Count(), yesterday.ToString("yyyy-MM-dd"));
                }
                else
                {
                    _logger.LogError("Failed to process orders for {Date}: {Message}", 
                        yesterday.ToString("yyyy-MM-dd"), result.Message);
                }
            }
            else
            {
                _logger.LogInformation("No orders found for {Date}", yesterday.ToString("yyyy-MM-dd"));
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
                    new() { Key = "Infoflex.Mätarställning", Value = "0", Type = "text" }, // Default value
                    new() { Key = "Infoflex.Modell", Value = order.Vehicle?.BilBetekning ?? "", Type = "text" },
                    new() { Key = "Infoflex.Modellar", Value = order.Vehicle?.BilArsm.ToString() ?? "", Type = "text" },
                    new() { Key = "Infoflex.Regnr", Value = order.OrhRenr ?? "", Type = "text" },
                    new() { Key = "Infoflex.Jobbtyp", Value = order.Categories?.ToArray() ?? new string[0], Type = "multiple" },
                    new() { Key = "Infoflex.Skapad", Value = order.OrhCreatedAt?.ToString("yyyy-MM-dd") ?? "", Type = "date" }
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
