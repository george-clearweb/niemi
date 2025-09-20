using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using FirebirdSql.Data.FirebirdClient;
using Niemi.Models;
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

}
