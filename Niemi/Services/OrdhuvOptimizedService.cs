using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class OrdhuvOptimizedService : IOrdhuvOptimizedService
{
    private readonly string _connectionString;
    private readonly ILogger<OrdhuvOptimizedService> _logger;

    public OrdhuvOptimizedService(IConfiguration configuration, ILogger<OrdhuvOptimizedService> logger)
    {
        _connectionString = configuration.GetConnectionString("FirebirdConnection") 
            ?? throw new ArgumentNullException("FirebirdConnection string is missing");
        _logger = logger;
    }

    public async Task<IEnumerable<OrdhuvDto>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<OrdhuvDto>();

        try
        {
            using var connection = new FbConnection(_connectionString);
            await connection.OpenAsync();
            
            // Step 1: Get orders that have invoices in the date range with customer data
            // Only select meaningful columns and use conditional logic to avoid fetching zeros
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
                    CASE WHEN o.ORH_BETKUNR > 0 THEN o.ORH_BETKUNR ELSE NULL END as ORH_BETKUNR,
                    CASE WHEN o.ORH_DRIVER_NO > 0 THEN o.ORH_DRIVER_NO ELSE NULL END as ORH_DRIVER_NO,
                    -- Customer data (ORH_KUNR) - only meaningful fields
                    c.KUN_KUNR as CUST_KUN_KUNR,
                    c.KUN_NAMN as CUST_KUN_NAMN,
                    CASE WHEN c.KUN_ADR2 IS NOT NULL AND c.KUN_ADR2 != '' THEN c.KUN_ADR2 ELSE NULL END as CUST_KUN_ADR2,
                    CASE WHEN c.KUN_ORGN IS NOT NULL AND c.KUN_ORGN != '' THEN c.KUN_ORGN ELSE NULL END as CUST_KUN_ORGN,
                    CASE WHEN c.KUN_TEL1 IS NOT NULL AND c.KUN_TEL1 != '' THEN c.KUN_TEL1 ELSE NULL END as CUST_KUN_TEL1,
                    CASE WHEN c.KUN_TEL2 IS NOT NULL AND c.KUN_TEL2 != '' THEN c.KUN_TEL2 ELSE NULL END as CUST_KUN_TEL2,
                    CASE WHEN c.KUN_EPOSTADRESS IS NOT NULL AND c.KUN_EPOSTADRESS != '' THEN c.KUN_EPOSTADRESS ELSE NULL END as CUST_KUN_EPOSTADRESS,
                    CASE WHEN c.KUN_MOMSNR IS NOT NULL AND c.KUN_MOMSNR != '' THEN c.KUN_MOMSNR ELSE NULL END as CUST_KUN_MOMSNR,
                    CASE WHEN c.KUN_KRTI > 0 THEN c.KUN_KRTI ELSE NULL END as CUST_KUN_KRTI,
                    CASE WHEN c.KUN_FLEETPAYER > 0 THEN c.KUN_FLEETPAYER ELSE NULL END as CUST_KUN_FLEETPAYER,
                    CASE WHEN c.REMINDER_DISABLE > 0 THEN c.REMINDER_DISABLE ELSE NULL END as CUST_REMINDER_DISABLE,
                    -- Payer data (ORH_BETKUNR) - only meaningful fields
                    p.KUN_KUNR as PAYER_KUN_KUNR,
                    p.KUN_NAMN as PAYER_KUN_NAMN,
                    CASE WHEN p.KUN_ADR2 IS NOT NULL AND p.KUN_ADR2 != '' THEN p.KUN_ADR2 ELSE NULL END as PAYER_KUN_ADR2,
                    CASE WHEN p.KUN_ORGN IS NOT NULL AND p.KUN_ORGN != '' THEN p.KUN_ORGN ELSE NULL END as PAYER_KUN_ORGN,
                    CASE WHEN p.KUN_TEL1 IS NOT NULL AND p.KUN_TEL1 != '' THEN p.KUN_TEL1 ELSE NULL END as PAYER_KUN_TEL1,
                    CASE WHEN p.KUN_TEL2 IS NOT NULL AND p.KUN_TEL2 != '' THEN p.KUN_TEL2 ELSE NULL END as PAYER_KUN_TEL2,
                    CASE WHEN p.KUN_EPOSTADRESS IS NOT NULL AND p.KUN_EPOSTADRESS != '' THEN p.KUN_EPOSTADRESS ELSE NULL END as PAYER_KUN_EPOSTADRESS,
                    CASE WHEN p.KUN_MOMSNR IS NOT NULL AND p.KUN_MOMSNR != '' THEN p.KUN_MOMSNR ELSE NULL END as PAYER_KUN_MOMSNR,
                    CASE WHEN p.KUN_KRTI > 0 THEN p.KUN_KRTI ELSE NULL END as PAYER_KUN_KRTI,
                    CASE WHEN p.KUN_FLEETPAYER > 0 THEN p.KUN_FLEETPAYER ELSE NULL END as PAYER_KUN_FLEETPAYER,
                    CASE WHEN p.REMINDER_DISABLE > 0 THEN p.REMINDER_DISABLE ELSE NULL END as PAYER_REMINDER_DISABLE,
                    -- Driver data (ORH_DRIVER_NO) - only meaningful fields
                    d.KUN_KUNR as DRIVER_KUN_KUNR,
                    d.KUN_NAMN as DRIVER_KUN_NAMN,
                    CASE WHEN d.KUN_ADR2 IS NOT NULL AND d.KUN_ADR2 != '' THEN d.KUN_ADR2 ELSE NULL END as DRIVER_KUN_ADR2,
                    CASE WHEN d.KUN_ORGN IS NOT NULL AND d.KUN_ORGN != '' THEN d.KUN_ORGN ELSE NULL END as DRIVER_KUN_ORGN,
                    CASE WHEN d.KUN_TEL1 IS NOT NULL AND d.KUN_TEL1 != '' THEN d.KUN_TEL1 ELSE NULL END as DRIVER_KUN_TEL1,
                    CASE WHEN d.KUN_TEL2 IS NOT NULL AND d.KUN_TEL2 != '' THEN d.KUN_TEL2 ELSE NULL END as DRIVER_KUN_TEL2,
                    CASE WHEN d.KUN_EPOSTADRESS IS NOT NULL AND d.KUN_EPOSTADRESS != '' THEN d.KUN_EPOSTADRESS ELSE NULL END as DRIVER_KUN_EPOSTADRESS,
                    CASE WHEN d.KUN_MOMSNR IS NOT NULL AND d.KUN_MOMSNR != '' THEN d.KUN_MOMSNR ELSE NULL END as DRIVER_KUN_MOMSNR,
                    CASE WHEN d.KUN_KRTI > 0 THEN d.KUN_KRTI ELSE NULL END as DRIVER_KUN_KRTI,
                    CASE WHEN d.KUN_FLEETPAYER > 0 THEN d.KUN_FLEETPAYER ELSE NULL END as DRIVER_KUN_FLEETPAYER,
                    CASE WHEN d.REMINDER_DISABLE > 0 THEN d.REMINDER_DISABLE ELSE NULL END as DRIVER_REMINDER_DISABLE
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
                
            _logger.LogInformation("Executing optimized orders with invoices query with fromDate: {FromDate}, toDate: {ToDate}", 
                fromDate, toDate);

            // Get all distinct orders
            using var orderReader = await orderCommand.ExecuteReaderAsync();
            var orderNumbers = new List<int>();
            
            while (await orderReader.ReadAsync())
            {
                var order = new OrdhuvDto
                {
                    OrhDokn = orderReader.GetInt32(0),                    // ORH_DOKN - Order Number
                    OrhKunr = orderReader.GetInt32(1),                    // ORH_KUNR - Customer Number
                    OrhDokd = orderReader.IsDBNull(2) ? null : orderReader.GetDateTime(2), // ORH_DOKD - Order Date
                    OrhRenr = orderReader.IsDBNull(3) ? null : orderReader.GetString(3),   // ORH_RENR - Reference Number
                    OrhStat = orderReader.IsDBNull(4) ? null : orderReader.GetString(4),   // ORH_STAT - Status
                    OrhLovdat = orderReader.IsDBNull(5) ? null : orderReader.GetDateTime(5), // ORH_LOVDAT - Delivery Date
                    OrhFakturerad = orderReader.IsDBNull(6) ? null : orderReader.GetString(6), // ORH_FAKTURERAD - Invoiced
                    OrhNamn = orderReader.IsDBNull(7) ? null : orderReader.GetString(7),   // ORH_NAMN - Customer Name
                    OrhSummainkl = orderReader.IsDBNull(8) ? null : orderReader.GetDouble(8), // ORH_SUMMAINKL - Sum Including
                    OrhCreatedAt = orderReader.IsDBNull(9) ? null : orderReader.GetDateTime(9), // ORH_CREATED_AT - Created At
                    OrhUpdatedAt = orderReader.IsDBNull(10) ? null : orderReader.GetDateTime(10), // ORH_UPDATED_AT - Updated At
                    OrhBetkunr = orderReader.IsDBNull(11) ? null : orderReader.GetInt32(11), // ORH_BETKUNR - Payer Number
                    OrhDriverNo = orderReader.IsDBNull(12) ? null : orderReader.GetInt32(12), // ORH_DRIVER_NO - Driver Number
                    Invoices = new List<InvoiceIndividualDto>(),
                    
                    // Customer data (ORH_KUNR)
                    Customer = orderReader.IsDBNull(13) ? null : new KunregDto
                    {
                        KunKunr = orderReader.GetInt32(13),               // CUST_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(14) ? null : orderReader.GetString(14), // CUST_KUN_NAMN
                        KunAdr2 = orderReader.IsDBNull(15) ? null : orderReader.GetString(15), // CUST_KUN_ADR2
                        KunOrgn = orderReader.IsDBNull(16) ? null : orderReader.GetString(16), // CUST_KUN_ORGN
                        KunTel1 = orderReader.IsDBNull(17) ? null : orderReader.GetString(17), // CUST_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(18) ? null : orderReader.GetString(18), // CUST_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(19) ? null : orderReader.GetString(19), // CUST_KUN_EPOSTADRESS
                        KunMomsnr = orderReader.IsDBNull(20) ? null : orderReader.GetString(20), // CUST_KUN_MOMSNR
                        KunKrti = orderReader.IsDBNull(21) ? null : orderReader.GetInt16(21), // CUST_KUN_KRTI
                        KunFleetpayer = orderReader.IsDBNull(22) ? null : orderReader.GetInt32(22), // CUST_KUN_FLEETPAYER
                        ReminderDisable = orderReader.IsDBNull(23) ? null : orderReader.GetInt32(23) // CUST_REMINDER_DISABLE
                    },
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(24) ? null : new KunregDto
                    {
                        KunKunr = orderReader.GetInt32(24),               // PAYER_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(25) ? null : orderReader.GetString(25), // PAYER_KUN_NAMN
                        KunAdr2 = orderReader.IsDBNull(26) ? null : orderReader.GetString(26), // PAYER_KUN_ADR2
                        KunOrgn = orderReader.IsDBNull(27) ? null : orderReader.GetString(27), // PAYER_KUN_ORGN
                        KunTel1 = orderReader.IsDBNull(28) ? null : orderReader.GetString(28), // PAYER_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(29) ? null : orderReader.GetString(29), // PAYER_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(30) ? null : orderReader.GetString(30), // PAYER_KUN_EPOSTADRESS
                        KunMomsnr = orderReader.IsDBNull(31) ? null : orderReader.GetString(31), // PAYER_KUN_MOMSNR
                        KunKrti = orderReader.IsDBNull(32) ? null : orderReader.GetInt16(32), // PAYER_KUN_KRTI
                        KunFleetpayer = orderReader.IsDBNull(33) ? null : orderReader.GetInt32(33), // PAYER_KUN_FLEETPAYER
                        ReminderDisable = orderReader.IsDBNull(34) ? null : orderReader.GetInt32(34) // PAYER_REMINDER_DISABLE
                    },
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(35) ? null : new KunregDto
                    {
                        KunKunr = orderReader.GetInt32(35),               // DRIVER_KUN_KUNR
                        KunNamn = orderReader.IsDBNull(36) ? null : orderReader.GetString(36), // DRIVER_KUN_NAMN
                        KunAdr2 = orderReader.IsDBNull(37) ? null : orderReader.GetString(37), // DRIVER_KUN_ADR2
                        KunOrgn = orderReader.IsDBNull(38) ? null : orderReader.GetString(38), // DRIVER_KUN_ORGN
                        KunTel1 = orderReader.IsDBNull(39) ? null : orderReader.GetString(39), // DRIVER_KUN_TEL1
                        KunTel2 = orderReader.IsDBNull(40) ? null : orderReader.GetString(40), // DRIVER_KUN_TEL2
                        KunEpostadress = orderReader.IsDBNull(41) ? null : orderReader.GetString(41), // DRIVER_KUN_EPOSTADRESS
                        KunMomsnr = orderReader.IsDBNull(42) ? null : orderReader.GetString(42), // DRIVER_KUN_MOMSNR
                        KunKrti = orderReader.IsDBNull(43) ? null : orderReader.GetInt16(43), // DRIVER_KUN_KRTI
                        KunFleetpayer = orderReader.IsDBNull(44) ? null : orderReader.GetInt32(44), // DRIVER_KUN_FLEETPAYER
                        ReminderDisable = orderReader.IsDBNull(45) ? null : orderReader.GetInt32(45) // DRIVER_REMINDER_DISABLE
                    }
                };
                
                results.Add(order);
                orderNumbers.Add(order.OrhDokn);
            }
            
            _logger.LogInformation("Found {OrderCount} unique orders: {OrderNumbers}", orderNumbers.Count, string.Join(", ", orderNumbers.Take(10)));
            orderReader.Close();

            // Step 2: Get all invoices for these orders - only meaningful fields
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

                _logger.LogInformation("Executing optimized invoices query for {OrderCount} orders", orderNumbers.Count);

                using var invoiceReader = await invoiceCommand.ExecuteReaderAsync();
                var invoiceCount = 0;
                var invoiceData = new Dictionary<int, InvoiceIndividualDto>();
                
                while (await invoiceReader.ReadAsync())
                {
                    invoiceCount++;
                    var currentInvoiceNo = invoiceReader.GetInt32(25); // INVOICE_NO is at index 25
                    
                    // Create or get existing invoice
                    if (!invoiceData.ContainsKey(currentInvoiceNo))
                    {
                        invoiceData[currentInvoiceNo] = new InvoiceIndividualDto
                        {
                            // Vehicle information
                            VehicleNo = invoiceReader.IsDBNull(0) ? null : invoiceReader.GetString(0),           // VEHICLE_NO
                            Manufacturer = invoiceReader.IsDBNull(1) ? null : invoiceReader.GetString(1),       // MANUFACTURER
                            Model = invoiceReader.IsDBNull(2) ? null : invoiceReader.GetString(2),              // MODEL
                            Vin = invoiceReader.IsDBNull(3) ? null : invoiceReader.GetString(3),                // VIN
                            RegistrationDate = invoiceReader.IsDBNull(4) ? null : invoiceReader.GetDateTime(4), // REGISTRATION_DATE
                            ModelYear = invoiceReader.IsDBNull(5) ? null : invoiceReader.GetInt16(5),          // MODEL_YEAR
                            
                            // Owner Information
                            OwnerNo = invoiceReader.IsDBNull(6) ? null : invoiceReader.GetInt32(6),             // OWNER_NO
                            OwnerName = invoiceReader.IsDBNull(7) ? null : invoiceReader.GetString(7),         // OWNER_NAME
                            OwnerAddress2 = invoiceReader.IsDBNull(8) ? null : invoiceReader.GetString(8),     // OWNER_ADRESS_2
                            OwnerZipAndCity = invoiceReader.IsDBNull(9) ? null : invoiceReader.GetString(9),   // OWNER_ZIP_AND_CITY
                            OwnerPhone = invoiceReader.IsDBNull(10) ? null : invoiceReader.GetString(10),      // OWNER_PHONE
                            OwnerMail = invoiceReader.IsDBNull(11) ? null : invoiceReader.GetString(11),       // OWNER_MAIL
                            
                            // Payer Information
                            PayerNo = invoiceReader.IsDBNull(12) ? null : invoiceReader.GetInt32(12),          // PAYER_NO
                            PayerName = invoiceReader.IsDBNull(13) ? null : invoiceReader.GetString(13),       // PAYER_NAME
                            PayerAddress2 = invoiceReader.IsDBNull(14) ? null : invoiceReader.GetString(14),   // PAYER_ADRESS_2
                            PayerZipAndCity = invoiceReader.IsDBNull(15) ? null : invoiceReader.GetString(15), // PAYER_ZIP_AND_CITY
                            PayerPhone = invoiceReader.IsDBNull(16) ? null : invoiceReader.GetString(16),      // PAYER_PHONE
                            PayerMail = invoiceReader.IsDBNull(17) ? null : invoiceReader.GetString(17),       // PAYER_MAIL
                            PayerVatNo = invoiceReader.IsDBNull(18) ? null : invoiceReader.GetString(18),      // PAYER_VATNO
                            
                            // Driver Information
                            DriverNo = invoiceReader.IsDBNull(19) ? null : invoiceReader.GetInt32(19),         // DRIVER_NO
                            DriverName = invoiceReader.IsDBNull(20) ? null : invoiceReader.GetString(20),      // DRIVER_NAME
                            DriverAddress2 = invoiceReader.IsDBNull(21) ? null : invoiceReader.GetString(21),  // DRIVER_ADRESS_2
                            DriverZipAndCity = invoiceReader.IsDBNull(22) ? null : invoiceReader.GetString(22), // DRIVER_ZIP_AND_CITY
                            DriverPhone = invoiceReader.IsDBNull(23) ? null : invoiceReader.GetString(23),     // DRIVER_PHONE
                            DriverMail = invoiceReader.IsDBNull(24) ? null : invoiceReader.GetString(24),      // DRIVER_MAIL
                            
                            // Invoice Information
                            InvoiceNo = currentInvoiceNo,                                                     // INVOICE_NO
                            
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
            _logger.LogInformation("Optimized orders with invoices query completed in {ElapsedMs}ms. Found {Count} orders with their invoices", 
                sw.ElapsedMilliseconds, results.Count);

            return results;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Optimized orders with invoices query failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
