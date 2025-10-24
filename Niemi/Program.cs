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
    // Enable Swagger in all environments (safe on internal network)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Niemi API V1");
        c.RoutePrefix = "swagger"; // Accessible at /swagger
    });

    app.UseHttpsRedirection();
}

// Helper method to get orders by date (shared between GET /ordhuv and POST /ordhuv/phonenr)
async Task<IEnumerable<OrdhuvDto>> GetOrdersByDateAsync(
    DateTime fromDate,
    DateTime toDate,
    string? environment,
    string[]? environments,
    string? orhStat,
    string? customerType,
    bool? invoiced,
    IOrdhuvOptimizedService ordhuvOptimizedService,
    ILogger<Program> logger)
{
    if (fromDate > toDate)
    {
        throw new ArgumentException("fromDate must be less than or equal to toDate");
    }

    // Default to invoiced=true if not specified
    var invoicedFilter = invoiced ?? true;

    logger.LogInformation("Querying database: fromDate={FromDate}, toDate={ToDate}, environment={Environment}, orhStat={OrhStat}, customerType={CustomerType}, invoiced={Invoiced}", 
        fromDate, toDate, environment, orhStat, customerType, invoicedFilter);

    var results = await ordhuvOptimizedService.GetOrdersByDateAsync(
        fromDate, 
        toDate,
        environment,
        environments,
        orhStat,
        customerType,
        invoicedFilter);

    logger.LogInformation("Retrieved {OrderCount} orders from database", results.Count());
    return results;
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
                facilities = facilities,
                currentEnvironment = current,
                totalEnvironments = environments.Count()
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
        bool? invoiced,
        IOrdhuvOptimizedService ordhuvOptimizedService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var results = await GetOrdersByDateAsync(
                fromDate, 
                toDate,
                environment,
                environments,
                orhStat,
                customerType,
                invoiced,
                ordhuvOptimizedService,
                logger);
                
            sw.Stop();
            logger.LogInformation("Optimized orders request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Optimized orders request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdhuv")
    .WithOpenApi();

    // GET endpoint for orders by single license plate
    app.MapGet("/ordhuv/licenseplate", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] string licensePlate,
        string? environment,
        [FromQuery] string[]? environments,
        string? orhStat,
        string? customerType,
        bool? invoiced,
        DateTime? fromDate,
        IOrdhuvOptimizedService ordhuvOptimizedService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            if (string.IsNullOrEmpty(licensePlate))
            {
                return Results.BadRequest("License plate is required");
            }

            // Convert single license plate to LicensePlateItem objects for the service
            var licensePlateItems = new List<LicensePlateItem> { new LicensePlateItem { Licenseplate = licensePlate } };

            var results = await ordhuvOptimizedService.GetOrdersByLicensePlatesAsync(
                licensePlateItems,
                environment,
                environments,
                orhStat,
                customerType,
                invoiced,
                fromDate);
                
            sw.Stop();
            logger.LogInformation("Orders by license plate request completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Ok(results);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Orders by license plate request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("GetOrdhuvByLicensePlate")
    .WithOpenApi();

    // POST endpoint for orders by multiple license plates
    app.MapPost("/ordhuv/licenseplate", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        List<string> request,
        string? environment,
        [FromQuery] string[]? environments,
        string? orhStat,
        string? customerType,
        bool? invoiced,
        DateTime? fromDate,
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
                customerType,
                invoiced,
                fromDate);
                
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
    .WithName("PostOrdhuvByLicensePlate")
    .WithOpenApi(operation => 
    {
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema 
                        { 
                            Type = "string",
                            Example = new Microsoft.OpenApi.Any.OpenApiString("BWL32X")
                        }
                    },
                    Example = new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("BWL32X"),
                        new Microsoft.OpenApi.Any.OpenApiString("LBO058"),
                        new Microsoft.OpenApi.Any.OpenApiString("UBJ02T")
                    }
                }
            }
        };
        return operation;
    });

    // POST endpoint for orders by phone numbers (URL params as defaults, array items can override)
    app.MapPost("/ordhuv/phonenr", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        List<PhoneNumberItemDto> request,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        string? environment,
        [FromQuery] string[]? environments,
        string? orhStat,
        string? customerType,
        bool? invoiced,
        IOrdhuvOptimizedService ordhuvOptimizedService) =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Validate request
            if (request == null || !request.Any())
            {
                return Results.BadRequest("At least one phone number item is required");
            }

            // Validate that all phone numbers are not empty
            if (request.Any(item => string.IsNullOrEmpty(item.PhoneNumber)))
            {
                return Results.BadRequest("All phone numbers must have a valid value");
            }

            if (fromDate > toDate)
            {
                return Results.BadRequest("FromDate must be less than or equal to ToDate");
            }

            // Default to invoiced=true if not specified
            var defaultInvoicedFilter = invoiced ?? true;

            // Step 1: Reuse GET /ordhuv logic to query database according to URL params
            var allOrders = await GetOrdersByDateAsync(
                fromDate, 
                toDate,
                environment,
                environments,
                orhStat,
                customerType,
                defaultInvoicedFilter,
                ordhuvOptimizedService,
                logger);

            // Step 2: Clean phone numbers in the database results
            // (This is already handled in the FilterOrdersByPhoneNumbers method)

            // Step 3: Process array items in parallel for better performance
            logger.LogInformation("Starting parallel processing of {ItemCount} items against {OrderCount} orders", request.Count, allOrders.Count());
            
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount // Use all available CPU cores
            };

            var allFilteredOrdersLock = new object();
            var allFilteredOrders = new List<PhoneNumberOrderResultDto>();

            await Task.Run(() =>
            {
                Parallel.ForEach(request, parallelOptions, item =>
                {
                    try
                    {
                        // Use array overrides if provided, otherwise use URL parameters
                        var itemFromDate = item.OverrideFromDate ?? fromDate;
                        var itemToDate = item.OverrideToDate ?? toDate;
                        var itemDatabase = item.OverrideDatabase ?? environment;
                        var itemOrhStat = item.OverrideOrhStat ?? orhStat;
                        var itemCustomerType = item.OverrideCustomerType ?? customerType;
                        var itemInvoiced = item.OverrideInvoiced ?? defaultInvoicedFilter;

                        // Validate date range for this item
                        if (itemFromDate > itemToDate)
                        {
                            logger.LogWarning("Invalid date range for callId {CallId}: fromDate {FromDate} > toDate {ToDate}", 
                                item.CallId, itemFromDate, itemToDate);
                            return; // Skip this item
                        }

                        // Filter the already-retrieved orders by this item's specific criteria
                        var itemFilteredOrders = allOrders.Where(order => 
                        {
                            // Filter by database if override is specified
                            if (!string.IsNullOrEmpty(itemDatabase) && !string.IsNullOrEmpty(order.Database) && 
                                !order.Database.Equals(itemDatabase, StringComparison.OrdinalIgnoreCase))
                                return false;

                            // Filter by date range if override is specified
                            if (item.OverrideFromDate.HasValue && order.OrhDokd < item.OverrideFromDate.Value)
                                return false;
                            if (item.OverrideToDate.HasValue && order.OrhDokd > item.OverrideToDate.Value)
                                return false;

                            // Filter by orhStat if override is specified
                            if (!string.IsNullOrEmpty(itemOrhStat) && !string.IsNullOrEmpty(order.OrhStat) && 
                                !order.OrhStat.Equals(itemOrhStat, StringComparison.OrdinalIgnoreCase))
                                return false;

                            // Filter by customerType if override is specified (check customer type from related customer)
                            if (!string.IsNullOrEmpty(itemCustomerType) && order.Customer != null && 
                                !string.IsNullOrEmpty(order.Customer.CustomerType) && 
                                !order.Customer.CustomerType.Equals(itemCustomerType, StringComparison.OrdinalIgnoreCase))
                                return false;

                            // Filter by invoiced if override is specified
                            if (item.OverrideInvoiced.HasValue)
                            {
                                bool hasInvoice = order.Invoices != null && order.Invoices.Any();
                                if (item.OverrideInvoiced.Value && !hasInvoice)
                                    return false;
                                if (!item.OverrideInvoiced.Value && hasInvoice)
                                    return false;
                            }

                            return true;
                        }).ToList();

                        // Filter by phone number in memory
                        var phoneFilteredOrders = FilterOrdersByPhoneNumbers(itemFilteredOrders, new List<string> { item.PhoneNumber });
                        
                        // Create result objects with input tracking information
                        var phoneNumberResults = phoneFilteredOrders.Select(order => new PhoneNumberOrderResultDto
                        {
                            CallId = item.CallId,
                            InputPhoneNumber = item.PhoneNumber,
                            Order = order
                        }).ToList();
                        
                        // Thread-safe addition to results
                        lock (allFilteredOrdersLock)
                        {
                            allFilteredOrders.AddRange(phoneNumberResults);
                        }

                        // Logging removed for parallel processing performance
                    }
                    catch (Exception ex)
                    {
                        // Log error without parameters to avoid threading issues
                        logger.LogError(ex, "Error processing callId {CallId}", item.CallId);
                    }
                });
            });

            logger.LogInformation("Parallel processing completed. Found {TotalOrders} orders across all items", allFilteredOrders.Count);
                
            sw.Stop();
            logger.LogInformation("Phone numbers request completed in {ElapsedMs}ms. Processed {ItemCount} items, found {TotalOrders} orders", 
                sw.ElapsedMilliseconds, request.Count, allFilteredOrders.Count);
            return Results.Ok(allFilteredOrders);
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "Phone numbers request failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Query failed: {ex.Message}");
        }
    })
    .WithName("PostOrdhuvByPhoneNumbers")
    .WithOpenApi(operation => 
    {
        operation.Responses = new OpenApiResponses
        {
            ["200"] = new OpenApiResponse
            {
                Description = "Successfully retrieved orders matching phone numbers",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "array",
                            Items = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["callId"] = new OpenApiSchema 
                                    { 
                                        Type = "string", 
                                        Example = new Microsoft.OpenApi.Any.OpenApiString("call-001") 
                                    },
                                    ["inputPhoneNumber"] = new OpenApiSchema 
                                    { 
                                        Type = "string", 
                                        Example = new Microsoft.OpenApi.Any.OpenApiString("+46701234567") 
                                    },
                                    ["order"] = new OpenApiSchema 
                                    { 
                                        Type = "object",
                                        Description = "Order details matching the phone number"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
        
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                ["callId"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("call-001") 
                                },
                                ["phoneNumber"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("+46701234567") 
                                },
                                ["overrideDatabase"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("NIEM4") 
                                },
                                ["overrideFromDate"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Format = "date-time",
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("2024-01-01T00:00:00Z") 
                                },
                                ["overrideToDate"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Format = "date-time",
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("2024-12-31T23:59:59Z") 
                                },
                                ["overrideOrhStat"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("A") 
                                },
                                ["overrideCustomerType"] = new OpenApiSchema 
                                { 
                                    Type = "string", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("Private") 
                                },
                                ["overrideInvoiced"] = new OpenApiSchema 
                                { 
                                    Type = "boolean", 
                                    Example = new Microsoft.OpenApi.Any.OpenApiBoolean(true) 
                                }
                            },
                            Required = new HashSet<string> { "phoneNumber" }
                        }
                    },
                    Example = new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["callId"] = new Microsoft.OpenApi.Any.OpenApiString("call-001"),
                            ["phoneNumber"] = new Microsoft.OpenApi.Any.OpenApiString("+46701234567"),
                            ["overrideDatabase"] = new Microsoft.OpenApi.Any.OpenApiString("NIEM4"),
                            ["overrideFromDate"] = new Microsoft.OpenApi.Any.OpenApiString("2024-01-01T00:00:00Z")
                        }
                    }
                }
            }
        };
        return operation;
    });

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

    // Production endpoint to fetch orders and forward to Rule.io
    // Example: POST /rule-flow?fromDate=2024-01-01&toDate=2024-01-31&orhStat=KON&customerType=Private
    app.MapPost("/rule-flow", async (
        HttpContext httpContext,
        ILogger<Program> logger,
        [Required] DateTime fromDate,
        [Required] DateTime toDate,
        string? orhStat, // Example: "KON" (only KON status orders)
        string? customerType, // Example: "Private" (only Private customers)
        IOrdhuvOptimizedService ordhuvOptimizedService,
        IRuleIoService ruleIoService) =>
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
            
            // Step 2: Filter orders that have either email or phone number
            var filteredOrders = orders.Where(order => 
                !string.IsNullOrEmpty(order.Customer?.KunEpostadress) || 
                !string.IsNullOrEmpty(order.Customer?.MobilePhone));
                
            logger.LogInformation("Filtered to {FilteredCount} orders with email or phone (from {TotalCount} total)", 
                filteredOrders.Count(), orders.Count());
            
            if (!filteredOrders.Any())
            {
                return Results.Ok(new { message = "No orders found with email or phone number", processedCount = 0 });
            }

            // Step 3: Transform orders to Rule.io format (single request with all subscribers)
            var ruleIoRequest = TransformOrdersToRuleIoFormat(filteredOrders);
            
            // Step 4: Send the single request to Rule.io
            var result = await ruleIoService.CreateSubscribersAsync(ruleIoRequest);
                
            sw.Stop();
            logger.LogInformation("Rule.io flow completed in {ElapsedMs}ms. Processed {OrderCount} orders in single request", 
                sw.ElapsedMilliseconds, orders.Count());
            
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
            logger.LogError(ex, "Rule.io flow failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return Results.Problem($"Rule.io flow failed: {ex.Message}");
        }
    })
    .WithName("RuleFlow")
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
                new() { Key = "Infoflex.Anlaggning", Value = GetFacilityInfo(order.Database ?? "").Name, Type = "text" },
                new() { Key = "Infoflex.AnlaggningEpost", Value = GetFacilityInfo(order.Database ?? "").Email, Type = "text" },
                new() { Key = "Infoflex.AnlaggningTfn", Value = GetFacilityInfo(order.Database ?? "").Phone, Type = "text" },
                new() { Key = "Infoflex.Fordonstyp", Value = order.Vehicle?.BilVehiclecat ?? "", Type = "text" },
                new() { Key = "Infoflex.Marke", Value = order.Vehicle?.Fabrikat ?? "", Type = "text" },
                new() { Key = "Infoflex.Mätarställning", Value = (order.OrhMils.HasValue && order.OrhMils.Value != 0) ? order.OrhMils.Value.ToString() : "", Type = "text" },
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

// Helper function to get facility information as a structured object
static FacilityDto GetFacilityInfo(string database)
{
    return database.ToUpper() switch
    {
        "NIE2V" => new FacilityDto { Name = "Spantgatan", Email = "verkstad.spantgatan@niemibil.se", Phone = "0920-830 60" }, //Verkstad
        "NIEM3" => new FacilityDto { Name = "Umeå", Email = "verkstad.umea@niemibil.se", Phone = "090-428 88" }, //Verkstad - Försäljning
        "NIEM4" => new FacilityDto { Name = "Skellefteå", Email = "verkstad.skelleftea@niemibil.se", Phone = "0910-548 50" }, //Verkstad - Försäljning
        "NIEM5" => new FacilityDto { Name = "Kiruna", Email = "kiruna@niemibil.se", Phone = "0980-642 00" }, //Verkstad - Försäljning
        "NIEM6" => new FacilityDto { Name = "Uppsala", Email = "verkstad.uppsala@niemibil.se", Phone = "018-69 68 68" }, //Verkstad - Försäljning
        
        // "NIEM7" => new FacilityDto { Name = "Gävle", Email = "gavle@niemibil.se", Phone = "026-16 19 00" }, //Försäljning - DISABLED
        // "NIEMI" => new FacilityDto { Name = "Banvägen", Email = "intresse@niemibil.se", Phone = "0920-26 00 87" }, //Försäljning - DISABLED
        _ => new FacilityDto { Name = "NIEMI BIL", Email = "noreply@niemibil.se", Phone = "0920-23 00 88" }
    };
}

// Helper function to filter orders by phone numbers
static IEnumerable<OrdhuvDto> FilterOrdersByPhoneNumbers(IEnumerable<OrdhuvDto> orders, List<string> searchPhoneNumbers)
{
    // Clean and normalize the search phone numbers
    var normalizedSearchPhones = searchPhoneNumbers.Select(CleanPhoneNumber).Where(p => !string.IsNullOrEmpty(p)).ToList();
    
    if (!normalizedSearchPhones.Any())
    {
        return orders; // Return all orders if no valid phone numbers provided
    }

    return orders.Where(order => 
    {
        // Check customer phone numbers
        if (order.Customer != null)
        {
            var customerPhones = new List<string>();
            if (!string.IsNullOrEmpty(order.Customer.KunTel1)) customerPhones.Add(order.Customer.KunTel1);
            if (!string.IsNullOrEmpty(order.Customer.KunTel2)) customerPhones.Add(order.Customer.KunTel2);
            if (!string.IsNullOrEmpty(order.Customer.KunTel3)) customerPhones.Add(order.Customer.KunTel3);
            if (!string.IsNullOrEmpty(order.Customer.MobilePhone)) customerPhones.Add(order.Customer.MobilePhone);

            var normalizedCustomerPhones = customerPhones.Select(CleanPhoneNumber).Where(p => !string.IsNullOrEmpty(p));
            if (normalizedCustomerPhones.Any(phone => normalizedSearchPhones.Contains(phone)))
                return true;
        }

        // Check payer phone numbers
        if (order.Payer != null)
        {
            var payerPhones = new List<string>();
            if (!string.IsNullOrEmpty(order.Payer.KunTel1)) payerPhones.Add(order.Payer.KunTel1);
            if (!string.IsNullOrEmpty(order.Payer.KunTel2)) payerPhones.Add(order.Payer.KunTel2);
            if (!string.IsNullOrEmpty(order.Payer.KunTel3)) payerPhones.Add(order.Payer.KunTel3);
            if (!string.IsNullOrEmpty(order.Payer.MobilePhone)) payerPhones.Add(order.Payer.MobilePhone);

            var normalizedPayerPhones = payerPhones.Select(CleanPhoneNumber).Where(p => !string.IsNullOrEmpty(p));
            if (normalizedPayerPhones.Any(phone => normalizedSearchPhones.Contains(phone)))
                return true;
        }

        // Check driver phone numbers
        if (order.Driver != null)
        {
            var driverPhones = new List<string>();
            if (!string.IsNullOrEmpty(order.Driver.KunTel1)) driverPhones.Add(order.Driver.KunTel1);
            if (!string.IsNullOrEmpty(order.Driver.KunTel2)) driverPhones.Add(order.Driver.KunTel2);
            if (!string.IsNullOrEmpty(order.Driver.KunTel3)) driverPhones.Add(order.Driver.KunTel3);
            if (!string.IsNullOrEmpty(order.Driver.MobilePhone)) driverPhones.Add(order.Driver.MobilePhone);

            var normalizedDriverPhones = driverPhones.Select(CleanPhoneNumber).Where(p => !string.IsNullOrEmpty(p));
            if (normalizedDriverPhones.Any(phone => normalizedSearchPhones.Contains(phone)))
                return true;
        }

        return false;
    });
}

// Helper function to clean and normalize phone numbers
static string CleanPhoneNumber(string phoneNumber)
{
    if (string.IsNullOrEmpty(phoneNumber))
        return string.Empty;

    // Remove all non-digit characters
    var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
    
    // Handle Swedish phone numbers
    if (cleaned.StartsWith("46"))
    {
        // Remove country code
        cleaned = cleaned.Substring(2);
    }
    else if (cleaned.StartsWith("0"))
    {
        // Remove leading zero
        cleaned = cleaned.Substring(1);
    }

    return cleaned;
}
