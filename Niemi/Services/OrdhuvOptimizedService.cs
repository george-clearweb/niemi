using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;
using Niemi.Services;

namespace Niemi.Services;

    public class OrdhuvOptimizedService : IOrdhuvOptimizedService
    {
        private readonly ILogger<OrdhuvOptimizedService> _logger;
    private readonly IDatabaseConfigService _databaseConfig;

    public OrdhuvOptimizedService(ILogger<OrdhuvOptimizedService> logger, IDatabaseConfigService databaseConfig)
        {
            _logger = logger;
        _databaseConfig = databaseConfig;
    }

    // Static keyword categories data for matching
    // Priority order matches official specification (IDs 1-33)
    // NOTE: Short keywords (AC, MV, MOK) have leading spaces to prevent false positives
    // Default: "Reparation" is added at order level only if order has labor rows but no categories matched
    private static readonly List<KeywordCategoryDto> KeywordCategories = new()
    {
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
                new() { Id = 28, Keyword = "HJULSMÄTNING" },
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

        /// <summary>
    /// Matches keywords in the provided text and returns the first match.
    /// If no keyword matches, returns null (Reparation will be added at order level if needed).
        /// </summary>
    private static (string? keyword, string? category) MatchKeywords(string? text)
        {
        if (string.IsNullOrEmpty(text))
                return (null, null);

        var upperText = text.ToUpperInvariant();

        foreach (var category in KeywordCategories)
        {
            foreach (var entry in category.Entries)
            {
                if (upperText.Contains(entry.Keyword))
                {
                    return (entry.Keyword, category.Category);
                }
            }
        }

        // No match found
                return (null, null);
            }

    private static (string? customerType, DateTime? birthDate) ParseCustomerInfo(string? kunOrgn)
    {
        if (string.IsNullOrEmpty(kunOrgn))
            {
                return (null, null);
            }

        // Check if it matches the Swedish personal number format: yyMMdd-####
        // This pattern indicates a private person
        if (System.Text.RegularExpressions.Regex.IsMatch(kunOrgn, @"^\d{6}-\d{4}$"))
        {
            try
            {
                // Extract the date part (yyMMdd)
                var datePart = kunOrgn.Substring(0, 6);
                var year = int.Parse(datePart.Substring(0, 2));
                var month = int.Parse(datePart.Substring(2, 2));
                var day = int.Parse(datePart.Substring(4, 2));

                // Convert 2-digit year to 4-digit year using dynamic logic
                // Based on current year and reasonable age assumptions for car owners (max 99 years old)
                var currentYear = DateTime.Now.Year;
                var currentCentury = (currentYear / 100) * 100;
                var previousCentury = currentCentury - 100;
                
                // Calculate possible birth years
                var possibleYearCurrent = currentCentury + year;
                var possibleYearPrevious = previousCentury + year;
                
                // Determine which year makes more sense based on age constraints
                var ageCurrent = currentYear - possibleYearCurrent;
                var agePrevious = currentYear - possibleYearPrevious;
                
                // Choose the year that results in a reasonable age (0-99 years old)
                var fullYear = (ageCurrent >= 0 && ageCurrent <= 99) ? possibleYearCurrent : possibleYearPrevious;
                
                // Additional validation: ensure the chosen year results in a reasonable age
                var finalAge = currentYear - fullYear;
                if (finalAge < 0 || finalAge > 99)
                {
                    // If neither year results in a reasonable age, treat as company
                    return ("Company", null);
                }

                // Validate the date
                if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                {
                    var birthDate = new DateTime(fullYear, month, day);
                    return ("Private", birthDate);
                }
            }
            catch
            {
                // If parsing fails, treat as company
            }
        }

        // If it doesn't match the personal number format, treat as company
        return ("Company", null);
    }

    private static IEnumerable<OrdhuvDto> FilterByCustomerType(IEnumerable<OrdhuvDto> orders, string customerType)
    {
        return customerType?.ToLower() switch
        {
            "private" => orders.Where(order => 
                order.Customer?.CustomerType?.Equals("Private", StringComparison.OrdinalIgnoreCase) == true ||
                order.Payer?.CustomerType?.Equals("Private", StringComparison.OrdinalIgnoreCase) == true ||
                order.Driver?.CustomerType?.Equals("Private", StringComparison.OrdinalIgnoreCase) == true),
            "company" => orders.Where(order => 
                order.Customer?.CustomerType?.Equals("Company", StringComparison.OrdinalIgnoreCase) == true ||
                order.Payer?.CustomerType?.Equals("Company", StringComparison.OrdinalIgnoreCase) == true ||
                order.Driver?.CustomerType?.Equals("Company", StringComparison.OrdinalIgnoreCase) == true),
            _ => orders
        };
    }

    private static string? ProcessPhoneNumbers(string? tel1, string? tel2, string? tel3)
    {
        // Test priority order: tel2 first, then tel1, then tel3
        var phoneNumbers = new[] { tel2, tel1, tel3 }.Where(p => !string.IsNullOrEmpty(p)).ToArray();
            
        foreach (var phone in phoneNumbers)
        {
            // Clean the phone number
            var cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone ?? "", @"[^\d]", "");
            
            // Check if it's a Swedish mobile number (starts with 07)
            if (cleanPhone.StartsWith("07") && cleanPhone.Length >= 9)
            {
                // Format as +467xxxxxxxx
                return "+46" + cleanPhone.Substring(1);
            }
        }
        
        return null;
    }

    private static (string firstName, string lastName, string companyName) ParseName(string? kunNamn, string? customerType)
    {
        if (string.IsNullOrEmpty(kunNamn))
            return (string.Empty, string.Empty, string.Empty);

        // For companies, treat the entire name as company name (no capitalization)
        if (customerType == "Company")
        {
            return (string.Empty, string.Empty, kunNamn.Trim());
        }

        // For private persons, look for comma to separate last name and first name
        var commaIndex = kunNamn.IndexOf(',');
        if (commaIndex > 0)
        {
            var lastName = kunNamn.Substring(0, commaIndex).Trim();
            var firstName = kunNamn.Substring(commaIndex + 1).Trim();
            return (CapitalizeName(firstName), CapitalizeName(lastName), string.Empty);
        }

        // If no comma, treat the whole thing as last name
        return (string.Empty, CapitalizeName(kunNamn.Trim()), string.Empty);
    }

    private static string CapitalizeName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Split by spaces and capitalize each word
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var capitalizedWords = words.Select(word => 
        {
            if (string.IsNullOrEmpty(word))
                return word;
            
            // Capitalize first letter, lowercase the rest
            return char.ToUpper(word[0]) + word.Substring(1).ToLower();
        });

        return string.Join(" ", capitalizedWords);
    }

    private static (string? zipCode, string? city) ParsePostalAddress(string? kunPadr)
    {
        if (string.IsNullOrEmpty(kunPadr))
                return (null, null);

        // Extract zip code (numbers at the beginning)
        var zipMatch = System.Text.RegularExpressions.Regex.Match(kunPadr, @"^(\d{3}\s?\d{2})");
        var zipCode = zipMatch.Success ? zipMatch.Groups[1].Value.Trim() : null;

        // Extract city (everything after the zip code)
        var city = zipMatch.Success 
            ? kunPadr.Substring(zipMatch.Length).Trim()
            : kunPadr.Trim();

        return (zipCode, string.IsNullOrEmpty(city) ? null : city);
    }

    public async Task<IEnumerable<OrdhuvDto>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allResults = new List<OrdhuvDto>();

        try
        {
            // Determine which environments to query
            var targetEnvironments = GetTargetEnvironments(environment, environments);
            
            _logger.LogInformation("Querying {EnvironmentCount} environments: {Environments}", 
                targetEnvironments.Length, string.Join(", ", targetEnvironments));

            // Query all environments in parallel
            var tasks = targetEnvironments.Select(env => QueryEnvironmentAsync(env, fromDate, toDate, orhStat, customerType));
            var environmentResults = await Task.WhenAll(tasks);
            
            // Combine all results (no deduplication as requested)
            foreach (var envResults in environmentResults)
            {
                allResults.AddRange(envResults);
            }
            
            // Apply customerType filtering if specified
            if (!string.IsNullOrEmpty(customerType))
            {
                var originalCount = allResults.Count;
                allResults = FilterByCustomerType(allResults, customerType).ToList();
                _logger.LogInformation("Applied customerType filter '{CustomerType}': {OriginalCount} -> {FilteredCount} orders", 
                    customerType, originalCount, allResults.Count);
            }
            
            // Calculate count/fraction for each ORDRAD row per license plate across ALL environments
            // Sum of all rows for each license plate should equal 1.0
            var ordersByLicensePlate = allResults.GroupBy(order => order.OrhRenr).ToList();
            foreach (var licensePlateGroup in ordersByLicensePlate)
            {
                var licensePlate = licensePlateGroup.Key;
                var allOrderRowsForLicensePlate = licensePlateGroup.SelectMany(order => order.OrderRows).ToList();
                
                if (allOrderRowsForLicensePlate.Any())
                {
                    var fractionPerRow = 1.0 / allOrderRowsForLicensePlate.Count;
                    foreach (var row in allOrderRowsForLicensePlate)
                    {
                        row.Count = fractionPerRow;
                    }
                    
                    _logger.LogDebug("Calculated Count for license plate {LicensePlate} across all environments: {RowCount} rows, fraction per row: {Fraction}", 
                        licensePlate, allOrderRowsForLicensePlate.Count, fractionPerRow);
                }
            }
            
            sw.Stop();
            _logger.LogInformation("Query completed in {ElapsedMs}ms. Found {Count} orders from {EnvironmentCount} environments", 
                sw.ElapsedMilliseconds, allResults.Count, targetEnvironments.Length);

            return allResults;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
        }

        /// <summary>
    /// Determines which environments to query based on parameters
        /// </summary>
    private string[] GetTargetEnvironments(string? environment, string[]? environments)
    {
        // If specific environment is requested, use only that
        if (!string.IsNullOrEmpty(environment))
        {
            return new[] { environment };
        }
        
        // If specific environments are requested, use those
        if (environments != null && environments.Length > 0)
        {
            return environments;
        }
        
        // Default: query all available environments
        return _databaseConfig.GetAvailableEnvironments();
        }

        /// <summary>
    /// Queries a single environment for orders with invoices
        /// </summary>
    private async Task<List<OrdhuvDto>> QueryEnvironmentAsync(string environment, DateTime fromDate, DateTime toDate, string? orhStat = null, string? customerType = null)
    {
        var results = new List<OrdhuvDto>();

        try
        {
            using var connection = new FbConnection(_databaseConfig.GetConnectionString(environment));
            await connection.OpenAsync();
            
            // Step 1: Get orders that have invoices in the date range with customer data
            var sqlQuery = $@"
                SELECT DISTINCT
                    o.ORH_DOKN,
                    o.ORH_KUNR,
                    o.ORH_DOKD,
                    o.ORH_RENR,
                    o.ORH_STAT,
                    o.ORH_LOVDAT,
                    o.ORH_FAKTURERAD,
                    o.ORH_NAMN,
                    o.ORH_SUMMAINKL,
                    o.ORH_SUMMAEXKL,
                    o.ORH_MILS,
                    o.ORH_CREATED_AT,
                    o.ORH_UPDATED_AT,
                    CASE WHEN o.ORH_BETKUNR > 0 THEN o.ORH_BETKUNR ELSE NULL END as ORH_BETKUNR,
                    CASE WHEN o.ORH_DRIVER_NO > 0 THEN o.ORH_DRIVER_NO ELSE NULL END as ORH_DRIVER_NO,
                    -- Customer data (ORH_KUNR)
                    c.KUN_KUNR as CUST_KUN_KUNR,
                    c.KUN_NAMN as CUST_KUN_NAMN,
                    c.KUN_ADR1 as CUST_KUN_ADR1,
                    c.KUN_ADR2 as CUST_KUN_ADR2,
                    c.KUN_PADR as CUST_KUN_PADR,
                    c.KUN_ORGN as CUST_KUN_ORGN,
                    c.KUN_TEL1 as CUST_KUN_TEL1,
                    c.KUN_TEL2 as CUST_KUN_TEL2,
                    c.KUN_TEL3 as CUST_KUN_TEL3,
                    c.KUN_EPOSTADRESS as CUST_KUN_EPOSTADRESS,
                    -- Payer data (ORH_BETKUNR)
                    p.KUN_KUNR as PAYER_KUN_KUNR,
                    p.KUN_NAMN as PAYER_KUN_NAMN,
                    p.KUN_ADR1 as PAYER_KUN_ADR1,
                    p.KUN_ADR2 as PAYER_KUN_ADR2,
                    p.KUN_PADR as PAYER_KUN_PADR,
                    p.KUN_ORGN as PAYER_KUN_ORGN,
                    p.KUN_TEL1 as PAYER_KUN_TEL1,
                    p.KUN_TEL2 as PAYER_KUN_TEL2,
                    p.KUN_TEL3 as PAYER_KUN_TEL3,
                    p.KUN_EPOSTADRESS as PAYER_KUN_EPOSTADRESS,
                    -- Driver data (ORH_DRIVER_NO)
                    d.KUN_KUNR as DRIVER_KUN_KUNR,
                    d.KUN_NAMN as DRIVER_KUN_NAMN,
                    d.KUN_ADR1 as DRIVER_KUN_ADR1,
                    d.KUN_ADR2 as DRIVER_KUN_ADR2,
                    d.KUN_PADR as DRIVER_KUN_PADR,
                    d.KUN_ORGN as DRIVER_KUN_ORGN,
                    d.KUN_TEL1 as DRIVER_KUN_TEL1,
                    d.KUN_TEL2 as DRIVER_KUN_TEL2,
                    d.KUN_TEL3 as DRIVER_KUN_TEL3,
                    d.KUN_EPOSTADRESS as DRIVER_KUN_EPOSTADRESS,
                    -- Vehicle data (ORH_RENR -> BILREG.BIL_RENR)
                    b.BIL_RENR as VEHICLE_BIL_RENR,
                    b.BIL_BETECKNING as VEHICLE_BIL_BETECKNING,
                    b.BIL_ARSM as VEHICLE_BIL_ARSM,
                    b.FABRIKAT as VEHICLE_FABRIKAT,
                    b.BIL_VEHICLECAT as VEHICLE_BIL_VEHICLECAT,
                    b.BIL_FUEL as VEHICLE_BIL_FUEL
                FROM ORDHUV o
                INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO
                INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR
                LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR
                LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR
                LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR
                WHERE f.TIME_STAMP >= @fromDate 
                  AND f.TIME_STAMP <= @toDate
                  AND f.KEY_NO IS NOT NULL 
                  AND f.KEY_NO != ''
                  {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")}
                ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
                
            using var orderCommand = new FbCommand(sqlQuery, connection);
            orderCommand.Parameters.AddWithValue("@fromDate", fromDate);
            orderCommand.Parameters.AddWithValue("@toDate", toDate);
            if (!string.IsNullOrEmpty(orhStat))
            {
                orderCommand.Parameters.AddWithValue("@orhStat", orhStat);
            }

            // Log the SQL command (shortened version)
            var shortSql = $"SELECT DISTINCT o.ORH_DOKN, o.ORH_KUNR, o.ORH_DOKD, o.ORH_RENR, o.ORH_STAT, o.ORH_LOVDAT, o.ORH_FAKTURERAD, o.ORH_NAMN, o.ORH_SUMMAINKL, o.ORH_CREATED_AT, o.ORH_UPDATED_AT, CASE WHEN o.ORH_BETKUNR > 0 THEN o.ORH_BETKUNR ELSE NULL END as ORH_BETKUNR, CASE WHEN o.ORH_DRIVER_NO > 0 THEN o.ORH_DRIVER_NO ELSE NULL END as ORH_DRIVER_NO, [Customer/Payer/Driver/Vehicle data] FROM ORDHUV o INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR WHERE f.TIME_STAMP >= @fromDate AND f.TIME_STAMP <= @toDate AND f.KEY_NO IS NOT NULL AND f.KEY_NO != '' {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")} ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
            _logger.LogInformation("Executing SQL on {Environment}: {SqlQuery}", environment, shortSql);
            _logger.LogInformation("SQL Parameters: fromDate={FromDate}, toDate={ToDate}, orhStat={OrhStat}", 
                fromDate, toDate, orhStat);

            // Get all distinct orders
            using var orderReader = await orderCommand.ExecuteReaderAsync();
            var orderNumbers = new List<int>();
            
            while (await orderReader.ReadAsync())
            {
                var order = new OrdhuvDto
                {
                    Database = environment, // Add database identifier
                    OrhDokn = orderReader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = orderReader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = orderReader.IsDBNull(2) ? null : orderReader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = orderReader.IsDBNull(3) ? null : orderReader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = orderReader.IsDBNull(4) ? null : orderReader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = orderReader.IsDBNull(5) ? null : orderReader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = orderReader.IsDBNull(6) ? null : orderReader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = orderReader.IsDBNull(7) ? null : orderReader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = orderReader.IsDBNull(8) ? null : orderReader.GetDouble(8), // ORH_SUMMAINKL - Sum Including
                    OrhSummaexkl = orderReader.IsDBNull(9) ? null : orderReader.GetDouble(9), // ORH_SUMMAEXKL - Sum Excluding
                    OrhMils = orderReader.IsDBNull(10) ? null : orderReader.GetInt32(10), // ORH_MILS - Odometer Reading
                    OrhCreatedAt = orderReader.IsDBNull(11) ? null : orderReader.GetDateTime(11), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = orderReader.IsDBNull(12) ? null : orderReader.GetDateTime(12), // ORH_UPDATED_AT - Updated At
                    OrhBetkunr = orderReader.IsDBNull(13) ? null : orderReader.GetInt32(13), // ORH_BETKUNR - Payer Number
                    OrhDriverNo = orderReader.IsDBNull(14) ? null : orderReader.GetInt32(14), // ORH_DRIVER_NO - Driver Number
                    Invoices = new List<InvoiceIndividualDto>(),
                    OrderRows = new List<OrdrRadDto>(),
                    
                    // Customer data (ORH_KUNR)
                    Customer = orderReader.IsDBNull(15) ? null : CreateKunregDto(orderReader, 15, 19),
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(25) ? null : CreateKunregDto(orderReader, 25, 29),
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(35) ? null : CreateKunregDto(orderReader, 35, 39),
                    
                    // Vehicle data (ORH_RENR -> BILREG.BIL_RENR)
                    Vehicle = orderReader.IsDBNull(45) ? null : CreateBilregDto(orderReader, 45)
                };
                
                results.Add(order);
                orderNumbers.Add(order.OrhDokn);
            }
            
            _logger.LogDebug("Found {OrderCount} orders in {Environment}", orderNumbers.Count, environment);

            // Step 2: Get all invoices for these orders - only meaningful fields
            if (orderNumbers.Any())
            {
                // Process invoices in batches to avoid Firebird's 1500 value limit
                var invoiceData = new Dictionary<int, InvoiceIndividualDto>();
                var batchSize = 1000; // Safe batch size for Firebird
                var orderNumberBatches = orderNumbers.Chunk(batchSize);
                var totalInvoiceCount = 0;
                
                foreach (var batch in orderNumberBatches)
                {
                    var orderNumbersParam = string.Join(",", batch);
                    using var invoiceCommand = new FbCommand($@"
                    SELECT 
                        i.VEHICLE_NO,
                        i.MANUFACTURER,
                        i.MODEL,
                        i.VIN,
                        i.REGISTRATION_DATE,
                        i.MODEL_YEAR,
                        CASE WHEN i.OWNER_NO > 0 THEN i.OWNER_NO ELSE NULL END as OWNER_NO,
                        i.OWNER_NAME,
                        CASE WHEN i.OWNER_ADRESS_2 IS NOT NULL AND i.OWNER_ADRESS_2 != '' THEN i.OWNER_ADRESS_2 ELSE NULL END as OWNER_ADRESS_2,
                        CASE WHEN i.OWNER_ZIP_AND_CITY IS NOT NULL AND i.OWNER_ZIP_AND_CITY != '' THEN i.OWNER_ZIP_AND_CITY ELSE NULL END as OWNER_ZIP_AND_CITY,
                        CASE WHEN i.OWNER_PHONE IS NOT NULL AND i.OWNER_PHONE != '' THEN i.OWNER_PHONE ELSE NULL END as OWNER_PHONE,
                        CASE WHEN i.OWNER_MAIL IS NOT NULL AND i.OWNER_MAIL != '' THEN i.OWNER_MAIL ELSE NULL END as OWNER_MAIL,
                        CASE WHEN i.PAYER_NO > 0 THEN i.PAYER_NO ELSE NULL END as PAYER_NO,
                        i.PAYER_NAME,
                        CASE WHEN i.PAYER_ADRESS_2 IS NOT NULL AND i.PAYER_ADRESS_2 != '' THEN i.PAYER_ADRESS_2 ELSE NULL END as PAYER_ADRESS_2,
                        CASE WHEN i.PAYER_ZIP_AND_CITY IS NOT NULL AND i.PAYER_ZIP_AND_CITY != '' THEN i.PAYER_ZIP_AND_CITY ELSE NULL END as PAYER_ZIP_AND_CITY,
                        CASE WHEN i.PAYER_PHONE IS NOT NULL AND i.PAYER_PHONE != '' THEN i.PAYER_PHONE ELSE NULL END as PAYER_PHONE,
                        CASE WHEN i.PAYER_MAIL IS NOT NULL AND i.PAYER_MAIL != '' THEN i.PAYER_MAIL ELSE NULL END as PAYER_MAIL,
                        CASE WHEN i.PAYER_VATNO IS NOT NULL AND i.PAYER_VATNO != '' THEN i.PAYER_VATNO ELSE NULL END as PAYER_VATNO,
                        CASE WHEN i.DRIVER_NO > 0 THEN i.DRIVER_NO ELSE NULL END as DRIVER_NO,
                        i.DRIVER_NAME,
                        CASE WHEN i.DRIVER_ADRESS_2 IS NOT NULL AND i.DRIVER_ADRESS_2 != '' THEN i.DRIVER_ADRESS_2 ELSE NULL END as DRIVER_ADRESS_2,
                        CASE WHEN i.DRIVER_ZIP_AND_CITY IS NOT NULL AND i.DRIVER_ZIP_AND_CITY != '' THEN i.DRIVER_ZIP_AND_CITY ELSE NULL END as DRIVER_ZIP_AND_CITY,
                        CASE WHEN i.DRIVER_PHONE IS NOT NULL AND i.DRIVER_PHONE != '' THEN i.DRIVER_PHONE ELSE NULL END as DRIVER_PHONE,
                        CASE WHEN i.DRIVER_MAIL IS NOT NULL AND i.DRIVER_MAIL != '' THEN i.DRIVER_MAIL ELSE NULL END as DRIVER_MAIL,
                        i.INVOICE_NO,
                        f.ID,
                        f.TIME_STAMP,
                        f.TRANSACTION_NO,
                        f.DESCRIPTION,
                        CASE WHEN f.ERROR_CODE IS NOT NULL AND f.ERROR_CODE != '' THEN f.ERROR_CODE ELSE NULL END as ERROR_CODE,
                        CASE WHEN f.ERROR_MESSAGE IS NOT NULL AND f.ERROR_MESSAGE != '' THEN f.ERROR_MESSAGE ELSE NULL END as ERROR_MESSAGE,
                        f.LOG_TYPE,
                        f.KEY_NO
                    FROM INVOICEINDIVIDUAL i
                    INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                    WHERE i.INVOICE_NO IN ({orderNumbersParam})
                      AND f.KEY_NO IS NOT NULL 
                      AND f.KEY_NO != ''
                      AND f.TIME_STAMP >= @fromDate 
                      AND f.TIME_STAMP <= @toDate
                    ORDER BY i.INVOICE_NO, f.TIME_STAMP", connection);

                    invoiceCommand.Parameters.AddWithValue("@fromDate", fromDate);
                    invoiceCommand.Parameters.AddWithValue("@toDate", toDate);

                    // Log the invoice SQL command (shortened version)
                    var shortInvoiceSql = $"SELECT i.VEHICLE_NO, i.MANUFACTURER, i.MODEL, i.VIN, i.REGISTRATION_DATE, i.MODEL_YEAR, [Owner/Payer/Driver data], i.INVOICE_NO, f.ID, f.TIME_STAMP, f.TRANSACTION_NO, f.DESCRIPTION, f.ERROR_CODE, f.ERROR_MESSAGE, f.LOG_TYPE, f.KEY_NO FROM INVOICEINDIVIDUAL i INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO WHERE i.INVOICE_NO IN ({orderNumbersParam}) AND f.KEY_NO IS NOT NULL AND f.KEY_NO != '' AND f.TIME_STAMP >= @fromDate AND f.TIME_STAMP <= @toDate ORDER BY i.INVOICE_NO, f.TIME_STAMP";
                    _logger.LogInformation("Executing Invoice SQL batch on {Environment}: {SqlQuery}", environment, shortInvoiceSql);
                    _logger.LogInformation("Invoice SQL Parameters: fromDate={FromDate}, toDate={ToDate}, orderNumbers={OrderNumbers} (batch size: {BatchSize})", 
                        fromDate, toDate, orderNumbersParam, batch.Count());

                    using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                    var batchInvoiceCount = 0;
                    
                    while (await invoiceReader.ReadAsync())
                    {
                        var currentInvoiceNo = invoiceReader.GetInt32(25); // INVOICE_NO
                        batchInvoiceCount++;
                        totalInvoiceCount++;
                        
                        if (!invoiceData.ContainsKey(currentInvoiceNo))
                        {
                            invoiceData[currentInvoiceNo] = new InvoiceIndividualDto
                            {
                                VehicleNo = invoiceReader.IsDBNull(0) ? null : invoiceReader.GetString(0), // VEHICLE_NO
                                Manufacturer = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1), // MANUFACTURER
                                Model = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2), // MODEL
                                Vin = invoiceReader.IsDBNull(3) ? null : invoiceReader.GetString(3), // VIN
                                RegistrationDate = invoiceReader.IsDBNull(4) ? null : invoiceReader.GetDateTime(4), // REGISTRATION_DATE
                                ModelYear = invoiceReader.IsDBNull(5) ? null : (short)invoiceReader.GetInt32(5), // MODEL_YEAR
                                OwnerNo = invoiceReader.IsDBNull(6) ? null : invoiceReader.GetInt32(6), // OWNER_NO
                                OwnerName = invoiceReader.IsDBNull(7) ? null : invoiceReader.GetString(7), // OWNER_NAME
                                OwnerAddress2 = invoiceReader.IsDBNull(8) ? null : invoiceReader.GetString(8), // OWNER_ADRESS_2
                                OwnerZipAndCity = invoiceReader.IsDBNull(9) ? null : invoiceReader.GetString(9), // OWNER_ZIP_AND_CITY
                                OwnerPhone = invoiceReader.IsDBNull(10) ? null : invoiceReader.GetString(10), // OWNER_PHONE
                                OwnerMail = invoiceReader.IsDBNull(11) ? null : invoiceReader.GetString(11), // OWNER_MAIL
                                PayerNo = invoiceReader.IsDBNull(12) ? null : invoiceReader.GetInt32(12), // PAYER_NO
                                PayerName = invoiceReader.IsDBNull(13) ? null : invoiceReader.GetString(13), // PAYER_NAME
                                PayerAddress2 = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetString(14), // PAYER_ADRESS_2
                                PayerZipAndCity = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetString(15), // PAYER_ZIP_AND_CITY
                                PayerPhone = invoiceReader.IsDBNull(16) ? null : invoiceReader.GetString(16), // PAYER_PHONE
                                PayerMail = invoiceReader.IsDBNull(17) ? null : invoiceReader.GetString(17), // PAYER_MAIL
                                PayerVatNo = invoiceReader.IsDBNull(18) ? null : invoiceReader.GetString(18), // PAYER_VATNO
                                DriverNo = invoiceReader.IsDBNull(19) ? null : invoiceReader.GetInt32(19), // DRIVER_NO
                                DriverName = invoiceReader.IsDBNull(20) ? null : invoiceReader.GetString(20), // DRIVER_NAME
                                DriverAddress2 = invoiceReader.IsDBNull(21) ? null : invoiceReader.GetString(21), // DRIVER_ADRESS_2
                                DriverZipAndCity = invoiceReader.IsDBNull(22) ? null : invoiceReader.GetString(22), // DRIVER_ZIP_AND_CITY
                                DriverPhone = invoiceReader.IsDBNull(23) ? null : invoiceReader.GetString(23), // DRIVER_PHONE
                                DriverMail = invoiceReader.IsDBNull(24) ? null : invoiceReader.GetString(24), // DRIVER_MAIL
                                InvoiceNo = currentInvoiceNo,
                                
                                // Initialize Fortnox Logs array
                                FortnoxLogs = new List<FortnoxLogDto>()
                            };
                        }
                        
                        // Add Fortnox log to the invoice
                        var fortnoxLog = new FortnoxLogDto
                        {
                            Id = invoiceReader.GetInt32(26),                                           // FORTNOX_LOG.ID
                            TimeStamp = invoiceReader.IsDBNull(27) ? null : invoiceReader.GetDateTime(27), // FORTNOX_LOG.TIME_STAMP
                            TransactionNo = invoiceReader.IsDBNull(28) ? null : invoiceReader.GetInt32(28), // FORTNOX_LOG.TRANSACTION_NO
                            Description = invoiceReader.IsDBNull(29) ? null : invoiceReader.GetString(29), // FORTNOX_LOG.DESCRIPTION
                            ErrorCode = invoiceReader.IsDBNull(30) ? null : invoiceReader.GetString(30), // FORTNOX_LOG.ERROR_CODE
                            ErrorMessage = invoiceReader.IsDBNull(31) ? null : invoiceReader.GetString(31), // FORTNOX_LOG.ERROR_MESSAGE
                            LogType = invoiceReader.IsDBNull(32) ? null : invoiceReader.GetInt32(32), // FORTNOX_LOG.LOG_TYPE
                            KeyNo = invoiceReader.IsDBNull(33) ? null : invoiceReader.GetString(33)     // FORTNOX_LOG.KEY_NO
                        };
                        
                        invoiceData[currentInvoiceNo].FortnoxLogs.Add(fortnoxLog);
                    }
                }
                
                // Add invoices to their corresponding orders
                foreach (var invoice in invoiceData.Values)
                {
                    var order = results.FirstOrDefault(o => o.OrhDokn == invoice.InvoiceNo);
                    if (order != null)
                    {
                        // Calculate min timestamp for this invoice
                        if (invoice.FortnoxLogs.Any())
                        {
                            invoice.MinFortnoxTimeStamp = invoice.FortnoxLogs
                                .Where(log => log.TimeStamp.HasValue)
                                .Min(log => log.TimeStamp);
                        }
                        
                        order.Invoices.Add(invoice);
                    }
                }
                
                _logger.LogDebug("Found {InvoiceCount} invoices for {OrderCount} orders in {Environment}", 
                    totalInvoiceCount, orderNumbers.Count, environment);
            }
            
            // Step 3: Get ORDRAD data for all orders in a single query
            if (orderNumbers.Any())
            {
                try
                {
                    // Process ORDRAD data in batches to avoid Firebird's 1500 value limit
                    var ordrRadData = new Dictionary<int, List<OrdrRadDto>>();
                    var batchSize = 1000; // Safe batch size for Firebird
                    var orderNumberBatches = orderNumbers.Chunk(batchSize);
                    
                    foreach (var batch in orderNumberBatches)
                    {
                        try
                        {
                        using var ordrRadCommand = new FbCommand($@"
                            SELECT 
                                ORD_DOKN,
                                ORD_RADNR,
                                ORD_ARTN,
                                CAST(ORD_ARTB AS VARCHAR(1000) CHARACTER SET UTF8) as ORD_ARTB,
                                ORD_ANTA,
                                ORD_INPRIS,
                                ORD_RABA,
                                ORD_MOMS,
                                ORD_TYP,
                                ORD_KOD,
                                ORD_MATKOD,
                                ORD_SUMMAEXKL,
                                ORD_CREATED_AT,
                                ORD_UPDATED_AT
                            FROM ORDRAD 
                            WHERE ORD_DOKN IN ({string.Join(",", batch.Select((_, i) => $"@ordDokn{i}"))})
                            ORDER BY ORD_DOKN, ORD_RADNR", connection);

                            // Add parameters for each order number in this batch
                            for (int i = 0; i < batch.Count(); i++)
                            {
                                ordrRadCommand.Parameters.AddWithValue($"@ordDokn{i}", batch.ElementAt(i));
                            }

                            // Log the ORDRAD SQL command (shortened version)
                            var shortOrdrRadSql = $"SELECT ORD_DOKN, ORD_RADNR, ORD_ARTN, ORD_ARTB, ORD_ANTA, ORD_INPRIS, ORD_RABA, ORD_MOMS, ORD_TYP, ORD_KOD, ORD_MATKOD, ORD_SUMMAEXKL, ORD_CREATED_AT, ORD_UPDATED_AT FROM ORDRAD WHERE ORD_DOKN IN ({string.Join(",", batch)}) ORDER BY ORD_DOKN, ORD_RADNR";
                            _logger.LogInformation("Executing ORDRAD SQL batch on {Environment}: {SqlQuery}", environment, shortOrdrRadSql);
                            _logger.LogInformation("ORDRAD SQL Parameters: orderNumbers={OrderNumbers} (batch size: {BatchSize})", string.Join(",", batch), batch.Count());

                        using var ordrRadReader = await ordrRadCommand.ExecuteReaderAsync();
                        
                        while (await ordrRadReader.ReadAsync())
                        {
                            var rowOrderNo = ordrRadReader.GetInt32(0); // ORD_DOKN
                            var artbText = ordrRadReader.IsDBNull(3) ? null : ordrRadReader.GetString(3); // ORD_ARTB
                            var ordAnta = ordrRadReader.IsDBNull(4) ? 0 : ordrRadReader.GetDouble(4); // ORD_ANTA
                            var ordTyp = ordrRadReader.IsDBNull(8) ? null : ordrRadReader.GetString(8); // ORD_TYP
                            
                            // Only match categories for labor rows (ORD_TYP = "A") with non-zero quantity
                            var (matchedKeyword, matchedCategory) = (ordTyp == "A" && ordAnta != 0) 
                                ? MatchKeywords(artbText) 
                                : (null, null);
                            
                            if (!ordrRadData.ContainsKey(rowOrderNo))
                            {
                                ordrRadData[rowOrderNo] = new List<OrdrRadDto>();
                            }
                            
                            var ordrRad = new OrdrRadDto
                            {
                                OrdDokn = rowOrderNo,                                           // ORD_DOKN
                                OrdRadnr = ordrRadReader.IsDBNull(1) ? 0 : (int)ordrRadReader.GetDouble(1), // ORD_RADNR
                                OrdArtn = ordrRadReader.IsDBNull(2) ? null : ordrRadReader.GetString(2), // ORD_ARTN
                                OrdArtb = artbText, // ORD_ARTB
                                OrdAnta = ordAnta, // ORD_ANTA
                                OrdInpris = ordrRadReader.IsDBNull(5) ? 0 : ordrRadReader.GetDouble(5), // ORD_INPRIS
                                OrdRaba = ordrRadReader.IsDBNull(6) ? 0 : ordrRadReader.GetDouble(6), // ORD_RABA
                                OrdMoms = ordrRadReader.IsDBNull(7) ? 0 : ordrRadReader.GetDouble(7), // ORD_MOMS
                                OrdTyp = ordTyp, // ORD_TYP
                                OrdKod = ordrRadReader.IsDBNull(9) ? null : ordrRadReader.GetString(9), // ORD_KOD
                                OrdMatkod = ordrRadReader.IsDBNull(10) ? null : ordrRadReader.GetString(10), // ORD_MATKOD
                                OrdSummaexkl = ordrRadReader.IsDBNull(11) ? 0 : ordrRadReader.GetDouble(11), // ORD_SUMMAEXKL
                                OrdCreatedAt = ordrRadReader.IsDBNull(12) ? null : ordrRadReader.GetDateTime(12), // ORD_CREATED_AT
                                OrdUpdatedAt = ordrRadReader.IsDBNull(13) ? null : ordrRadReader.GetDateTime(13), // ORD_UPDATED_AT
                                MatchedKeyword = matchedKeyword,
                                MatchedCategory = matchedCategory
                            };
                            
                            ordrRadData[rowOrderNo].Add(ordrRad);
                        }
                        
                            _logger.LogDebug("Processed ORDRAD batch for {OrderCount} orders in {Environment}", batch.Count(), environment);
                        }
                        catch (Exception ordrRadEx)
                        {
                            _logger.LogError(ordrRadEx, "Failed to process ORDRAD batch in {Environment}", environment);
                        }
                    }
                    
                    var totalRowCount = ordrRadData.Values.Sum(rows => rows.Count);
                    
                    _logger.LogDebug("ORDRAD returned {RowCount} rows for {OrderCount} orders in {Environment}", 
                        totalRowCount, ordrRadData.Count, environment);
                    
                    // Add ORDRAD data to orders
                foreach (var order in results)
                {
                        if (ordrRadData.ContainsKey(order.OrhDokn))
                        {
                            order.OrderRows = ordrRadData[order.OrhDokn];
                            
                            // Check if order has any labor rows (ORD_TYP = "A" with non-zero quantity)
                            var hasLaborRows = order.OrderRows
                                .Any(row => row.OrdTyp == "A" && row.OrdAnta != 0);
                            
                            // Extract distinct categories from order rows
                            var categories = order.OrderRows
                                .Where(row => !string.IsNullOrEmpty(row.MatchedCategory))
                                .Select(row => row.MatchedCategory!)
                                .Distinct()
                                .OrderBy(cat => cat)
                        .ToList();
                    
                            // If order has labor rows but no categories found, default to "Reparation"
                            if (hasLaborRows && categories.Count == 0)
                            {
                                categories.Add("Reparation");
                            }
                    
                            order.Categories = categories;
                            
                            // Set BilVehiclecat to "Husbil" if it's null/empty and at least one order row has OrdMatkod = "HU"
                            if (order.Vehicle != null && string.IsNullOrEmpty(order.Vehicle.BilVehiclecat))
                            {
                                var hasHusbilRow = order.OrderRows.Any(row => row.OrdMatkod == "HU");
                                if (hasHusbilRow)
                                {
                                    order.Vehicle.BilVehiclecat = "Husbil";
                                }
                            }
                            
                            _logger.LogDebug("Added {RowCount} ORDRAD rows to order {OrderNo} in {Environment}", 
                                order.OrderRows.Count, order.OrhDokn, environment);
                        }
                    }
                    
                    _logger.LogDebug("Found ORDRAD data for {OrderCount} orders in {Environment}", ordrRadData.Count, environment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch order row data in {Environment}: {Error}", environment, ex.Message);
                }
            }
            
            // Count calculation is now done at the main method level across all environments
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query environment {Environment}: {Error}", environment, ex.Message);
            return new List<OrdhuvDto>(); // Return empty list instead of throwing to allow other environments to succeed
        }
    }

    private static KunregDto CreateKunregDto(FbDataReader reader, int startIndex, int endIndex)
    {
        var kunKunr = reader.IsDBNull(startIndex) ? 0 : reader.GetInt32(startIndex);
        var kunNamn = reader.IsDBNull(startIndex + 1) ? null : reader.GetString(startIndex + 1);
        var kunAdr1 = reader.IsDBNull(startIndex + 2) ? null : reader.GetString(startIndex + 2);
        var kunAdr2 = reader.IsDBNull(startIndex + 3) ? null : reader.GetString(startIndex + 3);
        var kunPadr = reader.IsDBNull(startIndex + 4) ? null : reader.GetString(startIndex + 4);
        var kunOrgn = reader.IsDBNull(startIndex + 5) ? null : reader.GetString(startIndex + 5);
        var kunTel1 = reader.IsDBNull(startIndex + 6) ? null : reader.GetString(startIndex + 6);
        var kunTel2 = reader.IsDBNull(startIndex + 7) ? null : reader.GetString(startIndex + 7);
        var kunTel3 = reader.IsDBNull(startIndex + 8) ? null : reader.GetString(startIndex + 8);
        var kunEpostadress = reader.IsDBNull(startIndex + 9) ? null : reader.GetString(startIndex + 9);

        // Parse customer type and birthdate from kunOrgn
        var (customerType, birthDate) = ParseCustomerInfo(kunOrgn);

        // Process phone numbers to find mobile phone
        var mobilePhone = ProcessPhoneNumbers(kunTel1, kunTel2, kunTel3);

        // Parse name into first and last name or company name
        var (firstName, lastName, companyName) = ParseName(kunNamn, customerType);

        // Parse postal address into zip code and city
        var (zipCode, city) = ParsePostalAddress(kunPadr);

        return new KunregDto
        {
            KunKunr = kunKunr,
            KunNamn = kunNamn,
            KunAdr1 = kunAdr1,
            KunAdr2 = kunAdr2,
            KunPadr = kunPadr,
            KunOrgn = kunOrgn,
            KunTel1 = kunTel1,
            KunTel2 = kunTel2,
            KunTel3 = kunTel3,
            KunEpostadress = kunEpostadress,
            CustomerType = customerType,
            BirthDate = birthDate,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            ZipCode = zipCode,
            City = city,
            MobilePhone = mobilePhone
        };
    }

    private static BilregDto CreateBilregDto(FbDataReader reader, int startIndex)
    {
        var bilRenr = reader.IsDBNull(startIndex) ? null : reader.GetString(startIndex);
        var bilBeteckning = reader.IsDBNull(startIndex + 1) ? null : reader.GetString(startIndex + 1);
        var bilArsm = reader.IsDBNull(startIndex + 2) ? (short?)null : (short)reader.GetInt32(startIndex + 2);
        var fabrikat = reader.IsDBNull(startIndex + 3) ? null : reader.GetString(startIndex + 3);
        var bilVehiclecat = reader.IsDBNull(startIndex + 4) ? null : reader.GetString(startIndex + 4);
        var bilFuel = reader.IsDBNull(startIndex + 5) ? null : reader.GetString(startIndex + 5);

        return new BilregDto
        {
            BilBetekning = bilBeteckning,
            BilArsm = bilArsm,
            Fabrikat = fabrikat,
            BilVehiclecat = bilVehiclecat,
            BilFuel = bilFuel
        };
    }

    public async Task<IEnumerable<OrdhuvDto>> GetOrdersByDateAsync(DateTime fromDate, DateTime toDate, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null, bool? invoiced = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allResults = new List<OrdhuvDto>();

        try
        {
            // Determine which environments to query
            var targetEnvironments = GetTargetEnvironments(environment, environments);
            
            _logger.LogInformation("Querying {EnvironmentCount} environments by date: {Environments}", 
                targetEnvironments.Length, string.Join(", ", targetEnvironments));

            // Query all environments in parallel
            var tasks = targetEnvironments.Select(env => QueryEnvironmentByDateAsync(env, fromDate, toDate, orhStat, customerType, invoiced));
            var environmentResults = await Task.WhenAll(tasks);
            
            // Combine all results (no deduplication as requested)
            foreach (var envResults in environmentResults)
            {
                allResults.AddRange(envResults);
            }
            
            // Apply customerType filtering if specified
            if (!string.IsNullOrEmpty(customerType))
            {
                var originalCount = allResults.Count;
                allResults = FilterByCustomerType(allResults, customerType).ToList();
                _logger.LogInformation("Applied customerType filter '{CustomerType}': {OriginalCount} -> {FilteredCount} orders", 
                    customerType, originalCount, allResults.Count);
            }
            
            sw.Stop();
            _logger.LogInformation("Date-based query completed in {ElapsedMs}ms. Found {Count} orders from {EnvironmentCount} environments", 
                sw.ElapsedMilliseconds, allResults.Count, targetEnvironments.Length);

            return allResults;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Date-based query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<OrdhuvDto>> GetOrdersByLicensePlatesAsync(List<LicensePlateItem> licensePlates, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null, bool? invoiced = null, DateTime? fromDate = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allResults = new List<OrdhuvDto>();

        try
        {
            // Determine which environments to query
            var targetEnvironments = GetTargetEnvironments(environment, environments);
            
            _logger.LogInformation("Querying {EnvironmentCount} environments for license plates: {LicensePlates}", 
                targetEnvironments.Length, string.Join(", ", licensePlates.Select(lp => lp.Licenseplate)));

            // Query all environments in parallel
            var tasks = targetEnvironments.Select(env => QueryEnvironmentByLicensePlatesAsync(env, licensePlates, orhStat, customerType, invoiced, fromDate));
            var environmentResults = await Task.WhenAll(tasks);
            
            // Combine all results (no deduplication as requested)
            foreach (var envResults in environmentResults)
            {
                allResults.AddRange(envResults);
            }
            
            // Apply customerType filtering if specified
            if (!string.IsNullOrEmpty(customerType))
            {
                var originalCount = allResults.Count;
                allResults = FilterByCustomerType(allResults, customerType).ToList();
                _logger.LogInformation("Applied customerType filter '{CustomerType}': {OriginalCount} -> {FilteredCount} orders", 
                    customerType, originalCount, allResults.Count);
            }
            
            // Calculate count/fraction for each ORDRAD row per license plate across ALL environments
            // Sum of all rows for each license plate should equal 1.0
            var ordersByLicensePlate = allResults.GroupBy(order => order.OrhRenr).ToList();
            foreach (var licensePlateGroup in ordersByLicensePlate)
            {
                var licensePlate = licensePlateGroup.Key;
                var allOrderRowsForLicensePlate = licensePlateGroup.SelectMany(order => order.OrderRows).ToList();
                
                if (allOrderRowsForLicensePlate.Any())
                {
                    var fractionPerRow = 1.0 / allOrderRowsForLicensePlate.Count;
                    foreach (var row in allOrderRowsForLicensePlate)
                    {
                        row.Count = fractionPerRow;
                    }
                    
                    _logger.LogDebug("Calculated Count for license plate {LicensePlate} across all environments: {RowCount} rows, fraction per row: {Fraction}", 
                        licensePlate, allOrderRowsForLicensePlate.Count, fractionPerRow);
                }
            }
            
            sw.Stop();
            _logger.LogInformation("License plates query completed in {ElapsedMs}ms. Found {Count} orders from {EnvironmentCount} environments", 
                sw.ElapsedMilliseconds, allResults.Count, targetEnvironments.Length);

            return allResults;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "License plates query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Queries a single environment for orders by license plates
    /// </summary>
    private async Task<List<OrdhuvDto>> QueryEnvironmentByLicensePlatesAsync(string environment, List<LicensePlateItem> licensePlates, string? orhStat = null, string? customerType = null, bool? invoiced = null, DateTime? fromDate = null)
    {
        var results = new List<OrdhuvDto>();

        try
        {
            using var connection = new FbConnection(_databaseConfig.GetConnectionString(environment));
            await connection.OpenAsync();
            
            // Create parameter placeholders for license plates
            var licensePlateParams = string.Join(",", licensePlates.Select((_, i) => $"@licensePlate{i}"));
            
            // Step 1: Get orders that match the license plates with customer data
            var sqlQuery = $@"
                SELECT DISTINCT
                    o.ORH_DOKN,
                    o.ORH_KUNR,
                    o.ORH_DOKD,
                    o.ORH_RENR,
                    o.ORH_STAT,
                    o.ORH_LOVDAT,
                    o.ORH_FAKTURERAD,
                    o.ORH_NAMN,
                    o.ORH_SUMMAINKL,
                    o.ORH_SUMMAEXKL,
                    o.ORH_MILS,
                    o.ORH_CREATED_AT,
                    o.ORH_UPDATED_AT,
                    CASE WHEN o.ORH_BETKUNR > 0 THEN o.ORH_BETKUNR ELSE NULL END as ORH_BETKUNR,
                    CASE WHEN o.ORH_DRIVER_NO > 0 THEN o.ORH_DRIVER_NO ELSE NULL END as ORH_DRIVER_NO,
                    -- Customer data (ORH_KUNR)
                    c.KUN_KUNR as CUST_KUN_KUNR,
                    c.KUN_NAMN as CUST_KUN_NAMN,
                    c.KUN_ADR1 as CUST_KUN_ADR1,
                    c.KUN_ADR2 as CUST_KUN_ADR2,
                    c.KUN_PADR as CUST_KUN_PADR,
                    c.KUN_ORGN as CUST_KUN_ORGN,
                    c.KUN_TEL1 as CUST_KUN_TEL1,
                    c.KUN_TEL2 as CUST_KUN_TEL2,
                    c.KUN_TEL3 as CUST_KUN_TEL3,
                    c.KUN_EPOSTADRESS as CUST_KUN_EPOSTADRESS,
                    -- Payer data (ORH_BETKUNR)
                    p.KUN_KUNR as PAYER_KUN_KUNR,
                    p.KUN_NAMN as PAYER_KUN_NAMN,
                    p.KUN_ADR1 as PAYER_KUN_ADR1,
                    p.KUN_ADR2 as PAYER_KUN_ADR2,
                    p.KUN_PADR as PAYER_KUN_PADR,
                    p.KUN_ORGN as PAYER_KUN_ORGN,
                    p.KUN_TEL1 as PAYER_KUN_TEL1,
                    p.KUN_TEL2 as PAYER_KUN_TEL2,
                    p.KUN_TEL3 as PAYER_KUN_TEL3,
                    p.KUN_EPOSTADRESS as PAYER_KUN_EPOSTADRESS,
                    -- Driver data (ORH_DRIVER_NO)
                    d.KUN_KUNR as DRIVER_KUN_KUNR,
                    d.KUN_NAMN as DRIVER_KUN_NAMN,
                    d.KUN_ADR1 as DRIVER_KUN_ADR1,
                    d.KUN_ADR2 as DRIVER_KUN_ADR2,
                    d.KUN_PADR as DRIVER_KUN_PADR,
                    d.KUN_ORGN as DRIVER_KUN_ORGN,
                    d.KUN_TEL1 as DRIVER_KUN_TEL1,
                    d.KUN_TEL2 as DRIVER_KUN_TEL2,
                    d.KUN_TEL3 as DRIVER_KUN_TEL3,
                    d.KUN_EPOSTADRESS as DRIVER_KUN_EPOSTADRESS,
                    -- Vehicle data (ORH_RENR -> BILREG.BIL_RENR)
                    b.BIL_RENR as VEHICLE_BIL_RENR,
                    b.BIL_BETECKNING as VEHICLE_BIL_BETECKNING,
                    b.BIL_ARSM as VEHICLE_BIL_ARSM,
                    b.FABRIKAT as VEHICLE_FABRIKAT,
                    b.BIL_VEHICLECAT as VEHICLE_BIL_VEHICLECAT,
                    b.BIL_FUEL as VEHICLE_BIL_FUEL
                FROM ORDHUV o
                {(invoiced == true ? 
                    "INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO\n                INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO" : 
                    "LEFT JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO\n                LEFT JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO")}
                LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR
                LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR
                LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR
                LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR
                WHERE o.ORH_RENR IN ({licensePlateParams})
                  {(invoiced == true ? "AND f.KEY_NO IS NOT NULL AND f.KEY_NO != ''" : "")}
                  {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")}
                  {(fromDate.HasValue ? (invoiced == true ? "AND f.TIMESTAMP >= @fromDate" : "AND o.ORH_CREATED_AT >= @fromDate") : "")}
                ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
                
            using var orderCommand = new FbCommand(sqlQuery, connection);
            
            // Add parameters for each license plate
            for (int i = 0; i < licensePlates.Count; i++)
            {
                orderCommand.Parameters.AddWithValue($"@licensePlate{i}", licensePlates[i].Licenseplate);
            }
            
            if (!string.IsNullOrEmpty(orhStat))
            {
                orderCommand.Parameters.AddWithValue("@orhStat", orhStat);
            }
            
            if (fromDate.HasValue)
            {
                orderCommand.Parameters.AddWithValue("@fromDate", fromDate.Value);
            }

            // Log the SQL command (shortened version)
            var shortSql = $"SELECT DISTINCT o.ORH_DOKN, o.ORH_KUNR, o.ORH_DOKD, o.ORH_RENR, o.ORH_STAT, o.ORH_LOVDAT, o.ORH_FAKTURERAD, o.ORH_NAMN, o.ORH_SUMMAINKL, o.ORH_CREATED_AT, o.ORH_UPDATED_AT, [Customer/Payer/Driver/Vehicle data] FROM ORDHUV o INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR WHERE o.ORH_RENR IN ({licensePlateParams}) AND f.KEY_NO IS NOT NULL AND f.KEY_NO != '' {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")} ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
            _logger.LogInformation("Executing license plates SQL on {Environment}: {SqlQuery}", environment, shortSql);
            _logger.LogInformation("License plates SQL Parameters: licensePlates={LicensePlates}, orhStat={OrhStat}, invoiced={Invoiced}, fromDate={FromDate}", 
                string.Join(",", licensePlates.Select(lp => lp.Licenseplate)), orhStat, invoiced, fromDate);

            // Get all distinct orders
            using var orderReader = await orderCommand.ExecuteReaderAsync();
            var orderNumbers = new List<int>();
            
            while (await orderReader.ReadAsync())
            {
                var order = new OrdhuvDto
                {
                    Database = environment, // Add database identifier
                    OrhDokn = orderReader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = orderReader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = orderReader.IsDBNull(2) ? null : orderReader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = orderReader.IsDBNull(3) ? null : orderReader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = orderReader.IsDBNull(4) ? null : orderReader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = orderReader.IsDBNull(5) ? null : orderReader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = orderReader.IsDBNull(6) ? null : orderReader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = orderReader.IsDBNull(7) ? null : orderReader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = orderReader.IsDBNull(8) ? null : orderReader.GetDouble(8), // ORH_SUMMAINKL - Sum Including
                    OrhSummaexkl = orderReader.IsDBNull(9) ? null : orderReader.GetDouble(9), // ORH_SUMMAEXKL - Sum Excluding
                    OrhMils = orderReader.IsDBNull(10) ? null : orderReader.GetInt32(10), // ORH_MILS - Odometer Reading
                    OrhCreatedAt = orderReader.IsDBNull(11) ? null : orderReader.GetDateTime(11), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = orderReader.IsDBNull(12) ? null : orderReader.GetDateTime(12), // ORH_UPDATED_AT - Updated At
                    OrhBetkunr = orderReader.IsDBNull(13) ? null : orderReader.GetInt32(13), // ORH_BETKUNR - Payer Number
                    OrhDriverNo = orderReader.IsDBNull(14) ? null : orderReader.GetInt32(14), // ORH_DRIVER_NO - Driver Number
                    Invoices = new List<InvoiceIndividualDto>(),
                    OrderRows = new List<OrdrRadDto>(),
                    
                    // Customer data (ORH_KUNR)
                    Customer = orderReader.IsDBNull(15) ? null : CreateKunregDto(orderReader, 15, 19),
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(25) ? null : CreateKunregDto(orderReader, 25, 29),
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(35) ? null : CreateKunregDto(orderReader, 35, 39),
                    
                    // Vehicle data (ORH_RENR -> BILREG.BIL_RENR)
                    Vehicle = orderReader.IsDBNull(45) ? null : CreateBilregDto(orderReader, 45)
                };
                
                results.Add(order);
                orderNumbers.Add(order.OrhDokn);
            }
            
            _logger.LogDebug("Found {OrderCount} orders in {Environment}", orderNumbers.Count, environment);

            // Step 2: Get all invoices for these orders - only meaningful fields
            if (orderNumbers.Any())
            {
                // Process invoices in batches to avoid Firebird's 1500 value limit
                var invoiceData = new Dictionary<int, InvoiceIndividualDto>();
                var batchSize = 1000; // Safe batch size for Firebird
                var orderNumberBatches = orderNumbers.Chunk(batchSize);
                var totalInvoiceCount = 0;
                
                foreach (var batch in orderNumberBatches)
                {
                    var orderNumbersParam = string.Join(",", batch);
                    using var invoiceCommand = new FbCommand($@"
                        SELECT 
                            i.VEHICLE_NO,
                            i.MANUFACTURER,
                            i.MODEL,
                            i.VIN,
                            i.REGISTRATION_DATE,
                            i.MODEL_YEAR,
                            CASE WHEN i.OWNER_NO > 0 THEN i.OWNER_NO ELSE NULL END as OWNER_NO,
                            i.OWNER_NAME,
                            CASE WHEN i.OWNER_ADRESS_2 IS NOT NULL AND i.OWNER_ADRESS_2 != '' THEN i.OWNER_ADRESS_2 ELSE NULL END as OWNER_ADRESS_2,
                            CASE WHEN i.OWNER_ZIP_AND_CITY IS NOT NULL AND i.OWNER_ZIP_AND_CITY != '' THEN i.OWNER_ZIP_AND_CITY ELSE NULL END as OWNER_ZIP_AND_CITY,
                            CASE WHEN i.OWNER_PHONE IS NOT NULL AND i.OWNER_PHONE != '' THEN i.OWNER_PHONE ELSE NULL END as OWNER_PHONE,
                            CASE WHEN i.OWNER_MAIL IS NOT NULL AND i.OWNER_MAIL != '' THEN i.OWNER_MAIL ELSE NULL END as OWNER_MAIL,
                            CASE WHEN i.PAYER_NO > 0 THEN i.PAYER_NO ELSE NULL END as PAYER_NO,
                            i.PAYER_NAME,
                            CASE WHEN i.PAYER_ADRESS_2 IS NOT NULL AND i.PAYER_ADRESS_2 != '' THEN i.PAYER_ADRESS_2 ELSE NULL END as PAYER_ADRESS_2,
                            CASE WHEN i.PAYER_ZIP_AND_CITY IS NOT NULL AND i.PAYER_ZIP_AND_CITY != '' THEN i.PAYER_ZIP_AND_CITY ELSE NULL END as PAYER_ZIP_AND_CITY,
                            CASE WHEN i.PAYER_PHONE IS NOT NULL AND i.PAYER_PHONE != '' THEN i.PAYER_PHONE ELSE NULL END as PAYER_PHONE,
                            CASE WHEN i.PAYER_MAIL IS NOT NULL AND i.PAYER_MAIL != '' THEN i.PAYER_MAIL ELSE NULL END as PAYER_MAIL,
                            CASE WHEN i.PAYER_VATNO IS NOT NULL AND i.PAYER_VATNO != '' THEN i.PAYER_VATNO ELSE NULL END as PAYER_VATNO,
                            CASE WHEN i.DRIVER_NO > 0 THEN i.DRIVER_NO ELSE NULL END as DRIVER_NO,
                            i.DRIVER_NAME,
                            CASE WHEN i.DRIVER_ADRESS_2 IS NOT NULL AND i.DRIVER_ADRESS_2 != '' THEN i.DRIVER_ADRESS_2 ELSE NULL END as DRIVER_ADRESS_2,
                            CASE WHEN i.DRIVER_ZIP_AND_CITY IS NOT NULL AND i.DRIVER_ZIP_AND_CITY != '' THEN i.DRIVER_ZIP_AND_CITY ELSE NULL END as DRIVER_ZIP_AND_CITY,
                            CASE WHEN i.DRIVER_PHONE IS NOT NULL AND i.DRIVER_PHONE != '' THEN i.DRIVER_PHONE ELSE NULL END as DRIVER_PHONE,
                            CASE WHEN i.DRIVER_MAIL IS NOT NULL AND i.DRIVER_MAIL != '' THEN i.DRIVER_MAIL ELSE NULL END as DRIVER_MAIL,
                            i.INVOICE_NO,
                            f.ID,
                            f.TIME_STAMP,
                            f.TRANSACTION_NO,
                            f.DESCRIPTION,
                            CASE WHEN f.ERROR_CODE IS NOT NULL AND f.ERROR_CODE != '' THEN f.ERROR_CODE ELSE NULL END as ERROR_CODE,
                            CASE WHEN f.ERROR_MESSAGE IS NOT NULL AND f.ERROR_MESSAGE != '' THEN f.ERROR_MESSAGE ELSE NULL END as ERROR_MESSAGE,
                            f.LOG_TYPE,
                            f.KEY_NO
                        FROM INVOICEINDIVIDUAL i
                        INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                        WHERE i.INVOICE_NO IN ({orderNumbersParam})
                          AND f.KEY_NO IS NOT NULL 
                          AND f.KEY_NO != ''
                        ORDER BY i.INVOICE_NO, f.TIME_STAMP", connection);

                    // Log the invoice SQL command (shortened version)
                    var shortInvoiceSql = $"SELECT i.VEHICLE_NO, i.MANUFACTURER, i.MODEL, i.VIN, i.REGISTRATION_DATE, i.MODEL_YEAR, [Owner/Payer/Driver data], i.INVOICE_NO, f.ID, f.TIME_STAMP, f.TRANSACTION_NO, f.DESCRIPTION, f.ERROR_CODE, f.ERROR_MESSAGE, f.LOG_TYPE, f.KEY_NO FROM INVOICEINDIVIDUAL i INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO WHERE i.INVOICE_NO IN ({orderNumbersParam}) AND f.KEY_NO IS NOT NULL AND f.KEY_NO != '' ORDER BY i.INVOICE_NO, f.TIME_STAMP";
                    _logger.LogInformation("Executing Invoice SQL batch on {Environment}: {SqlQuery}", environment, shortInvoiceSql);
                    _logger.LogInformation("Invoice SQL Parameters: orderNumbers={OrderNumbers} (batch size: {BatchSize})", orderNumbersParam, batch.Count());

                    using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                    var batchInvoiceCount = 0;
                    
                    while (await invoiceReader.ReadAsync())
                    {
                        var currentInvoiceNo = invoiceReader.GetInt32(25); // INVOICE_NO
                        batchInvoiceCount++;
                        totalInvoiceCount++;
                        
                        if (!invoiceData.ContainsKey(currentInvoiceNo))
                        {
                            invoiceData[currentInvoiceNo] = new InvoiceIndividualDto
                            {
                                VehicleNo = invoiceReader.IsDBNull(0) ? null : invoiceReader.GetString(0), // VEHICLE_NO
                                Manufacturer = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1), // MANUFACTURER
                                Model = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2), // MODEL
                                Vin = invoiceReader.IsDBNull(3) ? null : invoiceReader.GetString(3), // VIN
                                RegistrationDate = invoiceReader.IsDBNull(4) ? null : invoiceReader.GetDateTime(4), // REGISTRATION_DATE
                                ModelYear = invoiceReader.IsDBNull(5) ? null : (short)invoiceReader.GetInt32(5), // MODEL_YEAR
                                OwnerNo = invoiceReader.IsDBNull(6) ? null : invoiceReader.GetInt32(6), // OWNER_NO
                                OwnerName = invoiceReader.IsDBNull(7) ? null : invoiceReader.GetString(7), // OWNER_NAME
                                OwnerAddress2 = invoiceReader.IsDBNull(8) ? null : invoiceReader.GetString(8), // OWNER_ADRESS_2
                                OwnerZipAndCity = invoiceReader.IsDBNull(9) ? null : invoiceReader.GetString(9), // OWNER_ZIP_AND_CITY
                                OwnerPhone = invoiceReader.IsDBNull(10) ? null : invoiceReader.GetString(10), // OWNER_PHONE
                                OwnerMail = invoiceReader.IsDBNull(11) ? null : invoiceReader.GetString(11), // OWNER_MAIL
                                PayerNo = invoiceReader.IsDBNull(12) ? null : invoiceReader.GetInt32(12), // PAYER_NO
                                PayerName = invoiceReader.IsDBNull(13) ? null : invoiceReader.GetString(13), // PAYER_NAME
                                PayerAddress2 = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetString(14), // PAYER_ADRESS_2
                                PayerZipAndCity = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetString(15), // PAYER_ZIP_AND_CITY
                                PayerPhone = invoiceReader.IsDBNull(16) ? null : invoiceReader.GetString(16), // PAYER_PHONE
                                PayerMail = invoiceReader.IsDBNull(17) ? null : invoiceReader.GetString(17), // PAYER_MAIL
                                PayerVatNo = invoiceReader.IsDBNull(18) ? null : invoiceReader.GetString(18), // PAYER_VATNO
                                DriverNo = invoiceReader.IsDBNull(19) ? null : invoiceReader.GetInt32(19), // DRIVER_NO
                                DriverName = invoiceReader.IsDBNull(20) ? null : invoiceReader.GetString(20), // DRIVER_NAME
                                DriverAddress2 = invoiceReader.IsDBNull(21) ? null : invoiceReader.GetString(21), // DRIVER_ADRESS_2
                                DriverZipAndCity = invoiceReader.IsDBNull(22) ? null : invoiceReader.GetString(22), // DRIVER_ZIP_AND_CITY
                                DriverPhone = invoiceReader.IsDBNull(23) ? null : invoiceReader.GetString(23), // DRIVER_PHONE
                                DriverMail = invoiceReader.IsDBNull(24) ? null : invoiceReader.GetString(24), // DRIVER_MAIL
                                InvoiceNo = currentInvoiceNo,
                                
                                // Initialize Fortnox Logs array
                                FortnoxLogs = new List<FortnoxLogDto>()
                            };
                        }
                        
                        // Add Fortnox log to the invoice
                        var fortnoxLog = new FortnoxLogDto
                        {
                            Id = invoiceReader.GetInt32(26),                                           // FORTNOX_LOG.ID
                            TimeStamp = invoiceReader.IsDBNull(27) ? null : invoiceReader.GetDateTime(27), // FORTNOX_LOG.TIME_STAMP
                            TransactionNo = invoiceReader.IsDBNull(28) ? null : invoiceReader.GetInt32(28), // FORTNOX_LOG.TRANSACTION_NO
                            Description = invoiceReader.IsDBNull(29) ? null : invoiceReader.GetString(29), // FORTNOX_LOG.DESCRIPTION
                            ErrorCode = invoiceReader.IsDBNull(30) ? null : invoiceReader.GetString(30), // FORTNOX_LOG.ERROR_CODE
                            ErrorMessage = invoiceReader.IsDBNull(31) ? null : invoiceReader.GetString(31), // FORTNOX_LOG.ERROR_MESSAGE
                            LogType = invoiceReader.IsDBNull(32) ? null : invoiceReader.GetInt32(32), // FORTNOX_LOG.LOG_TYPE
                            KeyNo = invoiceReader.IsDBNull(33) ? null : invoiceReader.GetString(33)     // FORTNOX_LOG.KEY_NO
                        };
                        
                        invoiceData[currentInvoiceNo].FortnoxLogs.Add(fortnoxLog);
                    }
                }
                
                // Add invoices to their corresponding orders
                foreach (var invoice in invoiceData.Values)
                {
                    var order = results.FirstOrDefault(o => o.OrhDokn == invoice.InvoiceNo);
                    if (order != null)
                    {
                        // Calculate min timestamp for this invoice
                        if (invoice.FortnoxLogs.Any())
                        {
                            invoice.MinFortnoxTimeStamp = invoice.FortnoxLogs
                                .Where(log => log.TimeStamp.HasValue)
                                .Min(log => log.TimeStamp);
                        }
                        
                        order.Invoices.Add(invoice);
                    }
                }
                
                _logger.LogDebug("Found {InvoiceCount} invoices for {OrderCount} orders in {Environment}", 
                    totalInvoiceCount, orderNumbers.Count, environment);
            }
            
            // Step 3: Get ORDRAD data for all orders in a single query
            if (orderNumbers.Any())
            {
                try
                {
                    // Process ORDRAD data in batches to avoid Firebird's 1500 value limit
                    var ordrRadData = new Dictionary<int, List<OrdrRadDto>>();
                    var batchSize = 1000; // Safe batch size for Firebird
                    var orderNumberBatches = orderNumbers.Chunk(batchSize);
                    
                    foreach (var batch in orderNumberBatches)
                    {
                        try
                        {
                        using var ordrRadCommand = new FbCommand($@"
                            SELECT 
                                ORD_DOKN,
                                ORD_RADNR,
                                ORD_ARTN,
                                CAST(ORD_ARTB AS VARCHAR(1000) CHARACTER SET UTF8) as ORD_ARTB,
                                ORD_ANTA,
                                ORD_INPRIS,
                                ORD_RABA,
                                ORD_MOMS,
                                ORD_TYP,
                                ORD_KOD,
                                ORD_MATKOD,
                                ORD_SUMMAEXKL,
                                ORD_CREATED_AT,
                                ORD_UPDATED_AT
                            FROM ORDRAD 
                            WHERE ORD_DOKN IN ({string.Join(",", batch.Select((_, i) => $"@ordDokn{i}"))})
                            ORDER BY ORD_DOKN, ORD_RADNR", connection);

                            // Add parameters for each order number in this batch
                            for (int i = 0; i < batch.Count(); i++)
                            {
                                ordrRadCommand.Parameters.AddWithValue($"@ordDokn{i}", batch.ElementAt(i));
                            }

                            // Log the ORDRAD SQL command (shortened version)
                            var shortOrdrRadSql = $"SELECT ORD_DOKN, ORD_RADNR, ORD_ARTN, ORD_ARTB, ORD_ANTA, ORD_INPRIS, ORD_RABA, ORD_MOMS, ORD_TYP, ORD_KOD, ORD_MATKOD, ORD_SUMMAEXKL, ORD_CREATED_AT, ORD_UPDATED_AT FROM ORDRAD WHERE ORD_DOKN IN ({string.Join(",", batch)}) ORDER BY ORD_DOKN, ORD_RADNR";
                            _logger.LogInformation("Executing ORDRAD SQL batch on {Environment}: {SqlQuery}", environment, shortOrdrRadSql);
                            _logger.LogInformation("ORDRAD SQL Parameters: orderNumbers={OrderNumbers} (batch size: {BatchSize})", string.Join(",", batch), batch.Count());

                        using var ordrRadReader = await ordrRadCommand.ExecuteReaderAsync();
                        
                        while (await ordrRadReader.ReadAsync())
                        {
                            var rowOrderNo = ordrRadReader.GetInt32(0); // ORD_DOKN
                            var artbText = ordrRadReader.IsDBNull(3) ? null : ordrRadReader.GetString(3); // ORD_ARTB
                            var ordAnta = ordrRadReader.IsDBNull(4) ? 0 : ordrRadReader.GetDouble(4); // ORD_ANTA
                            var ordTyp = ordrRadReader.IsDBNull(8) ? null : ordrRadReader.GetString(8); // ORD_TYP
                            
                            // Only match categories for labor rows (ORD_TYP = "A") with non-zero quantity
                            var (matchedKeyword, matchedCategory) = (ordTyp == "A" && ordAnta != 0) 
                                ? MatchKeywords(artbText) 
                                : (null, null);
                            
                            if (!ordrRadData.ContainsKey(rowOrderNo))
                            {
                                ordrRadData[rowOrderNo] = new List<OrdrRadDto>();
                            }
                            
                            var ordrRad = new OrdrRadDto
                            {
                                OrdDokn = rowOrderNo,                                           // ORD_DOKN
                                OrdRadnr = ordrRadReader.IsDBNull(1) ? 0 : (int)ordrRadReader.GetDouble(1), // ORD_RADNR
                                OrdArtn = ordrRadReader.IsDBNull(2) ? null : ordrRadReader.GetString(2), // ORD_ARTN
                                OrdArtb = artbText, // ORD_ARTB
                                OrdAnta = ordAnta, // ORD_ANTA
                                OrdInpris = ordrRadReader.IsDBNull(5) ? 0 : ordrRadReader.GetDouble(5), // ORD_INPRIS
                                OrdRaba = ordrRadReader.IsDBNull(6) ? 0 : ordrRadReader.GetDouble(6), // ORD_RABA
                                OrdMoms = ordrRadReader.IsDBNull(7) ? 0 : ordrRadReader.GetDouble(7), // ORD_MOMS
                                OrdTyp = ordTyp, // ORD_TYP
                                OrdKod = ordrRadReader.IsDBNull(9) ? null : ordrRadReader.GetString(9), // ORD_KOD
                                OrdMatkod = ordrRadReader.IsDBNull(10) ? null : ordrRadReader.GetString(10), // ORD_MATKOD
                                OrdSummaexkl = ordrRadReader.IsDBNull(11) ? 0 : ordrRadReader.GetDouble(11), // ORD_SUMMAEXKL
                                OrdCreatedAt = ordrRadReader.IsDBNull(12) ? null : ordrRadReader.GetDateTime(12), // ORD_CREATED_AT
                                OrdUpdatedAt = ordrRadReader.IsDBNull(13) ? null : ordrRadReader.GetDateTime(13), // ORD_UPDATED_AT
                                MatchedKeyword = matchedKeyword,
                                MatchedCategory = matchedCategory
                            };
                            
                            ordrRadData[rowOrderNo].Add(ordrRad);
                        }
                        
                            _logger.LogDebug("Processed ORDRAD batch for {OrderCount} orders in {Environment}", batch.Count(), environment);
                        }
                        catch (Exception ordrRadEx)
                        {
                            _logger.LogError(ordrRadEx, "Failed to process ORDRAD batch in {Environment}", environment);
                        }
                    }
                    
                    var totalRowCount = ordrRadData.Values.Sum(rows => rows.Count);
                    
                    _logger.LogDebug("ORDRAD returned {RowCount} rows for {OrderCount} orders in {Environment}", 
                        totalRowCount, ordrRadData.Count, environment);
                    
                    // Add ORDRAD data to orders
                foreach (var order in results)
                {
                        if (ordrRadData.ContainsKey(order.OrhDokn))
                        {
                            order.OrderRows = ordrRadData[order.OrhDokn];
                            
                            // Check if order has any labor rows (ORD_TYP = "A" with non-zero quantity)
                            var hasLaborRows = order.OrderRows
                                .Any(row => row.OrdTyp == "A" && row.OrdAnta != 0);
                            
                            // Extract distinct categories from order rows
                            var categories = order.OrderRows
                                .Where(row => !string.IsNullOrEmpty(row.MatchedCategory))
                                .Select(row => row.MatchedCategory!)
                                .Distinct()
                                .OrderBy(cat => cat)
                        .ToList();
                    
                            // If order has labor rows but no categories found, default to "Reparation"
                            if (hasLaborRows && categories.Count == 0)
                            {
                                categories.Add("Reparation");
                            }
                    
                            order.Categories = categories;
                            
                            // Set BilVehiclecat to "Husbil" if it's null/empty and at least one order row has OrdMatkod = "HU"
                            if (order.Vehicle != null && string.IsNullOrEmpty(order.Vehicle.BilVehiclecat))
                            {
                                var hasHusbilRow = order.OrderRows.Any(row => row.OrdMatkod == "HU");
                                if (hasHusbilRow)
                                {
                                    order.Vehicle.BilVehiclecat = "Husbil";
                                }
                            }
                            
                            _logger.LogDebug("Added {RowCount} ORDRAD rows to order {OrderNo} in {Environment}", 
                                order.OrderRows.Count, order.OrhDokn, environment);
                        }
                    }
                    
                    _logger.LogDebug("Found ORDRAD data for {OrderCount} orders in {Environment}", ordrRadData.Count, environment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch order row data in {Environment}: {Error}", environment, ex.Message);
                }
            }
            
            // Count calculation is now done at the main method level across all environments
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query environment {Environment}: {Error}", environment, ex.Message);
            return new List<OrdhuvDto>(); // Return empty list instead of throwing to allow other environments to succeed
        }
    }

    /// <summary>
    /// Queries a single environment for orders by date with optional invoice filtering
    /// </summary>
    private async Task<List<OrdhuvDto>> QueryEnvironmentByDateAsync(string environment, DateTime fromDate, DateTime toDate, string? orhStat = null, string? customerType = null, bool? invoiced = null)
    {
        var results = new List<OrdhuvDto>();

        try
        {
            using var connection = new FbConnection(_databaseConfig.GetConnectionString(environment));
            await connection.OpenAsync();
            
            // Step 1: Get orders that match the date range with customer data
            var sqlQuery = $@"
                SELECT DISTINCT
                    o.ORH_DOKN,
                    o.ORH_KUNR,
                    o.ORH_DOKD,
                    o.ORH_RENR,
                    o.ORH_STAT,
                    o.ORH_LOVDAT,
                    o.ORH_FAKTURERAD,
                    o.ORH_NAMN,
                    o.ORH_SUMMAINKL,
                    o.ORH_SUMMAEXKL,
                    o.ORH_MILS,
                    o.ORH_CREATED_AT,
                    o.ORH_UPDATED_AT,
                    CASE WHEN o.ORH_BETKUNR > 0 THEN o.ORH_BETKUNR ELSE NULL END as ORH_BETKUNR,
                    CASE WHEN o.ORH_DRIVER_NO > 0 THEN o.ORH_DRIVER_NO ELSE NULL END as ORH_DRIVER_NO,
                    -- Customer data (ORH_KUNR)
                    c.KUN_KUNR as CUST_KUN_KUNR,
                    c.KUN_NAMN as CUST_KUN_NAMN,
                    c.KUN_ADR1 as CUST_KUN_ADR1,
                    c.KUN_ADR2 as CUST_KUN_ADR2,
                    c.KUN_PADR as CUST_KUN_PADR,
                    c.KUN_ORGN as CUST_KUN_ORGN,
                    c.KUN_TEL1 as CUST_KUN_TEL1,
                    c.KUN_TEL2 as CUST_KUN_TEL2,
                    c.KUN_TEL3 as CUST_KUN_TEL3,
                    c.KUN_EPOSTADRESS as CUST_KUN_EPOSTADRESS,
                    -- Payer data (ORH_BETKUNR)
                    p.KUN_KUNR as PAYER_KUN_KUNR,
                    p.KUN_NAMN as PAYER_KUN_NAMN,
                    p.KUN_ADR1 as PAYER_KUN_ADR1,
                    p.KUN_ADR2 as PAYER_KUN_ADR2,
                    p.KUN_PADR as PAYER_KUN_PADR,
                    p.KUN_ORGN as PAYER_KUN_ORGN,
                    p.KUN_TEL1 as PAYER_KUN_TEL1,
                    p.KUN_TEL2 as PAYER_KUN_TEL2,
                    p.KUN_TEL3 as PAYER_KUN_TEL3,
                    p.KUN_EPOSTADRESS as PAYER_KUN_EPOSTADRESS,
                    -- Driver data (ORH_DRIVER_NO)
                    d.KUN_KUNR as DRIVER_KUN_KUNR,
                    d.KUN_NAMN as DRIVER_KUN_NAMN,
                    d.KUN_ADR1 as DRIVER_KUN_ADR1,
                    d.KUN_ADR2 as DRIVER_KUN_ADR2,
                    d.KUN_PADR as DRIVER_KUN_PADR,
                    d.KUN_ORGN as DRIVER_KUN_ORGN,
                    d.KUN_TEL1 as DRIVER_KUN_TEL1,
                    d.KUN_TEL2 as DRIVER_KUN_TEL2,
                    d.KUN_TEL3 as DRIVER_KUN_TEL3,
                    d.KUN_EPOSTADRESS as DRIVER_KUN_EPOSTADRESS,
                    -- Vehicle data
                    b.BIL_RENR as VEHICLE_BIL_RENR,
                    b.BIL_BETECKNING as VEHICLE_BIL_BETECKNING,
                    b.BIL_ARSM as VEHICLE_BIL_ARSM,
                    b.FABRIKAT as VEHICLE_FABRIKAT,
                    b.BIL_VEHICLECAT as VEHICLE_BIL_VEHICLECAT,
                    b.BIL_FUEL as VEHICLE_BIL_FUEL
                FROM ORDHUV o
                {(invoiced == true ? 
                    "INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO\n                INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO" : 
                    "LEFT JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO\n                LEFT JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO")}
                LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR
                LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR
                LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR
                LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR
                WHERE {(invoiced == true ? "f.TIME_STAMP >= @fromDate AND f.TIME_STAMP <= @toDate" : "o.ORH_DOKD >= @fromDate AND o.ORH_DOKD <= @toDate")}
                  {(invoiced == true ? "AND f.KEY_NO IS NOT NULL AND f.KEY_NO != ''" : "")}
                  {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")}
                ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
                
            using var orderCommand = new FbCommand(sqlQuery, connection);
            
            orderCommand.Parameters.AddWithValue("@fromDate", fromDate);
            orderCommand.Parameters.AddWithValue("@toDate", toDate);
            
            if (!string.IsNullOrEmpty(orhStat))
            {
                orderCommand.Parameters.AddWithValue("@orhStat", orhStat);
            }

            // Log the SQL command (shortened version)
            var shortSql = $"SELECT DISTINCT o.ORH_DOKN, o.ORH_KUNR, o.ORH_DOKD, o.ORH_RENR, o.ORH_STAT, o.ORH_LOVDAT, o.ORH_FAKTURERAD, o.ORH_NAMN, o.ORH_SUMMAINKL, o.ORH_CREATED_AT, o.ORH_UPDATED_AT, [Customer/Payer/Driver/Vehicle data] FROM ORDHUV o {(invoiced == true ? "INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO" : "LEFT JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO LEFT JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO")} LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR LEFT JOIN BILREG b ON o.ORH_RENR = b.BIL_RENR WHERE {(invoiced == true ? "f.TIME_STAMP >= @fromDate AND f.TIME_STAMP <= @toDate" : "o.ORH_DOKD >= @fromDate AND o.ORH_DOKD <= @toDate")} {(invoiced == true ? "AND f.KEY_NO IS NOT NULL AND f.KEY_NO != ''" : "")} {(!string.IsNullOrEmpty(orhStat) ? "AND o.ORH_STAT = @orhStat" : "")} ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
            _logger.LogInformation("Executing date-based SQL on {Environment}: {SqlQuery}", environment, shortSql);
            _logger.LogInformation("Date-based SQL Parameters: fromDate={FromDate}, toDate={ToDate}, orhStat={OrhStat}, invoiced={Invoiced}", 
                fromDate, toDate, orhStat, invoiced);

            // Get all distinct orders
            using var orderReader = await orderCommand.ExecuteReaderAsync();
            var orderNumbers = new List<int>();
            
            while (await orderReader.ReadAsync())
            {
                var order = new OrdhuvDto
                {
                    Database = environment, // Add database identifier
                    OrhDokn = orderReader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = orderReader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = orderReader.IsDBNull(2) ? null : orderReader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = orderReader.IsDBNull(3) ? null : orderReader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = orderReader.IsDBNull(4) ? null : orderReader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = orderReader.IsDBNull(5) ? null : orderReader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = orderReader.IsDBNull(6) ? null : orderReader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = orderReader.IsDBNull(7) ? null : orderReader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = orderReader.IsDBNull(8) ? null : orderReader.GetDouble(8), // ORH_SUMMAINKL - Sum Including
                    OrhSummaexkl = orderReader.IsDBNull(9) ? null : orderReader.GetDouble(9), // ORH_SUMMAEXKL - Sum Excluding
                    OrhMils = orderReader.IsDBNull(10) ? null : orderReader.GetInt32(10), // ORH_MILS - Odometer Reading
                    OrhCreatedAt = orderReader.IsDBNull(11) ? null : orderReader.GetDateTime(11), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = orderReader.IsDBNull(12) ? null : orderReader.GetDateTime(12), // ORH_UPDATED_AT - Updated At
                    OrhBetkunr = orderReader.IsDBNull(13) ? null : orderReader.GetInt32(13), // ORH_BETKUNR - Payer Number
                    OrhDriverNo = orderReader.IsDBNull(14) ? null : orderReader.GetInt32(14), // ORH_DRIVER_NO - Driver Number
                    Invoices = new List<InvoiceIndividualDto>(),
                    OrderRows = new List<OrdrRadDto>(),
                    
                    // Customer data (ORH_KUNR)
                    Customer = orderReader.IsDBNull(15) ? null : CreateKunregDto(orderReader, 15, 19),
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(25) ? null : CreateKunregDto(orderReader, 25, 29),
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(35) ? null : CreateKunregDto(orderReader, 35, 39),
                    
                    // Vehicle data (ORH_RENR -> BILREG.BIL_RENR)
                    Vehicle = orderReader.IsDBNull(45) ? null : CreateBilregDto(orderReader, 45)
                };
                
                results.Add(order);
                orderNumbers.Add(order.OrhDokn);
            }
            
            _logger.LogDebug("Found {OrderCount} orders in {Environment}", orderNumbers.Count, environment);

            // Step 2: Get all invoices for these orders - only if invoiced=true or null (default to invoiced)
            if (orderNumbers.Any() && (invoiced == true || invoiced == null))
            {
                // Process invoices in batches to avoid Firebird's 1500 value limit
                var invoiceData = new Dictionary<int, InvoiceIndividualDto>();
                var batchSize = 1000; // Safe batch size for Firebird
                var orderNumberBatches = orderNumbers.Chunk(batchSize);
                var totalInvoiceCount = 0;
                
                foreach (var batch in orderNumberBatches)
                {
                    var orderNumbersParam = string.Join(",", batch);
                    using var invoiceCommand = new FbCommand($@"
                        SELECT 
                            i.VEHICLE_NO,
                            i.MANUFACTURER,
                            i.MODEL,
                            i.VIN,
                            i.REGISTRATION_DATE,
                            i.MODEL_YEAR,
                            CASE WHEN i.OWNER_NO > 0 THEN i.OWNER_NO ELSE NULL END as OWNER_NO,
                            i.OWNER_NAME,
                            CASE WHEN i.OWNER_ADRESS_2 IS NOT NULL AND i.OWNER_ADRESS_2 != '' THEN i.OWNER_ADRESS_2 ELSE NULL END as OWNER_ADRESS_2,
                            CASE WHEN i.OWNER_ZIP_AND_CITY IS NOT NULL AND i.OWNER_ZIP_AND_CITY != '' THEN i.OWNER_ZIP_AND_CITY ELSE NULL END as OWNER_ZIP_AND_CITY,
                            CASE WHEN i.OWNER_PHONE IS NOT NULL AND i.OWNER_PHONE != '' THEN i.OWNER_PHONE ELSE NULL END as OWNER_PHONE,
                            CASE WHEN i.OWNER_MAIL IS NOT NULL AND i.OWNER_MAIL != '' THEN i.OWNER_MAIL ELSE NULL END as OWNER_MAIL,
                            CASE WHEN i.PAYER_NO > 0 THEN i.PAYER_NO ELSE NULL END as PAYER_NO,
                            i.PAYER_NAME,
                            CASE WHEN i.PAYER_ADRESS_2 IS NOT NULL AND i.PAYER_ADRESS_2 != '' THEN i.PAYER_ADRESS_2 ELSE NULL END as PAYER_ADRESS_2,
                            CASE WHEN i.PAYER_ZIP_AND_CITY IS NOT NULL AND i.PAYER_ZIP_AND_CITY != '' THEN i.PAYER_ZIP_AND_CITY ELSE NULL END as PAYER_ZIP_AND_CITY,
                            CASE WHEN i.PAYER_PHONE IS NOT NULL AND i.PAYER_PHONE != '' THEN i.PAYER_PHONE ELSE NULL END as PAYER_PHONE,
                            CASE WHEN i.PAYER_MAIL IS NOT NULL AND i.PAYER_MAIL != '' THEN i.PAYER_MAIL ELSE NULL END as PAYER_MAIL,
                            CASE WHEN i.PAYER_VATNO IS NOT NULL AND i.PAYER_VATNO != '' THEN i.PAYER_VATNO ELSE NULL END as PAYER_VATNO,
                            CASE WHEN i.DRIVER_NO > 0 THEN i.DRIVER_NO ELSE NULL END as DRIVER_NO,
                            i.DRIVER_NAME,
                            CASE WHEN i.DRIVER_ADRESS_2 IS NOT NULL AND i.DRIVER_ADRESS_2 != '' THEN i.DRIVER_ADRESS_2 ELSE NULL END as DRIVER_ADRESS_2,
                            CASE WHEN i.DRIVER_ZIP_AND_CITY IS NOT NULL AND i.DRIVER_ZIP_AND_CITY != '' THEN i.DRIVER_ZIP_AND_CITY ELSE NULL END as DRIVER_ZIP_AND_CITY,
                            CASE WHEN i.DRIVER_PHONE IS NOT NULL AND i.DRIVER_PHONE != '' THEN i.DRIVER_PHONE ELSE NULL END as DRIVER_PHONE,
                            CASE WHEN i.DRIVER_MAIL IS NOT NULL AND i.DRIVER_MAIL != '' THEN i.DRIVER_MAIL ELSE NULL END as DRIVER_MAIL,
                            i.INVOICE_NO,
                            f.ID,
                            f.TIME_STAMP,
                            f.TRANSACTION_NO,
                            f.DESCRIPTION,
                            CASE WHEN f.ERROR_CODE IS NOT NULL AND f.ERROR_CODE != '' THEN f.ERROR_CODE ELSE NULL END as ERROR_CODE,
                            CASE WHEN f.ERROR_MESSAGE IS NOT NULL AND f.ERROR_MESSAGE != '' THEN f.ERROR_MESSAGE ELSE NULL END as ERROR_MESSAGE,
                            f.LOG_TYPE,
                            f.KEY_NO
                        FROM INVOICEINDIVIDUAL i
                        INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                        WHERE i.INVOICE_NO IN ({orderNumbersParam})
                          AND f.KEY_NO IS NOT NULL 
                          AND f.KEY_NO != ''
                        ORDER BY i.INVOICE_NO, f.TIME_STAMP", connection);

                    // Log the invoice SQL command (shortened version)
                    var shortInvoiceSql = $"SELECT i.VEHICLE_NO, i.MANUFACTURER, i.MODEL, i.VIN, i.REGISTRATION_DATE, i.MODEL_YEAR, [Owner/Payer/Driver data], i.INVOICE_NO, f.ID, f.TIME_STAMP, f.TRANSACTION_NO, f.DESCRIPTION, f.ERROR_CODE, f.ERROR_MESSAGE, f.LOG_TYPE, f.KEY_NO FROM INVOICEINDIVIDUAL i INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO WHERE i.INVOICE_NO IN ({orderNumbersParam}) AND f.KEY_NO IS NOT NULL AND f.KEY_NO != '' ORDER BY i.INVOICE_NO, f.TIME_STAMP";
                    _logger.LogInformation("Executing Invoice SQL batch on {Environment}: {SqlQuery}", environment, shortInvoiceSql);
                    _logger.LogInformation("Invoice SQL Parameters: orderNumbers={OrderNumbers} (batch size: {BatchSize})", orderNumbersParam, batch.Count());

                    using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                    var batchInvoiceCount = 0;
                    
                    while (await invoiceReader.ReadAsync())
                    {
                        var currentInvoiceNo = invoiceReader.GetInt32(25); // INVOICE_NO
                        batchInvoiceCount++;
                        totalInvoiceCount++;
                        
                        if (!invoiceData.ContainsKey(currentInvoiceNo))
                        {
                            invoiceData[currentInvoiceNo] = new InvoiceIndividualDto
                            {
                                VehicleNo = invoiceReader.IsDBNull(0) ? null : invoiceReader.GetString(0), // VEHICLE_NO
                                Manufacturer = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1), // MANUFACTURER
                                Model = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2), // MODEL
                                Vin = invoiceReader.IsDBNull(3) ? null : invoiceReader.GetString(3), // VIN
                                RegistrationDate = invoiceReader.IsDBNull(4) ? null : invoiceReader.GetDateTime(4), // REGISTRATION_DATE
                                ModelYear = invoiceReader.IsDBNull(5) ? null : (short)invoiceReader.GetInt32(5), // MODEL_YEAR
                                OwnerNo = invoiceReader.IsDBNull(6) ? null : invoiceReader.GetInt32(6), // OWNER_NO
                                OwnerName = invoiceReader.IsDBNull(7) ? null : invoiceReader.GetString(7), // OWNER_NAME
                                OwnerAddress2 = invoiceReader.IsDBNull(8) ? null : invoiceReader.GetString(8), // OWNER_ADRESS_2
                                OwnerZipAndCity = invoiceReader.IsDBNull(9) ? null : invoiceReader.GetString(9), // OWNER_ZIP_AND_CITY
                                OwnerPhone = invoiceReader.IsDBNull(10) ? null : invoiceReader.GetString(10), // OWNER_PHONE
                                OwnerMail = invoiceReader.IsDBNull(11) ? null : invoiceReader.GetString(11), // OWNER_MAIL
                                PayerNo = invoiceReader.IsDBNull(12) ? null : invoiceReader.GetInt32(12), // PAYER_NO
                                PayerName = invoiceReader.IsDBNull(13) ? null : invoiceReader.GetString(13), // PAYER_NAME
                                PayerAddress2 = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetString(14), // PAYER_ADRESS_2
                                PayerZipAndCity = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetString(15), // PAYER_ZIP_AND_CITY
                                PayerPhone = invoiceReader.IsDBNull(16) ? null : invoiceReader.GetString(16), // PAYER_PHONE
                                PayerMail = invoiceReader.IsDBNull(17) ? null : invoiceReader.GetString(17), // PAYER_MAIL
                                PayerVatNo = invoiceReader.IsDBNull(18) ? null : invoiceReader.GetString(18), // PAYER_VATNO
                                DriverNo = invoiceReader.IsDBNull(19) ? null : invoiceReader.GetInt32(19), // DRIVER_NO
                                DriverName = invoiceReader.IsDBNull(20) ? null : invoiceReader.GetString(20), // DRIVER_NAME
                                DriverAddress2 = invoiceReader.IsDBNull(21) ? null : invoiceReader.GetString(21), // DRIVER_ADRESS_2
                                DriverZipAndCity = invoiceReader.IsDBNull(22) ? null : invoiceReader.GetString(22), // DRIVER_ZIP_AND_CITY
                                DriverPhone = invoiceReader.IsDBNull(23) ? null : invoiceReader.GetString(23), // DRIVER_PHONE
                                DriverMail = invoiceReader.IsDBNull(24) ? null : invoiceReader.GetString(24), // DRIVER_MAIL
                                InvoiceNo = currentInvoiceNo,
                                
                                // Initialize Fortnox Logs array
                                FortnoxLogs = new List<FortnoxLogDto>()
                            };
                        }
                        
                        // Add Fortnox log to the invoice
                        var fortnoxLog = new FortnoxLogDto
                        {
                            Id = invoiceReader.GetInt32(26),                                           // FORTNOX_LOG.ID
                            TimeStamp = invoiceReader.IsDBNull(27) ? null : invoiceReader.GetDateTime(27), // FORTNOX_LOG.TIME_STAMP
                            TransactionNo = invoiceReader.IsDBNull(28) ? null : invoiceReader.GetInt32(28), // FORTNOX_LOG.TRANSACTION_NO
                            Description = invoiceReader.IsDBNull(29) ? null : invoiceReader.GetString(29), // FORTNOX_LOG.DESCRIPTION
                            ErrorCode = invoiceReader.IsDBNull(30) ? null : invoiceReader.GetString(30), // FORTNOX_LOG.ERROR_CODE
                            ErrorMessage = invoiceReader.IsDBNull(31) ? null : invoiceReader.GetString(31), // FORTNOX_LOG.ERROR_MESSAGE
                            LogType = invoiceReader.IsDBNull(32) ? null : invoiceReader.GetInt32(32), // FORTNOX_LOG.LOG_TYPE
                            KeyNo = invoiceReader.IsDBNull(33) ? null : invoiceReader.GetString(33)     // FORTNOX_LOG.KEY_NO
                        };
                        
                        invoiceData[currentInvoiceNo].FortnoxLogs.Add(fortnoxLog);
                    }
                    
                    _logger.LogDebug("Processed {InvoiceCount} invoices in batch for {Environment}", batchInvoiceCount, environment);
                }
                
                _logger.LogInformation("Total invoices processed for {Environment}: {TotalCount}", environment, totalInvoiceCount);

                // Step 3: Get order rows for these orders
                var orderRowData = new Dictionary<int, List<OrdrRadDto>>();
                foreach (var batch in orderNumberBatches)
                {
                    var orderNumbersParam = string.Join(",", batch);
                    using var orderRowCommand = new FbCommand($@"
                        SELECT 
                            ORD_DOKN,
                            ORD_RADNR,
                            ORD_ANTAL,
                            ORD_PRIS,
                            ORD_SUMMA,
                            ORD_MATKOD,
                            ORD_BETECKNING,
                            ORD_KATEGORI,
                            ORD_FAKTURERAD,
                            ORD_CREATED_AT,
                            ORD_UPDATED_AT
                        FROM ORDRAD
                        WHERE ORD_DOKN IN ({orderNumbersParam})
                        ORDER BY ORD_DOKN, ORD_RADNR", connection);

                    using var orderRowReader = await orderRowCommand.ExecuteReaderAsync();
                    
                    while (await orderRowReader.ReadAsync())
                    {
                        var orderNo = orderRowReader.GetInt32(0); // ORD_DOKN
                        var artbText = orderRowReader.IsDBNull(3) ? null : orderRowReader.GetString(3); // ORD_ARTB
                        var ordAnta = orderRowReader.IsDBNull(4) ? 0 : orderRowReader.GetDouble(4); // ORD_ANTA
                        var ordTyp = orderRowReader.IsDBNull(8) ? null : orderRowReader.GetString(8); // ORD_TYP
                        
                        // Only match categories for labor rows (ORD_TYP = "A") with non-zero quantity
                        var (matchedKeyword, matchedCategory) = (ordTyp == "A" && ordAnta != 0) 
                            ? MatchKeywords(artbText) 
                            : (null, null);
                        
                        if (!orderRowData.ContainsKey(orderNo))
                        {
                            orderRowData[orderNo] = new List<OrdrRadDto>();
                        }
                        
                        var orderRow = new OrdrRadDto
                        {
                            OrdDokn = orderNo,                                           // ORD_DOKN
                            OrdRadnr = orderRowReader.IsDBNull(1) ? 0 : (int)orderRowReader.GetDouble(1), // ORD_RADNR
                            OrdArtn = orderRowReader.IsDBNull(2) ? null : orderRowReader.GetString(2), // ORD_ARTN
                            OrdArtb = artbText, // ORD_ARTB
                            OrdAnta = ordAnta, // ORD_ANTA
                            OrdInpris = orderRowReader.IsDBNull(5) ? 0 : orderRowReader.GetDouble(5), // ORD_INPRIS
                            OrdRaba = orderRowReader.IsDBNull(6) ? 0 : orderRowReader.GetDouble(6), // ORD_RABA
                            OrdMoms = orderRowReader.IsDBNull(7) ? 0 : orderRowReader.GetDouble(7), // ORD_MOMS
                            OrdTyp = ordTyp, // ORD_TYP
                            OrdKod = orderRowReader.IsDBNull(9) ? null : orderRowReader.GetString(9), // ORD_KOD
                            OrdMatkod = orderRowReader.IsDBNull(10) ? null : orderRowReader.GetString(10), // ORD_MATKOD
                            OrdSummaexkl = orderRowReader.IsDBNull(11) ? 0 : orderRowReader.GetDouble(11), // ORD_SUMMAEXKL
                            OrdCreatedAt = orderRowReader.IsDBNull(12) ? null : orderRowReader.GetDateTime(12), // ORD_CREATED_AT
                            OrdUpdatedAt = orderRowReader.IsDBNull(13) ? null : orderRowReader.GetDateTime(13), // ORD_UPDATED_AT
                            MatchedKeyword = matchedKeyword,
                            MatchedCategory = matchedCategory
                        };
                        
                        orderRowData[orderNo].Add(orderRow);
                    }
                }

                // Step 4: Associate invoices and order rows with orders
                foreach (var order in results)
                {
                    if (invoiceData.ContainsKey(order.OrhDokn))
                    {
                        order.Invoices.Add(invoiceData[order.OrhDokn]);
                    }
                    
                    if (orderRowData.ContainsKey(order.OrhDokn))
                    {
                        order.OrderRows = orderRowData[order.OrhDokn];
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query environment {Environment}: {Error}", environment, ex.Message);
            return new List<OrdhuvDto>(); // Return empty list instead of throwing to allow other environments to succeed
        }
    }
}
