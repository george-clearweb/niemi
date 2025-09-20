namespace Niemi.Services;

using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niemi.Models;

public class OrdhuvService : IOrdhuvService
{
    private readonly string _connectionString;
    private readonly ILogger<OrdhuvService> _logger;

    public OrdhuvService(IConfiguration configuration, ILogger<OrdhuvService> logger)
    {
        _connectionString = configuration.GetConnectionString("FirebirdConnection") 
            ?? throw new ArgumentNullException("FirebirdConnection string is missing");
        _logger = logger;
    }

    public async Task<IEnumerable<Ordhuv>> GetOrdhuvDataAsync(int skip, int take)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<Ordhuv>();

        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand($@"
                SELECT FIRST {take} SKIP {skip}
                    ORH_DOKN,
                    ORH_KUNR,
                    ORH_DOKD,
                    ORH_RENR,
                    ORH_STAT,
                    ORH_LOVDAT,
                    ORH_FAKTURERAD,
                    ORH_NAMN,
                    ORH_SUMMAINKL,
                    ORH_CREATED_AT,
                    ORH_UPDATED_AT
                FROM ORDHUV
                ORDER BY ORH_DOKN DESC", connection);

            _logger.LogInformation("Executing ORDHUV query with skip: {Skip}, take: {Take}", skip, take);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new Ordhuv
                {
                    OrhDokn = reader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = reader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = reader.IsDBNull(2) ? null : reader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = reader.IsDBNull(3) ? null : reader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = reader.IsDBNull(4) ? null : reader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = reader.IsDBNull(5) ? null : reader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = reader.IsDBNull(6) ? null : reader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = reader.IsDBNull(7) ? null : reader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = reader.GetDouble(8),              // ORH_SUMMAINKL - Sum Including
                    OrhCreatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) // ORH_UPDATED_AT - Updated At
                });
            }

            sw.Stop();
            _logger.LogInformation("ORDHUV query completed in {ElapsedMs}ms. Found {Count} orders", 
                sw.ElapsedMilliseconds, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ORDHUV query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<Ordhuv?> GetOrdhuvByIdAsync(int orderNr)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand(@"
                SELECT 
                    ORH_DOKN,
                    ORH_KUNR,
                    ORH_DOKD,
                    ORH_RENR,
                    ORH_STAT,
                    ORH_LOVDAT,
                    ORH_FAKTURERAD,
                    ORH_NAMN,
                    ORH_SUMMAINKL,
                    ORH_CREATED_AT,
                    ORH_UPDATED_AT
                FROM ORDHUV
                WHERE ORH_DOKN = @orderNr", connection);

            command.Parameters.AddWithValue("@orderNr", orderNr);

            _logger.LogInformation("Executing ORDHUV by ID query for OrderNr: {OrderNr}", orderNr);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                sw.Stop();
                _logger.LogInformation("ORDHUV by ID query completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
                
                return new Ordhuv
                {
                    OrhDokn = reader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = reader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = reader.IsDBNull(2) ? null : reader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = reader.IsDBNull(3) ? null : reader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = reader.IsDBNull(4) ? null : reader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = reader.IsDBNull(5) ? null : reader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = reader.IsDBNull(6) ? null : reader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = reader.IsDBNull(7) ? null : reader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = reader.GetDouble(8),              // ORH_SUMMAINKL - Sum Including
                    OrhCreatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) // ORH_UPDATED_AT - Updated At
                };
            }

            sw.Stop();
            _logger.LogInformation("ORDHUV not found. Query completed in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "ORDHUV by ID query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<Ordhuv>> GetInvoicedOrdersByDateAsync(DateTime fromDate, DateTime toDate, int skip, int take)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<Ordhuv>();

        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand($@"
                SELECT FIRST {take} SKIP {skip}
                    ORH_DOKN,
                    ORH_KUNR,
                    ORH_DOKD,
                    ORH_RENR,
                    ORH_STAT,
                    ORH_LOVDAT,
                    ORH_FAKTURERAD,
                    ORH_NAMN,
                    ORH_SUMMAINKL,
                    ORH_CREATED_AT,
                    ORH_UPDATED_AT
                FROM ORDHUV
                WHERE ORH_DOKD >= @fromDate 
                  AND ORH_DOKD <= @toDate
                ORDER BY ORH_DOKD DESC, ORH_DOKN DESC", connection);

            command.Parameters.AddWithValue("@fromDate", fromDate);
            command.Parameters.AddWithValue("@toDate", toDate);

            _logger.LogInformation("Executing invoiced ORDHUV query with skip: {Skip}, take: {Take}, fromDate: {FromDate}, toDate: {ToDate}", 
                skip, take, fromDate, toDate);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new Ordhuv
                {
                    OrhDokn = reader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = reader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = reader.IsDBNull(2) ? null : reader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = reader.IsDBNull(3) ? null : reader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = reader.IsDBNull(4) ? null : reader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = reader.IsDBNull(5) ? null : reader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = reader.IsDBNull(6) ? null : reader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = reader.IsDBNull(7) ? null : reader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = reader.GetDouble(8),              // ORH_SUMMAINKL - Sum Including
                    OrhCreatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10) // ORH_UPDATED_AT - Updated At
                });
            }

            sw.Stop();
            _logger.LogInformation("Invoiced ORDHUV query completed in {ElapsedMs}ms. Found {Count} invoiced orders", 
                sw.ElapsedMilliseconds, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Invoiced ORDHUV query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<Ordhuv>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<Ordhuv>();

        try
        {
            using var connection = new FbConnection(_connectionString);
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
                    o.ORH_CREATED_AT,
                    o.ORH_UPDATED_AT,
                    o.ORH_BETKUNR,
                    o.ORH_DRIVER_NO,
                    -- Customer data (ORH_KUNR)
                    c.KUN_KUNR as CUST_KUN_KUNR,
                    c.KUN_NAMN as CUST_KUN_NAMN,
                    c.KUN_ADR1 as CUST_KUN_ADR1,
                    c.KUN_ADR2 as CUST_KUN_ADR2,
                    c.KUN_TEL1 as CUST_KUN_TEL1,
                    c.KUN_TEL2 as CUST_KUN_TEL2,
                    c.KUN_EPOSTADRESS as CUST_KUN_EPOSTADRESS,
                    c.KUN_ORGN as CUST_KUN_ORGN,
                    c.KUN_MOMSNR as CUST_KUN_MOMSNR,
                    -- Payer data (ORH_BETKUNR)
                    p.KUN_KUNR as PAYER_KUN_KUNR,
                    p.KUN_NAMN as PAYER_KUN_NAMN,
                    p.KUN_ADR1 as PAYER_KUN_ADR1,
                    p.KUN_ADR2 as PAYER_KUN_ADR2,
                    p.KUN_TEL1 as PAYER_KUN_TEL1,
                    p.KUN_TEL2 as PAYER_KUN_TEL2,
                    p.KUN_EPOSTADRESS as PAYER_KUN_EPOSTADRESS,
                    p.KUN_ORGN as PAYER_KUN_ORGN,
                    p.KUN_MOMSNR as PAYER_KUN_MOMSNR,
                    -- Driver data (ORH_DRIVER_NO)
                    d.KUN_KUNR as DRIVER_KUN_KUNR,
                    d.KUN_NAMN as DRIVER_KUN_NAMN,
                    d.KUN_ADR1 as DRIVER_KUN_ADR1,
                    d.KUN_ADR2 as DRIVER_KUN_ADR2,
                    d.KUN_TEL1 as DRIVER_KUN_TEL1,
                    d.KUN_TEL2 as DRIVER_KUN_TEL2,
                    d.KUN_EPOSTADRESS as DRIVER_KUN_EPOSTADRESS,
                    d.KUN_ORGN as DRIVER_KUN_ORGN,
                    d.KUN_MOMSNR as DRIVER_KUN_MOMSNR
                FROM ORDHUV o
                INNER JOIN INVOICEINDIVIDUAL i ON o.ORH_DOKN = i.INVOICE_NO
                INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                LEFT JOIN KUNREG c ON o.ORH_KUNR = c.KUN_KUNR
                LEFT JOIN KUNREG p ON o.ORH_BETKUNR = p.KUN_KUNR
                LEFT JOIN KUNREG d ON o.ORH_DRIVER_NO = d.KUN_KUNR
                WHERE f.TIME_STAMP >= @fromDate 
                  AND f.TIME_STAMP <= @toDate
                  AND f.KEY_NO IS NOT NULL 
                  AND f.KEY_NO != ''
                ORDER BY o.ORH_DOKD DESC, o.ORH_DOKN DESC";
                
            using var orderCommand = new FbCommand(sqlQuery, connection);

            orderCommand.Parameters.AddWithValue("@fromDate", fromDate);
            orderCommand.Parameters.AddWithValue("@toDate", toDate);
                
            _logger.LogInformation("Executing orders with invoices query with fromDate: {FromDate}, toDate: {ToDate}", 
                fromDate, toDate);
            _logger.LogInformation("SQL Query: {SqlQuery}", sqlQuery);

            // Get all distinct orders
            using var orderReader = await orderCommand.ExecuteReaderAsync();
            var orderNumbers = new List<int>();
            
            while (await orderReader.ReadAsync())
            {
                var order = new Ordhuv
                {
                    OrhDokn = orderReader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = orderReader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = orderReader.IsDBNull(2) ? null : orderReader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = orderReader.IsDBNull(3) ? null : orderReader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = orderReader.IsDBNull(4) ? null : orderReader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = orderReader.IsDBNull(5) ? null : orderReader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = orderReader.IsDBNull(6) ? null : orderReader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = orderReader.IsDBNull(7) ? null : orderReader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = orderReader.GetDouble(8),              // ORH_SUMMAINKL - Sum Including
                    OrhCreatedAt = orderReader.IsDBNull(9) ? null : orderReader.GetDateTime(9), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = orderReader.IsDBNull(10) ? null : orderReader.GetDateTime(10), // ORH_UPDATED_AT - Updated At
                    OrhBetkunr = orderReader.IsDBNull(11) ? 0 : orderReader.GetInt32(11), // ORH_BETKUNR - Payer Number
                    OrhDriverNo = orderReader.IsDBNull(12) ? 0 : orderReader.GetInt32(12), // ORH_DRIVER_NO - Driver Number
                    Invoices = new List<InvoiceIndividual>(),
                    
                    // Customer data (ORH_KUNR)
                    Customer = orderReader.IsDBNull(13) ? null : new Kunreg
                    {
                        KunKunr = orderReader.GetInt32(13),               // CUST_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(14) ? null : orderReader.GetString(14), // CUST_KUN_NAMN
                        KunAdr1 = orderReader.IsDBNull(15) ? null : orderReader.GetString(15), // CUST_KUN_ADR1
                        KunAdr2 = orderReader.IsDBNull(16) ? null : orderReader.GetString(16), // CUST_KUN_ADR2
                        KunTel1 = orderReader.IsDBNull(17) ? null : orderReader.GetString(17), // CUST_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(18) ? null : orderReader.GetString(18), // CUST_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(19) ? null : orderReader.GetString(19), // CUST_KUN_EPOSTADRESS
                        KunOrgn = orderReader.IsDBNull(20) ? null : orderReader.GetString(20), // CUST_KUN_ORGN
                        KunMomsnr = orderReader.IsDBNull(21) ? null : orderReader.GetString(21) // CUST_KUN_MOMSNR
                    },
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(22) ? null : new Kunreg
                    {
                        KunKunr = orderReader.GetInt32(22),               // PAYER_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(23) ? null : orderReader.GetString(23), // PAYER_KUN_NAMN
                        KunAdr1 = orderReader.IsDBNull(24) ? null : orderReader.GetString(24), // PAYER_KUN_ADR1
                        KunAdr2 = orderReader.IsDBNull(25) ? null : orderReader.GetString(25), // PAYER_KUN_ADR2
                        KunTel1 = orderReader.IsDBNull(26) ? null : orderReader.GetString(26), // PAYER_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(27) ? null : orderReader.GetString(27), // PAYER_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(28) ? null : orderReader.GetString(28), // PAYER_KUN_EPOSTADRESS
                        KunOrgn = orderReader.IsDBNull(29) ? null : orderReader.GetString(29), // PAYER_KUN_ORGN
                        KunMomsnr = orderReader.IsDBNull(30) ? null : orderReader.GetString(30) // PAYER_KUN_MOMSNR
                    },
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(31) ? null : new Kunreg
                    {
                        KunKunr = orderReader.GetInt32(31),               // DRIVER_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(32) ? null : orderReader.GetString(32), // DRIVER_KUN_NAMN
                        KunAdr1 = orderReader.IsDBNull(33) ? null : orderReader.GetString(33), // DRIVER_KUN_ADR1
                        KunAdr2 = orderReader.IsDBNull(34) ? null : orderReader.GetString(34), // DRIVER_KUN_ADR2
                        KunTel1 = orderReader.IsDBNull(35) ? null : orderReader.GetString(35), // DRIVER_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(36) ? null : orderReader.GetString(36), // DRIVER_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(37) ? null : orderReader.GetString(37), // DRIVER_KUN_EPOSTADRESS
                        KunOrgn = orderReader.IsDBNull(38) ? null : orderReader.GetString(38), // DRIVER_KUN_ORGN
                        KunMomsnr = orderReader.IsDBNull(39) ? null : orderReader.GetString(39) // DRIVER_KUN_MOMSNR
                    }
                };
                
                results.Add(order);
                orderNumbers.Add(order.OrhDokn);
            }
            
            _logger.LogInformation("Found {OrderCount} unique orders: {OrderNumbers}", orderNumbers.Count, string.Join(", ", orderNumbers.Take(10)));
            _logger.LogInformation("Created {ResultCount} order objects in results list", results.Count);
            orderReader.Close();

            // Step 2: Get all invoices for these orders
            if (orderNumbers.Any())
            {
                var orderNumbersParam = string.Join(",", orderNumbers);
                using var invoiceCommand = new FbCommand($@"
                    SELECT 
                        i.VEHICLE_NO,
                        i.MANUFACTURER,
                        i.MODEL,
                        i.VIN,
                        i.REGISTRATION_DATE,
                        i.MODEL_YEAR,
                        i.OWNER_NO,
                        i.OWNER_NAME,
                        i.OWNER_ADRESS_1,
                        i.OWNER_ADRESS_2,
                        i.OWNER_ZIP_AND_CITY,
                        i.OWNER_PHONE,
                        i.OWNER_MAIL,
                        i.PAYER_NO,
                        i.PAYER_NAME,
                        i.PAYER_ADRESS_1,
                        i.PAYER_ADRESS_2,
                        i.PAYER_ZIP_AND_CITY,
                        i.PAYER_PHONE,
                        i.PAYER_MAIL,
                        i.PAYER_VATNO,
                        i.DRIVER_NO,
                        i.DRIVER_NAME,
                        i.DRIVER_ADRESS_1,
                        i.DRIVER_ADRESS_2,
                        i.DRIVER_ZIP_AND_CITY,
                        i.DRIVER_PHONE,
                        i.DRIVER_MAIL,
                        i.INVOICE_NO,
                        f.ID,
                        f.TIME_STAMP,
                        f.TRANSACTION_NO,
                        f.DESCRIPTION,
                        f.ERROR_CODE,
                        f.ERROR_MESSAGE,
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

                _logger.LogInformation("Executing invoices query for {OrderCount} orders", orderNumbers.Count);
                _logger.LogInformation("Invoice SQL Query: {InvoiceSqlQuery}", $@"
                    SELECT 
                        i.VEHICLE_NO, i.MANUFACTURER, i.MODEL, i.VIN, i.REGISTRATION_DATE, i.MODEL_YEAR,
                        i.OWNER_NO, i.OWNER_NAME, i.OWNER_ADRESS_1, i.OWNER_ADRESS_2, i.OWNER_ZIP_AND_CITY, i.OWNER_PHONE, i.OWNER_MAIL,
                        i.PAYER_NO, i.PAYER_NAME, i.PAYER_ADRESS_1, i.PAYER_ADRESS_2, i.PAYER_ZIP_AND_CITY, i.PAYER_PHONE, i.PAYER_MAIL, i.PAYER_VATNO,
                        i.DRIVER_NO, i.DRIVER_NAME, i.DRIVER_ADRESS_1, i.DRIVER_ADRESS_2, i.DRIVER_ZIP_AND_CITY, i.DRIVER_PHONE, i.DRIVER_MAIL,
                        i.INVOICE_NO,
                        f.ID, f.TIME_STAMP, f.TRANSACTION_NO, f.DESCRIPTION, f.ERROR_CODE, f.ERROR_MESSAGE, f.LOG_TYPE, f.KEY_NO
                    FROM INVOICEINDIVIDUAL i
                    INNER JOIN FORTNOX_LOG f ON CAST(i.INVOICE_NO AS VARCHAR(50)) = f.KEY_NO
                    WHERE i.INVOICE_NO IN ({orderNumbersParam})
                      AND f.KEY_NO IS NOT NULL 
                      AND f.KEY_NO != ''
                      AND f.TIME_STAMP >= @fromDate 
                      AND f.TIME_STAMP <= @toDate
                    ORDER BY i.INVOICE_NO, f.TIME_STAMP");

                using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                var invoiceCount = 0;
                var invoiceData = new Dictionary<int, InvoiceIndividual>();
                
                while (await invoiceReader.ReadAsync())
                {
                    invoiceCount++;
                    var currentInvoiceNo = invoiceReader.GetInt32(28); // INVOICE_NO is at index 28
                    _logger.LogInformation("Processing invoice {InvoiceNo} (count: {Count})", currentInvoiceNo, invoiceCount);
                    
                    // Create or get existing invoice
                    if (!invoiceData.ContainsKey(currentInvoiceNo))
                    {
                        invoiceData[currentInvoiceNo] = new InvoiceIndividual
                        {
                            // Invoice Individual Data
                            VehicleNo = invoiceReader.IsDBNull(0) ? null : invoiceReader.GetString(0),           // VEHICLE_NO
                            Manufacturer = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1),       // MANUFACTURER
                            Model = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2),              // MODEL
                            Vin = invoiceReader.IsDBNull(3) ? null : invoiceReader.GetString(3),                // VIN
                            RegistrationDate = invoiceReader.IsDBNull(4) ? null : invoiceReader.GetDateTime(4), // REGISTRATION_DATE
                            ModelYear = invoiceReader.IsDBNull(5) ? null : invoiceReader.GetInt16(5),          // MODEL_YEAR
                            
                            // Owner Information
                            OwnerNo = invoiceReader.GetInt32(6),                                                // OWNER_NO
                            OwnerName = invoiceReader.IsDBNull(7) ? null : invoiceReader.GetString(7),         // OWNER_NAME
                            OwnerAddress1 = invoiceReader.IsDBNull(8) ? null : invoiceReader.GetString(8),     // OWNER_ADRESS_1
                            OwnerAddress2 = invoiceReader.IsDBNull(9) ? null : invoiceReader.GetString(9),     // OWNER_ADRESS_2
                            OwnerZipAndCity = invoiceReader.IsDBNull(10) ? null : invoiceReader.GetString(10), // OWNER_ZIP_AND_CITY
                            OwnerPhone = invoiceReader.IsDBNull(11) ? null : invoiceReader.GetString(11),      // OWNER_PHONE
                            OwnerMail = invoiceReader.IsDBNull(12) ? null : invoiceReader.GetString(12),       // OWNER_MAIL
                            
                            // Payer Information
                            PayerNo = invoiceReader.GetInt32(13),                                               // PAYER_NO
                            PayerName = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetString(14),       // PAYER_NAME
                            PayerAddress1 = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetString(15),   // PAYER_ADRESS_1
                            PayerAddress2 = invoiceReader.IsDBNull(16) ? null : invoiceReader.GetString(16),   // PAYER_ADRESS_2
                            PayerZipAndCity = invoiceReader.IsDBNull(17) ? null : invoiceReader.GetString(17), // PAYER_ZIP_AND_CITY
                            PayerPhone = invoiceReader.IsDBNull(18) ? null : invoiceReader.GetString(18),      // PAYER_PHONE
                            PayerMail = invoiceReader.IsDBNull(19) ? null : invoiceReader.GetString(19),       // PAYER_MAIL
                            PayerVatNo = invoiceReader.IsDBNull(20) ? null : invoiceReader.GetString(20),      // PAYER_VATNO
                            
                            // Driver Information
                            DriverNo = invoiceReader.GetInt32(21),                                              // DRIVER_NO
                            DriverName = invoiceReader.IsDBNull(22) ? null : invoiceReader.GetString(22),      // DRIVER_NAME
                            DriverAddress1 = invoiceReader.IsDBNull(23) ? null : invoiceReader.GetString(23),  // DRIVER_ADRESS_1
                            DriverAddress2 = invoiceReader.IsDBNull(24) ? null : invoiceReader.GetString(24),  // DRIVER_ADRESS_2
                            DriverZipAndCity = invoiceReader.IsDBNull(25) ? null : invoiceReader.GetString(25), // DRIVER_ZIP_AND_CITY
                            DriverPhone = invoiceReader.IsDBNull(26) ? null : invoiceReader.GetString(26),     // DRIVER_PHONE
                            DriverMail = invoiceReader.IsDBNull(27) ? null : invoiceReader.GetString(27),      // DRIVER_MAIL
                            
                            // Invoice Information
                            InvoiceNo = currentInvoiceNo,                                                     // INVOICE_NO
                            
                            // Initialize Fortnox Logs array
                            FortnoxLogs = new List<FortnoxLog>()
                        };
                    }
                    
                    // Add Fortnox log to the invoice
                    var fortnoxLog = new FortnoxLog
                    {
                        Id = invoiceReader.GetInt32(29),                                           // FORTNOX_LOG.ID
                        TimeStamp = invoiceReader.IsDBNull(30) ? null : invoiceReader.GetDateTime(30), // FORTNOX_LOG.TIME_STAMP
                        TransactionNo = invoiceReader.IsDBNull(31) ? null : invoiceReader.GetInt32(31), // FORTNOX_LOG.TRANSACTION_NO
                        Description = invoiceReader.IsDBNull(32) ? null : invoiceReader.GetString(32), // FORTNOX_LOG.DESCRIPTION
                        ErrorCode = invoiceReader.IsDBNull(33) ? null : invoiceReader.GetString(33), // FORTNOX_LOG.ERROR_CODE
                        ErrorMessage = invoiceReader.IsDBNull(34) ? null : invoiceReader.GetString(34), // FORTNOX_LOG.ERROR_MESSAGE
                        LogType = invoiceReader.IsDBNull(35) ? null : invoiceReader.GetInt32(35), // FORTNOX_LOG.LOG_TYPE
                        KeyNo = invoiceReader.IsDBNull(36) ? null : invoiceReader.GetString(36)     // FORTNOX_LOG.KEY_NO
                    };
                    
                    invoiceData[currentInvoiceNo].FortnoxLogs.Add(fortnoxLog);
                }
                
                // Add invoices to their corresponding orders
                foreach (var invoice in invoiceData.Values)
                {
                    var order = results.FirstOrDefault(o => o.OrhDokn == invoice.InvoiceNo);
                    if (order != null)
                    {
                        order.Invoices.Add(invoice);
                        _logger.LogInformation("Added invoice {InvoiceNo} with {LogCount} Fortnox logs to order {OrderNo}", 
                            invoice.InvoiceNo, invoice.FortnoxLogs.Count, order.OrhDokn);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find order for invoice {InvoiceNo}", invoice.InvoiceNo);
                    }
                }
                
                _logger.LogInformation("Found {InvoiceCount} invoices for {OrderCount} orders", invoiceCount, orderNumbers.Count);
            }

            sw.Stop();
            _logger.LogInformation("Orders with invoices query completed in {ElapsedMs}ms. Found {Count} orders with their invoices", 
                sw.ElapsedMilliseconds, results.Count);

            // Removed reflection-based cleanup for performance

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Orders with invoices query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<string> GetOrdhuvTableStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            // Try a simple approach - just get one row to see what columns exist
            using var command = new FbCommand(@"
                SELECT FIRST 1 *
                FROM ORDHUV", connection);

            var columns = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var columnType = reader.GetFieldType(i).Name;
                    var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL";
                    columns.Add($"{columnName} ({columnType}) = {value}");
                }
            }

            return string.Join("\n", columns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ORDHUV table structure");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> GetInvoiceIndividualTableStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand("SELECT FIRST 1 * FROM INVOICEINDIVIDUAL", connection);
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
            
            return "No data found in INVOICEINDIVIDUAL table";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get INVOICEINDIVIDUAL table structure");
            throw;
        }
    }

    public async Task<string> GetFortnoxLogTableStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand("SELECT FIRST 1 * FROM FORTNOX_LOG", connection);
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
            
            return "No data found in FORTNOX_LOG table";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get FORTNOX_LOG table structure");
            throw;
        }
    }

    public async Task<string> GetKunregTableStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand("SELECT FIRST 1 * FROM KUNREG", connection);
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
            
            return "No data found in KUNREG table";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get KUNREG table structure");
            throw;
        }
    }

    public async Task<string> GetBilregTableStructureAsync()
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new FbCommand("SELECT FIRST 1 * FROM BILREG", connection);
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
            
            return "No data found in BILREG table";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get BILREG table structure");
            throw;
        }
    }

    // Removed reflection-based cleanup methods for performance

}
