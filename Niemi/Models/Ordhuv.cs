namespace Niemi.Models;

public class Ordhuv
{
    public int OrhDokn { get; set; }                    // ORH_DOKN - Order Number
    public int OrhKunr { get; set; }                    // ORH_KUNR - Customer Number
    public DateTime? OrhDokd { get; set; }              // ORH_DOKD - Order Date
    public string? OrhRenr { get; set; }                // ORH_RENR - Reference Number
    public string? OrhStat { get; set; }                // ORH_STAT - Status
    public DateTime? OrhLovdat { get; set; }            // ORH_LOVDAT - Delivery Date
    public double? OrhMaxpr { get; set; }               // ORH_MAXPR - Max Price
    public int? OrhMils { get; set; }                   // ORH_MILS - Miles
    public DateTime? OrhIndat { get; set; }             // ORh_INDAT - Input Date
    public DateTime? OrhUtdat { get; set; }             // ORH_UTDAT - Output Date
    public string? OrhSalj { get; set; }                // ORH_SALJ - Sales Person
    public double? OrhFanr { get; set; }                // ORH_FANR - Invoice Number
    public string? OrhRef { get; set; }                 // ORH_REF - Reference
    public double OrhArb { get; set; }                  // ORH_ARB - Work Amount
    public double OrhDel { get; set; }                  // ORH_DEL - Parts Amount
    public double OrhForbr { get; set; }                // ORH_FORBR - Consumption Amount
    public double OrhMomsbel { get; set; }              // ORH_MOMSBEL - VAT Amount
    public double OrhFavg { get; set; }                 // ORH_FAVG - Average Amount
    public double OrhUtj { get; set; }                  // ORH_UTJ - Output Amount
    public string? OrhUtskriven { get; set; }           // ORH_UTSKRIVEN - Printed
    public string? OrhFakturerad { get; set; }          // ORH_FAKTURERAD - Invoiced
    public string? OrhNamn { get; set; }                // ORH_NAMN - Customer Name
    public DateTime? OrhForfdat { get; set; }           // ORH_FORFDAT - Forward Date
    public string? OrhArbkod { get; set; }              // ORH_ARBKOD - Work Code
    public string? OrhMatkod { get; set; }              // ORH_MATKOD - Material Code
    public int OrhBetkunr { get; set; }                 // ORH_BETKUNR - Payment Customer Number
    public string? OrhBetnamn { get; set; }             // ORH_BETNAMN - Payment Customer Name
    public double OrhSjrisk { get; set; }               // ORH_SJRISK - Insurance Risk
    public double OrhSjriskmoms { get; set; }           // ORH_SJRISKMOMS - Insurance Risk VAT
    public short OrhSjvrsktyp { get; set; }             // ORH_SJVRSKTYP - Insurance Risk Type
    public string? OrhSjriskjanej { get; set; }         // ORH_SJRISKJANEJ - Insurance Risk Yes/No
    public string? OrhSplit { get; set; }               // ORH_SPLIT - Split
    public double OrhMoms2 { get; set; }                // ORH_MOMS2 - VAT 2
    public double OrhMoms3 { get; set; }                // ORH_MOMS3 - VAT 3
    public string? OrhUser { get; set; }                // ORH_USER - User
    public double OrhSummaint { get; set; }             // ORH_SUMMAINT - Sum Internal
    public double OrhSummagar { get; set; }             // ORH_SUMMAGAR - Sum Garage
    public double OrhSummaexkl { get; set; }            // ORH_SUMMAEXKL - Sum Excluding
    public double OrhSummainkl { get; set; }            // ORH_SUMMAINKL - Sum Including
    public double OrhSummaintforbr { get; set; }        // ORH_SUMMAINTFORBR - Sum Internal Consumption
    public double OrhSummagarforbr { get; set; }        // ORH_SUMMAGARFORBR - Sum Garage Consumption
    public double OrhSummaintarb { get; set; }          // ORH_SUMMAINTARB - Sum Internal Work
    public double OrhSummaintdel { get; set; }          // ORH_SUMMAINTDEL - Sum Internal Parts
    public double OrhSummagardel { get; set; }          // ORH_SUMMAGARDEL - Sum Garage Parts
    public double OrhSummagararb { get; set; }          // ORH_SUMMAGARARB - Sum Garage Work
    public string? OrhSkadnr { get; set; }              // ORH_SKADNR - Damage Number
    public string? OrhVarref { get; set; }              // ORH_VARREF - Variable Reference
    public string? OrhInuse { get; set; }               // ORH_INUSE - In Use
    public string? OrhSfklar { get; set; }              // ORH_SFKLAR - SF Clear
    public string? OrhKto { get; set; }                 // ORH_KTO - Account
    public string? OrhEfakt { get; set; }               // ORH_EFAKT - E-Invoice
    public int? OrhStatus { get; set; }                 // ORH_STATUS - Status
    public string? OrhBestArb { get; set; }             // ORH_BEST_ARB - Best Work
    public string? OrhBestArbPrint { get; set; }        // ORH_BEST_ARB_PRINT - Best Work Print
    public int OrhOrdernr { get; set; }                 // ORH_ORDERNR - Order Number
    public int OrhOrigin { get; set; }                  // ORH_ORIGIN - Origin
    public decimal? OrhKontant { get; set; }            // ORH_KONTANT - Cash
    public string? OrhKontantkto { get; set; }          // ORH_KONTANTKTO - Cash Account
    public decimal? OrhKort { get; set; }               // ORH_KORT - Card
    public string? OrhKortkto { get; set; }             // ORH_KORTKTO - Card Account
    public decimal? OrhPreskort { get; set; }           // ORH_PRESKORT - Prepaid Card
    public string? OrhPreskortkto { get; set; }         // ORH_PRESKORTKTO - Prepaid Card Account
    public string? OrhSign { get; set; }                // ORH_SIGN - Signature
    public decimal OrhMoms1bel { get; set; }            // ORH_MOMS1BEL - VAT 1 Amount
    public decimal OrhMoms2bel { get; set; }            // ORH_MOMS2BEL - VAT 2 Amount
    public decimal OrhMoms3bel { get; set; }            // ORH_MOMS3BEL - VAT 3 Amount
    public decimal OrhMoms1percent { get; set; }        // ORH_MOMS1PERCENT - VAT 1 Percent
    public decimal OrhMoms2percent { get; set; }        // ORH_MOMS2PERCENT - VAT 2 Percent
    public decimal OrhMoms3percent { get; set; }        // ORH_MOMS3PERCENT - VAT 3 Percent
    public string? OrhUnitId { get; set; }              // ORH_UNIT_ID - Unit ID
    public string? OrhKassaId { get; set; }             // ORH_KASSA_ID - Cash Register ID
    public int? OrhTransaktion { get; set; }            // ORH_TRANSAKTION - Transaction
    public int? OrhKvittokopia { get; set; }            // ORH_KVITTOKOPIA - Receipt Copy
    public string? OrhCashier { get; set; }             // ORH_CASHIER - Cashier
    public DateTime? OrhStarttid { get; set; }          // ORH_STARTTID - Start Time
    public DateTime? OrhStopptid { get; set; }          // ORH_STOPPTID - Stop Time
    public double? OrhTotaltid { get; set; }            // ORH_TOTALTID - Total Time
    public int OrhElsaSync { get; set; }                // ORH_ELSA_SYNC - ELSA Sync
    public int OrhFromElsa { get; set; }                // ORH_FROM_ELSA - From ELSA
    public int OrhElsaOrder { get; set; }               // ORH_ELSA_ORDER - ELSA Order
    public int OrhTirestoreorder { get; set; }          // ORH_TIRESTOREORDER - Tire Store Order
    public string? OrhServiceorder { get; set; }        // ORH_SERVICEORDER - Service Order
    public string? OrhInsurance { get; set; }           // ORH_INSURANCE - Insurance
    public string? OrhInsuranceid { get; set; }         // ORH_INSURANCEID - Insurance ID
    public int OrhOriginalOrderno { get; set; }         // ORH_ORIGINAL_ORDERNO - Original Order Number
    public string? OrhIntMsg { get; set; }              // ORH_INT_MSG - Internal Message
    public int? OrhOpportunityId { get; set; }          // ORH_OPPORTUNITY_ID - Opportunity ID
    public DateTime? OrhCreAt { get; set; }             // ORH_CRE_AT - Created At
    public DateTime? OrhOrderAt { get; set; }           // ORH_ORDER_AT - Order At
    public int OrhVatSw { get; set; }                   // ORH_VAT_SW - VAT Switch
    public int OrhCabasSw { get; set; }                 // ORH_CABAS_SW - CABAS Switch
    public int OrhOrdertype { get; set; }               // ORH_ORDERTYPE - Order Type
    public string? OrhFleetAgreementno { get; set; }    // ORH_FLEET_AGREEMENTNO - Fleet Agreement Number
    public int CustomerStatus { get; set; }             // CUSTOMER_STATUS - Customer Status
    public DateTime? CustomerStatusRemindSentAt { get; set; } // CUSTOMER_STATUS_REMIND_SENT_AT
    public DateTime? CustomerStatusPickupSentAt { get; set; } // CUSTOMER_STATUS_PICKUP_SENT_AT
    public int ReminderDisable { get; set; }            // REMINDER_DISABLE - Reminder Disable
    public int OrderedPartsStatus { get; set; }         // ORDERED_PARTS_STATUS - Ordered Parts Status
    public DateTime? CustomerStatusMessageSentAt { get; set; } // CUSTOMER_STATUS_MESSAGE_SENT_AT
    public int ResursInvoice { get; set; }              // RESURS_INVOICE - Resurs Invoice
    public string? ResursInvoicePdf { get; set; }       // RESURS_INVOICE_PDF - Resurs Invoice PDF
    public decimal? OrhKort2 { get; set; }              // ORH_KORT2 - Card 2
    public string? OrhKort2kto { get; set; }            // ORH_KORT2KTO - Card 2 Account
    public decimal? OrhSwish { get; set; }              // ORH_SWISH - Swish
    public string? OrhSwishkto { get; set; }            // ORH_SWISHKTO - Swish Account
    public string? OrhCreatedBy { get; set; }           // ORH_CREATED_BY - Created By
    public DateTime? OrhCreatedAt { get; set; }         // ORH_CREATED_AT - Created At
    public string? OrhUpdatedBy { get; set; }           // ORH_UPDATED_BY - Updated By
    public DateTime? OrhUpdatedAt { get; set; }         // ORH_UPDATED_AT - Updated At
    public int SchemaVersion { get; set; }              // SCHEMA_VERSION - Schema Version
    public string? OrhKod { get; set; }                 // ORH_KOD - Code
    public int OrhDriverNo { get; set; }                // ORH_DRIVER_NO - Driver Number
    public DateTime? GdprUpdatedAt { get; set; }        // GDPR_UPDATED_AT - GDPR Updated At
    public int? BookedStatus { get; set; }              // BOOKED_STATUS - Booked Status
    public string? OrhEorderid { get; set; }            // ORH_EORDERID - E-Order ID
    public string? Ocr { get; set; }                    // OCR - OCR
    public int? SentStatus { get; set; }                // SENT_STATUS - Sent Status
    public int? PaymentStatus { get; set; }             // PAYMENT_STATUS - Payment Status
    public string? UniqueOrderRef { get; set; }         // UNIQUE_ORDER_REF - Unique Order Reference
    public string? CustOrderRef { get; set; }           // CUST_ORDER_REF - Customer Order Reference
    public int? SentElectronic { get; set; }            // SENT_ELECTRONIC - Sent Electronic
    public int? AppointmentId { get; set; }             // APPOINTMENT_ID - Appointment ID
    
    // Navigation property for invoices
    public List<InvoiceIndividual> Invoices { get; set; } = new List<InvoiceIndividual>();
    
    // Navigation properties for customer relationships
    public Kunreg? Customer { get; set; }              // Customer (ORH_KUNR -> KUNREG.KUN_KUNR)
    public Kunreg? Payer { get; set; }                 // Payer/Billing Customer (ORH_BETKUNR -> KUNREG.KUN_KUNR)
    public Kunreg? Driver { get; set; }                // Driver (ORH_DRIVER_NO -> KUNREG.KUN_KUNR)
}
