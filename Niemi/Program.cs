using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using FirebirdSql.Data.FirebirdClient;
using Niemi.Models;
using Niemi.Models.DTOs;
using Niemi.Services;
using Microsoft.EntityFrameworkCore;
using Niemi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
ConfigureServices(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
ConfigureApp(app);

// Configure endpoints
ConfigureEndpoints(app);

app.Run();



// Configuration methods
void ConfigureServices(WebApplicationBuilder builder)
{
    // Add Entity Framework
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTION_STRING"))
               .LogTo(Console.WriteLine, LogLevel.Information)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors());

    // Configure JSON serialization to ignore null values
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Niemi API", 
            Version = "v1",
            Description = "API for Niemi data access"
        });
    });

    // Database configuration service
    builder.Services.AddSingleton<IDatabaseConfigService, DatabaseConfigService>();
    
    // Business services
    builder.Services.AddScoped<ILaginkService, LaginkService>();
    builder.Services.AddScoped<IOrdhuvService, OrdhuvService>();
    builder.Services.AddScoped<IOrdhuvOptimizedService, OrdhuvOptimizedService>();
    builder.Services.AddScoped<IOrdrRadService, OrdrRadService>();
    
    // Scheduled services
    builder.Services.AddHostedService<ScheduledOrderService>();
    builder.Services.AddScoped<IScheduledOrderService>(provider => 
        provider.GetRequiredService<ScheduledOrderService>());
    
    // HTTP client for Rule.io
    builder.Services.AddHttpClient<IRuleIoService, RuleIoService>();
    
    // HTTP client for HttpBin testing
    builder.Services.AddHttpClient<IHttpBinService, HttpBinService>();
}

void ConfigureApp(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Niemi API V1");
        });
    }

    app.UseHttpsRedirection();
}

void ConfigureEndpoints(WebApplication app)
{
    app.MapGet("/laginkhd", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        int? skip,
        int? take,
        ILaginkService laginkService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (fromDate > toDate)
            {
                return Results.BadRequest("fromDate must be less than or equal to toDate");
            }

            var results = await laginkService.GetLaginkDataAsync(
                fromDate, 
                toDate, 
                skip ?? 0, 
                take ?? 100);
                
            sw.Stop();
            logger.LogInformation("Request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetLaginkHd")
    .WithOpenApi();


    // Temporary endpoint to discover table structure (hidden from Swagger)
    app.MapGet("/ordhuv/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdhuvService ordhuvService) =>
    {
        try
        {
            var structure = await ordhuvService.GetOrdhuvTableStructureAsync();
            return Results.Ok(new { tableStructure = structure });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get table structure");
            return Results.Problem($"Failed to get table structure: {ex.Message}");
        }
    })
    .WithName("GetOrdhuvStructure")
    .ExcludeFromDescription();

    // Temporary endpoint to discover INVOICEINDIVIDUAL table structure (hidden from Swagger)
    app.MapGet("/invoice/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdhuvService ordhuvService) =>
    {
        try
        {
            var structure = await ordhuvService.GetInvoiceIndividualTableStructureAsync();
            return Results.Ok(new { tableStructure = structure });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get INVOICEINDIVIDUAL table structure");
            return Results.Problem($"Failed to get table structure: {ex.Message}");
        }
    })
    .WithName("GetInvoiceStructure")
    .ExcludeFromDescription();

    // Temporary endpoint to discover FORTNOX_LOG table structure (hidden from Swagger)
    app.MapGet("/fortnox/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdhuvService ordhuvService) =>
    {
        try
        {
            var structure = await ordhuvService.GetFortnoxLogTableStructureAsync();
            return Results.Ok(new { tableStructure = structure });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get FORTNOX_LOG table structure");
            return Results.Problem($"Failed to get table structure: {ex.Message}");
        }
    })
    .WithName("GetFortnoxStructure")
    .ExcludeFromDescription();

    // Temporary endpoint to discover KUNREG table structure (hidden from Swagger)
    app.MapGet("/kunreg/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdhuvService ordhuvService) =>
    {
        try
        {
            var structure = await ordhuvService.GetKunregTableStructureAsync();
            return Results.Ok(new { tableStructure = structure });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get KUNREG table structure");
            return Results.Problem($"Failed to get table structure: {ex.Message}");
        }
    })
    .WithName("GetKunregStructure")
    .ExcludeFromDescription();

    // Temporary endpoint to discover BILREG table structure (hidden from Swagger)
    app.MapGet("/bilreg/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdhuvService ordhuvService) =>
    {
        try
        {
            var structure = await ordhuvService.GetBilregTableStructureAsync();
            return Results.Ok(new { tableStructure = structure });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get BILREG table structure");
            return Results.Problem($"Failed to get table structure: {ex.Message}");
        }
    })
    .WithName("GetBilregStructure")
    .ExcludeFromDescription();

    // Database environment management endpoints
    app.MapGet("/database", (
        HttpContext httpContext,
        ILogger<Program> logger,
        IDatabaseConfigService databaseConfig) =>
    {
        try
        {
            var environments = databaseConfig.GetAvailableEnvironments();
            var current = databaseConfig.GetCurrentEnvironment();
            
            // Get facility information for each environment
            var facilities = environments.Select(env => new {
                database = env,
                facility = GetFacilityInfo(env)
            }).ToArray();
            
            return Results.Ok(new { 
                facilities = facilities
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get database environments");
            return Results.Problem($"Failed to get environments: {ex.Message}");
        }
    })
    .WithName("GetDatabase")
    .WithOpenApi();

    // Main orders endpoint
    app.MapGet("/ordhuv", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        string? environment,
        string[]? environments,
        string? orhStat,
        string? customerType,
        IOrdhuvOptimizedService ordhuvOptimizedService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (fromDate > toDate)
            {
                return Results.BadRequest("fromDate must be less than or equal to toDate");
            }

            var results = await ordhuvOptimizedService.GetOrdersWithInvoicesByDateAsync(
                fromDate, 
                toDate,
                environment,
                environments,
                orhStat,
                customerType);
                
            sw.Stop();
            logger.LogInformation("Optimized orders with invoices request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Optimized orders with invoices request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdhuv")
    .WithOpenApi();

    // POST endpoint for orders by license plates
    app.MapPost("/ordhuv", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        List<string> request,
        string? environment,
        [FromQuery] string[]? environments,
        string? orhStat,
        string? customerType,
        IOrdhuvOptimizedService ordhuvOptimizedService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (request == null || !request.Any())
            {
                return Results.BadRequest("At least one license plate is required");
            }

            // Validate that all license plates are not empty
            if (request.Any(lp => string.IsNullOrEmpty(lp)))
            {
                return Results.BadRequest("All license plates must have a valid value");
            }

            // Convert string array to LicensePlateItem objects for the service
            var licensePlateItems = request.Select(lp => new LicensePlateItem { Licenseplate = lp }).ToList();

            var results = await ordhuvOptimizedService.GetOrdersByLicensePlatesAsync(
                licensePlateItems,
                environment,
                environments,
                orhStat,
                customerType);
                
            sw.Stop();
            logger.LogInformation("Orders by license plates request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Orders by license plates request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("PostOrdhuv")
    .WithOpenApi();

    // ORDRAD endpoints (hidden from Swagger)
    app.MapGet("/ordrad/structure", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdrRadService ordrRadService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var results = await ordrRadService.GetOrdrRadStructureAsync();
                
            sw.Stop();
            logger.LogInformation("ORDRAD structure request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "ORDRAD structure request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdrRadStructure")
    .ExcludeFromDescription();

    app.MapGet("/ordrad/categories", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        IOrdrRadService ordrRadService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var results = await ordrRadService.GetKeywordCategoriesAsync();
                
            sw.Stop();
            logger.LogInformation("Keyword categories request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Keyword categories request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetCategories")
    .WithOpenApi();

    // Rule.io subscribers endpoint
    app.MapPost("/rule-io/subscribers", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        RuleIoSubscribersRequestDto request,
        IRuleIoService ruleIoService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (request.Subscribers == null || !request.Subscribers.Any())
            {
                return Results.BadRequest("At least one subscriber is required");
            }

            var result = await ruleIoService.CreateSubscribersAsync(request);
                
            sw.Stop();
            logger.LogInformation("Rule.io subscribers request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            
            if (result.Success)
            {
                return Results.Ok(result);
            }
            else
            {
                return Results.BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Rule.io subscribers request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Request failed: {ex.Message}");
        }
    })
    .WithName("CreateRuleIoSubscribers")
    .WithOpenApi();

    // HttpBin test endpoint
    app.MapPost("/httpbin/test", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        RuleIoSubscribersRequestDto request,
        IHttpBinService httpBinService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (request.Subscribers == null || !request.Subscribers.Any())
            {
                return Results.BadRequest("At least one subscriber is required");
            }

            var result = await httpBinService.SendToHttpBinAsync(request);
                
            sw.Stop();
            logger.LogInformation("HttpBin test request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            
            if (result.Success)
            {
                return Results.Ok(result);
            }
            else
            {
                return Results.BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "HttpBin test request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Request failed: {ex.Message}");
        }
    })
    .WithName("TestHttpBin")
    .WithOpenApi();

    // Endpoint to fetch orders and forward to HttpBin for testing
    app.MapPost("/test-flow", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        string? orhStat,
        string? customerType,
        IOrdhuvOptimizedService ordhuvOptimizedService,
        IHttpBinService httpBinService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (fromDate > toDate)
            {
                return Results.BadRequest("fromDate must be less than or equal to toDate");
            }

            // Step 1: Fetch orders from the optimized endpoint
            logger.LogInformation("Fetching orders from {FromDate} to {ToDate} with orhStat={OrhStat}, customerType={CustomerType}", 
                fromDate, toDate, orhStat, customerType);
            
            var orders = await ordhuvOptimizedService.GetOrdersWithInvoicesByDateAsync(
                fromDate, 
                toDate,
                null, // environment
                null, // environments
                orhStat,
                customerType);
                
            logger.LogInformation("Found {OrderCount} orders", orders.Count());
            
            // Step 2: Transform orders to Rule.io format (single request with all subscribers)
            var ruleIoRequest = TransformOrdersToRuleIoFormat(orders);
            
            // Step 3: Send the single request to HttpBin
            var result = await httpBinService.SendToHttpBinAsync(ruleIoRequest);
                
            sw.Stop();
            logger.LogInformation("Test flow completed in {ElapsedMs}ms. Processed {OrderCount} orders in single request", 
                sw.ElapsedMilliseconds, orders.Count());
            
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Test flow failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Test flow failed: {ex.Message}");
        }
    })
    .WithName("TestFlow")
    .WithOpenApi();

    // Manual trigger for scheduled order processing
    app.MapPost("/scheduled/process-daily-orders", async (
        ILogger<Program> logger,
        IScheduledOrderService scheduledOrderService) =>
    {
        try
        {
            logger.LogInformation("Manually triggering daily order processing");
            await scheduledOrderService.ProcessDailyOrdersAsync();
            return Results.Ok(new { message = "Daily order processing completed successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during manual daily order processing");
            return Results.Problem($"Manual processing failed: {ex.Message}");
        }
    })
    .WithName("ProcessDailyOrders")
    .WithOpenApi();

}

// Helper function to transform orders to Rule.io format
static RuleIoSubscribersRequestDto TransformOrdersToRuleIoFormat(IEnumerable<OrdhuvDto> orders)
{
    var subscribers = new List<RuleIoSubscriberDto>();
    
    foreach (var order in orders)
    {
        // Create subscriber data from order
        var subscriber = new RuleIoSubscriberDto
        {
            Email = order.Customer?.KunEpostadress ?? "",
            PhoneNumber = order.Customer?.MobilePhone,
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

// Helper function to get facility information as an array
static string[] GetFacilityInfo(string database)
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
