using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class OrdrRadService : IOrdrRadService
{
    private readonly string _connectionString;
    private readonly ILogger<OrdrRadService> _logger;

    // Static keyword categories data
    // NOTE: Short keywords (AC, MV, MOK) have leading spaces to prevent false positives
    private static readonly List<KeywordCategoryDto> KeywordCategories = new()
    {
        new KeywordCategoryDto
        {
            Category = "Reparation",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 0, Keyword = "REPARATION" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "Felsökning",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 1, Keyword = "DIAGNOS" },
                new() { Id = 2, Keyword = "FELKOD" },
                new() { Id = 3, Keyword = "AVLÄS" },
                new() { Id = 4, Keyword = "FELSÖK" },
                new() { Id = 5, Keyword = "MOTORLA" },
                new() { Id = 6, Keyword = "UNDERSÖK" },
                new() { Id = 7, Keyword = "USK" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "AC",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 8, Keyword = " AC" }, // Leading space required - AC is too common without it
                new() { Id = 9, Keyword = "KONDENSOR" },
                new() { Id = 10, Keyword = "KYLER" },
                new() { Id = 11, Keyword = "KOMPRESSOR" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "Service",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 12, Keyword = "SERVICE" },
                new() { Id = 13, Keyword = "MÅNAD" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "Tillbehör",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 14, Keyword = "DRAG" },
                new() { Id = 15, Keyword = "EXTRALJUS" },
                new() { Id = 16, Keyword = "LEDRAMP" },
                new() { Id = 17, Keyword = "LED-RAMP" },
                new() { Id = 18, Keyword = " MV" }, // Leading space required - MV is too common without it
                new() { Id = 19, Keyword = "KUPEV" },
                new() { Id = 20, Keyword = " MOK" }, // Leading space required - MOK is too common without it
                new() { Id = 21, Keyword = "MOTORVÄRMARE" },
                new() { Id = 22, Keyword = "KUPÉVÄRMARE" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "Bromsar",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 23, Keyword = "BROMS" },
                new() { Id = 24, Keyword = "KLOSSAR" },
                new() { Id = 25, Keyword = "SKIVOR" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "Däck",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 26, Keyword = "DÄCK" },
                new() { Id = 27, Keyword = "HJULINSTÄLLNING" },
                new() { Id = 28, Keyword = "HJULSMATNING" },
                new() { Id = 29, Keyword = "HJULSKIFT" },
                new() { Id = 30, Keyword = "TPMS" },
                new() { Id = 31, Keyword = "PUNK" },
                new() { Id = 32, Keyword = "BALANS" }
            }
        },
        new KeywordCategoryDto
        {
            Category = "CTC",
            Entries = new List<KeywordEntryDto>
            {
                new() { Id = 33, Keyword = "CTC" }
            }
        }
    };

    public OrdrRadService(IConfiguration configuration, ILogger<OrdrRadService> logger)
    {
        _connectionString = configuration.GetConnectionString("FirebirdConnection") 
            ?? throw new ArgumentNullException("FirebirdConnection string is missing");
        _logger = logger;
    }

    public async Task<string> GetOrdrRadStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            // Try to discover the correct table name for order rows
            // Common patterns: ORDRAD, ORDRD, ORDHUV_RAD, etc.
            var possibleTableNames = new[] { "ORDRAD", "ORDRD", "ORDHUV_RAD", "ORDHUV_RD", "ORD_RAD", "ORD_RD" };
            string? workingTableName = null;
            
            foreach (var tableName in possibleTableNames)
            {
                try
                {
                    using var testCommand = new FbCommand($"SELECT FIRST 1 * FROM {tableName}", connection);
                    using var testReader = await testCommand.ExecuteReaderAsync();
                    workingTableName = tableName;
                    _logger.LogInformation("Found working table: {TableName}", tableName);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Table {TableName} not found: {Error}", tableName, ex.Message);
                }
            }
            
            if (workingTableName == null)
            {
                _logger.LogWarning("No order row table found. Available tables might be different.");
                return "No order row table found. Available tables might be different.";
            }
            
            // Get table structure - only get column information, not data
            var sqlQuery = $"SELECT FIRST 1 * FROM {workingTableName}";
                
            using var command = new FbCommand(sqlQuery, connection);
            
            _logger.LogInformation("Executing {TableName} structure discovery query", workingTableName);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var structure = new System.Text.StringBuilder();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var fieldType = reader.GetFieldType(i);
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL";
                    structure.AppendLine($"{fieldName} ({fieldType.Name}) = {value}");
                }
                return structure.ToString();
            }
            
            return $"No data found in {workingTableName} table";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ORDRAD table structure");
            return $"Error: {ex.Message}";
        }
    }

    public Task<IEnumerable<KeywordCategoryDto>> GetKeywordCategoriesAsync()
    {
        return Task.FromResult<IEnumerable<KeywordCategoryDto>>(KeywordCategories);
    }
}
