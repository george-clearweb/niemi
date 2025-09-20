namespace Niemi.Models.DTOs;

public class OrdhuvDto
{
    // Core order information
    public int OrhDokn { get; set; }                    // ORH_DOKN - Order Number
    public int OrhKunr { get; set; }                    // ORH_KUNR - Customer Number
    public DateTime? OrhDokd { get; set; }              // ORH_DOKD - Order Date
    public string? OrhRenr { get; set; }                // ORH_RENR - Reference Number
    public string? OrhStat { get; set; }                // ORH_STAT - Status
    public DateTime? OrhLovdat { get; set; }            // ORH_LOVDAT - Delivery Date
    public string? OrhFakturerad { get; set; }          // ORH_FAKTURERAD - Invoiced
    public string? OrhNamn { get; set; }                // ORH_NAMN - Customer Name
    public double? OrhSummainkl { get; set; }           // ORH_SUMMAINKL - Sum Including
    public DateTime? OrhCreatedAt { get; set; }         // ORH_CREATED_AT - Created At
    public DateTime? OrhUpdatedAt { get; set; }         // ORH_UPDATED_AT - Updated At
    
    // Additional meaningful fields (only include if they have actual values)
    public int? OrhBetkunr { get; set; }                // ORH_BETKUNR - Payment Customer Number (only if not 0)
    public int? OrhDriverNo { get; set; }               // ORH_DRIVER_NO - Driver Number (only if not 0)
    
    // Timestamp information from FortnoxLogs across all invoices
    public DateTime? MinFortnoxTimeStamp { get; set; }  // Earliest timestamp from all FortnoxLogs for this order
    public DateTime? MaxFortnoxTimeStamp { get; set; }  // Latest timestamp from all FortnoxLogs for this order
    
    // Navigation properties
    public List<InvoiceIndividualDto> Invoices { get; set; } = new List<InvoiceIndividualDto>();
    public KunregDto? Customer { get; set; }            // Customer (ORH_KUNR -> KUNREG.KUN_KUNR)
    public KunregDto? Payer { get; set; }               // Payer/Billing Customer (ORH_BETKUNR -> KUNREG.KUN_KUNR)
    public KunregDto? Driver { get; set; }              // Driver (ORH_DRIVER_NO -> KUNREG.KUN_KUNR)
    public BilregDto? Vehicle { get; set; }             // Vehicle (ORH_RENR -> BILREG.BIL_RENR)
}
