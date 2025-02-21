using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using FirebirdSql.Data.FirebirdClient;
using Niemi.Models;
using Niemi.Services;

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

    builder.Services.AddScoped<ILaginkService, LaginkService>();
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
}
