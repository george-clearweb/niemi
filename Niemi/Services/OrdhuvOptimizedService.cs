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

        /// <summary>
        /// Splits customer name into first name and last name
        /// Requires: comma followed by exactly one space, and first name must be a single word
        /// Examples: "Andersson, Erik" -> lastName: "Andersson", firstName: "Erik"
        ///           "JOHANSSON, ANNA" -> lastName: "Johansson", firstName: "Anna"
        ///           "Andersson,  Erik" -> lastName: null, firstName: null (multiple spaces - invalid)
        ///           "ABU, MAXWELL KWAME" -> lastName: null, firstName: null (first name has spaces - invalid)
        ///           "Erik Andersson" -> lastName: null, firstName: null (no comma)
        /// </summary>
        private static (string? lastName, string? firstName) SplitCustomerName(string? customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return (null, null);

            var normalized = customerName.Trim();
            
            // Look for pattern: "LastName, FirstName" (comma followed by exactly one space)
            var commaIndex = normalized.IndexOf(',');
            if (commaIndex == -1)
            {
                // No comma found
                return (null, null);
            }

            // Check if there's exactly one space after the comma
            if (commaIndex + 2 >= normalized.Length || normalized[commaIndex + 1] != ' ')
            {
                // No space or more than one space after comma
                return (null, null);
            }

            // Check if there's a second space (which would make it invalid)
            if (commaIndex + 3 < normalized.Length && normalized[commaIndex + 2] == ' ')
            {
                // More than one space after comma
                return (null, null);
            }

            // Extract last name (before comma) and first name (after comma and single space)
            var lastName = normalized.Substring(0, commaIndex).Trim();
            var firstName = normalized.Substring(commaIndex + 2).Trim(); // Skip comma and single space

            // Check that first name doesn't contain any spaces (should be single word)
            if (firstName.Contains(' '))
            {
                // First name contains spaces, which is invalid
                return (null, null);
            }

            // Apply proper capitalization (Title Case)
            lastName = ToTitleCase(lastName);
            firstName = ToTitleCase(firstName);

            return (string.IsNullOrEmpty(lastName) ? null : lastName,
                    string.IsNullOrEmpty(firstName) ? null : firstName);
        }

        /// <summary>
        /// Converts string to Title Case (first letter uppercase, rest lowercase)
        /// </summary>
        private static string ToTitleCase(string? input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length == 0)
                return string.Empty;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        /// <summary>
        /// Finds the first mobile phone number from the provided phone numbers and formats it as +467##
        /// Logic: Clean spaces and "-", strip leading 0/+/46, if starts with 7 then add +46
        /// Examples: 
        /// - "+46706670431" -> "+46706670431" (already correct)
        /// - "0707799545" -> "+46707799545"
        /// - "4670 559 68 47" -> "+46705596847"
        /// - "070-383 35 67" -> "+46703833567"
        /// - "070 668 73 87" -> "+46706687387"
        /// - "0920-230088" -> null (not mobile - doesn't start with 7)
        /// - "461121-9231" -> null (not mobile - doesn't start with 7)
        /// </summary>
        private static string? FindAndFormatMobilePhone(string? tel1, string? tel2, string? tel3)
        {
            var phoneNumbers = new[] { tel1, tel2, tel3 };
            
            foreach (var phone in phoneNumbers)
            {
                if (string.IsNullOrWhiteSpace(phone))
                    continue;
                    
                var formatted = FormatMobilePhone(phone);
                if (formatted != null)
                    return formatted;
            }
            
            return null;
        }

        /// <summary>
        /// Formats a phone number if it's a Swedish mobile number, otherwise returns null
        /// </summary>
        private static string? FormatMobilePhone(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Step 1: Clean spaces and "-"
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "");
            
            // Step 2: Strip leading 0, +, 46
            while (cleaned.StartsWith("0") || cleaned.StartsWith("+") || cleaned.StartsWith("46"))
            {
                if (cleaned.StartsWith("0"))
                    cleaned = cleaned.Substring(1);
                else if (cleaned.StartsWith("+"))
                    cleaned = cleaned.Substring(1);
                else if (cleaned.StartsWith("46"))
                    cleaned = cleaned.Substring(2);
            }
            
            // Step 3: If it starts with 7, it's a mobile
            if (cleaned.StartsWith("7"))
            {
                // Step 4: Add +46
                return "+46" + cleaned;
            }
            
            // Not a mobile number
            return null;
        }

        /// <summary>
        /// Splits postal address into zip code and city
        /// Examples: "97346  LULEÅ" -> zip: "97346", city: "LULEÅ"
        ///           "945 33 ROSVIK" -> zip: "94533", city: "ROSVIK"
        /// </summary>
        private static (string? zipCode, string? city) SplitPostalAddress(string? postalAddress)
        {
            if (string.IsNullOrWhiteSpace(postalAddress))
                return (null, null);

            // Remove extra whitespace and normalize
            var normalized = postalAddress.Trim();
            
            // Find the first sequence of letters (city starts here)
            var cityStartIndex = -1;
            for (int i = 0; i < normalized.Length; i++)
            {
                if (char.IsLetter(normalized[i]))
                {
                    cityStartIndex = i;
                    break;
                }
            }

            if (cityStartIndex == -1)
            {
                // No letters found, might be just numbers - strip all spaces for consistency
                var zipCodeOnly = normalized.Replace(" ", "");
                return (string.IsNullOrEmpty(zipCodeOnly) ? null : zipCodeOnly, null);
            }

            // Extract zip code (everything before the first letter) and strip all spaces
            var zipCode = normalized.Substring(0, cityStartIndex).Replace(" ", "");
            
            // Extract city (everything from the first letter onwards)
            var city = normalized.Substring(cityStartIndex).Trim();

            // Return null for empty strings
            return (string.IsNullOrEmpty(zipCode) ? null : zipCode,
                    string.IsNullOrEmpty(city) ? null : city);
        }

        /// <summary>
        /// Creates a KunregDto from the data reader with postal address, name splitting, and mobile phone detection
        /// </summary>
        private static KunregDto CreateKunregDto(System.Data.Common.DbDataReader reader, int kunKunrIndex, int kunPadrIndex)
        {
            var kunNamn = reader.IsDBNull(kunKunrIndex + 1) ? null : reader.GetString(kunKunrIndex + 1);
            var kunAdr1 = reader.IsDBNull(kunKunrIndex + 2) ? null : reader.GetString(kunKunrIndex + 2);
            var kunAdr2 = reader.IsDBNull(kunKunrIndex + 3) ? null : reader.GetString(kunKunrIndex + 3);
            var kunPadr = reader.IsDBNull(kunPadrIndex) ? null : reader.GetString(kunPadrIndex);
            var kunOrgn = reader.IsDBNull(kunKunrIndex + 5) ? null : reader.GetString(kunKunrIndex + 5);
            var kunTel1 = reader.IsDBNull(kunKunrIndex + 6) ? null : reader.GetString(kunKunrIndex + 6);
            var kunTel2 = reader.IsDBNull(kunKunrIndex + 7) ? null : reader.GetString(kunKunrIndex + 7);
            var kunTel3 = reader.IsDBNull(kunKunrIndex + 8) ? null : reader.GetString(kunKunrIndex + 8);
            var kunEpostadress = reader.IsDBNull(kunKunrIndex + 9) ? null : reader.GetString(kunKunrIndex + 9);
            
            var (lastName, firstName) = SplitCustomerName(kunNamn);
            var (zipCode, city) = SplitPostalAddress(kunPadr);
            var mobilePhone = FindAndFormatMobilePhone(kunTel1, kunTel2, kunTel3);
            
            return new KunregDto
            {
                // Original database fields
                KunKunr = reader.IsDBNull(kunKunrIndex) ? 0 : 
                         (int.TryParse(reader.GetString(kunKunrIndex), out var kunKunrValue) ? kunKunrValue : 0),
                KunNamn = kunNamn,
                KunAdr1 = kunAdr1,
                KunAdr2 = kunAdr2,
                KunPadr = kunPadr,
                KunOrgn = kunOrgn,
                KunTel1 = kunTel1,
                KunTel2 = kunTel2,
                KunTel3 = kunTel3,
                KunEpostadress = kunEpostadress,
                
                // Calculated/parsed fields
                FirstName = firstName,
                LastName = lastName,
                ZipCode = zipCode,
                City = city,
                MobilePhone = mobilePhone
            };
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
                    d.KUN_EPOSTADRESS as DRIVER_KUN_EPOSTADRESS
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
                    Customer = orderReader.IsDBNull(13) ? null : CreateKunregDto(orderReader, 13, 17),
                    
                    // Payer data (ORH_BETKUNR)
                    Payer = orderReader.IsDBNull(23) ? null : CreateKunregDto(orderReader, 23, 27),
                    
                    // Driver data (ORH_DRIVER_NO)
                    Driver = orderReader.IsDBNull(33) ? null : CreateKunregDto(orderReader, 33, 37)
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
                
                // Add invoices to their corresponding orders and calculate timestamps
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
                        _logger.LogInformation("Added invoice {InvoiceNo} with {LogCount} Fortnox logs to order {OrderNo}", 
                            invoice.InvoiceNo, invoice.FortnoxLogs.Count, order.OrhDokn);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find order for invoice {InvoiceNo}", invoice.InvoiceNo);
                    }
                }
                
                // Calculate min and max timestamps for each order across all invoices
                foreach (var order in results)
                {
                    var allTimestamps = order.Invoices
                        .SelectMany(invoice => invoice.FortnoxLogs)
                        .Where(log => log.TimeStamp.HasValue)
                        .Select(log => log.TimeStamp!.Value)
                        .ToList();
                    
                    if (allTimestamps.Any())
                    {
                        order.MinFortnoxTimeStamp = allTimestamps.Min();
                        order.MaxFortnoxTimeStamp = allTimestamps.Max();
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
