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
        options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTION_STRING")));

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

    // ORDHUV endpoints
    app.MapGet("/ordhuv", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        int? skip,
        int? take,
        IOrdhuvService ordhuvService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var results = await ordhuvService.GetOrdhuvDataAsync(
                skip ?? 0, 
                take ?? 100);
                
            sw.Stop();
            logger.LogInformation("ORDHUV request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "ORDHUV request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdhuv")
    .WithOpenApi();

    app.MapGet("/ordhuv/{orderNr:int}", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        int orderNr,
        IOrdhuvService ordhuvService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var result = await ordhuvService.GetOrdhuvByIdAsync(orderNr);
                
            sw.Stop();
            logger.LogInformation("ORDHUV by ID request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            
            if (result == null)
                return Results.NotFound($"Order with number {orderNr} not found");
                
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "ORDHUV by ID request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdhuvById")
    .WithOpenApi();

    // Invoiced orders by date endpoints
    app.MapGet("/ordhuv/invoiced", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        int? skip,
        int? take,
        IOrdhuvService ordhuvService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (fromDate > toDate)
            {
                return Results.BadRequest("fromDate must be less than or equal to toDate");
            }

            var results = await ordhuvService.GetInvoicedOrdersByDateAsync(
                fromDate, 
                toDate, 
                skip ?? 0, 
                take ?? 100);
                
            sw.Stop();
            logger.LogInformation("Invoiced ORDHUV request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Invoiced ORDHUV request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetInvoicedOrdhuv")
    .WithOpenApi();

    // Orders with invoices by date endpoints
    app.MapGet("/ordhuv/with-invoices", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        IOrdhuvService ordhuvService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (fromDate > toDate)
            {
                return Results.BadRequest("fromDate must be less than or equal to toDate");
            }

            var results = await ordhuvService.GetOrdersWithInvoicesByDateAsync(
                fromDate, 
                toDate);
                
            sw.Stop();
            logger.LogInformation("Orders with invoices request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Orders with invoices request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdersWithInvoices")
    .WithOpenApi();

    // Temporary endpoint to discover table structure
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
    .WithOpenApi();

    // Temporary endpoint to discover INVOICEINDIVIDUAL table structure
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
    .WithOpenApi();

    // Temporary endpoint to discover FORTNOX_LOG table structure
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
    .WithOpenApi();

    // Temporary endpoint to discover KUNREG table structure
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
    .WithOpenApi();

    // Temporary endpoint to discover BILREG table structure
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
    .WithOpenApi();

    // Database environment management endpoints
    app.MapGet("/database/environments", (
        HttpContext httpContext,
        ILogger<Program> logger,
        IDatabaseConfigService databaseConfig) =>
    {
        try
        {
            var environments = databaseConfig.GetAvailableEnvironments();
            var current = databaseConfig.GetCurrentEnvironment();
            return Results.Ok(new { current, available = environments });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get database environments");
            return Results.Problem($"Failed to get environments: {ex.Message}");
        }
    })
    .WithName("GetDatabaseEnvironments")
    .WithOpenApi();

    // Optimized endpoint for orders with invoices
    app.MapGet("/ordhuv/with-invoices-optimized", async (
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
    .WithName("GetOrdersWithInvoicesOptimized")
    .WithOpenApi();

    // ORDRAD endpoints
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
    .WithOpenApi();

    app.MapGet("/ordrad/keyword-categories", async (
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
    .WithName("GetKeywordCategories")
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
            
            return Results.Ok(new {
                summary = new {
                    orderCount = orders.Count(),
                    subscriberCount = ruleIoRequest.Subscribers.Count,
                    elapsedMs = sw.ElapsedMilliseconds
                },
                request = ruleIoRequest,
                response = result
            });
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
                new() { Key = "Infoflex.Belopp", Value = order.OrhSummainkl?.ToString() ?? "", Type = "text" },
                new() { Key = "Infoflex.Anlaggning", Value = GetFacilityInfo(order.Database ?? ""), Type = "multiple" },
                new() { Key = "Infoflex.Fordonstyp", Value = order.Vehicle?.BilVehiclecat ?? "", Type = "text" },
                new() { Key = "Infoflex.Marke", Value = order.Vehicle?.Fabrikat ?? "", Type = "text" },
                new() { Key = "Infoflex.Miltal", Value = "0", Type = "text" }, // Default value
                new() { Key = "Infoflex.Modell", Value = order.Vehicle?.BilBetekning ?? "", Type = "text" },
                new() { Key = "Infoflex.Modellar", Value = order.Vehicle?.BilArsm.ToString() ?? "", Type = "text" },
                new() { Key = "Infoflex.Regnr", Value = order.OrhRenr ?? "", Type = "text" },
                new() { Key = "Infoflex.Ordrad", Value = order.Categories?.ToArray() ?? new string[0], Type = "multiple" }
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
        "NIE2V" => new[] { "Niemi Boden", "info@niemiboden.se", "0920-23 00 88" },
        "NIEM3" => new[] { "Niemi Luleå", "info@niemilulea.se", "0920-23 00 89" },
        "NIEM4" => new[] { "Niemi Piteå", "info@niemipitea.se", "0920-23 00 90" },
        "NIEM5" => new[] { "Niemi Skellefteå", "info@niemiskelleftea.se", "0920-23 00 91" },
        "NIEM6" => new[] { "Niemi Umeå", "info@niemiumea.se", "0920-23 00 92" },
        "NIEM7" => new[] { "Niemi Örnsköldsvik", "info@niemiornskoldsvik.se", "0920-23 00 93" },
        "NIEMI" => new[] { "Niemi Stockholm", "info@niemistockholm.se", "0920-23 00 94" },
        _ => new[] { "Niemi Unknown", "noreply@niemibil.se", "0920-23 00 88" }
    };
}
