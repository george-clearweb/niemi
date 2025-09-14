namespace Niemi.Models.DTOs;

public class InvoiceIndividualDto
{
    // Vehicle information
    public string? VehicleNo { get; set; }              // VEHICLE_NO
    public string? Manufacturer { get; set; }           // MANUFACTURER
    public string? Model { get; set; }                  // MODEL
    public string? Vin { get; set; }                    // VIN
    public DateTime? RegistrationDate { get; set; }     // REGISTRATION_DATE
    public short? ModelYear { get; set; }               // MODEL_YEAR
    
    // Owner Information (only include if different from customer)
    public int? OwnerNo { get; set; }                   // OWNER_NO (only if different from customer)
    public string? OwnerName { get; set; }              // OWNER_NAME
    public string? OwnerAddress2 { get; set; }          // OWNER_ADRESS_2 (only if not empty)
    public string? OwnerZipAndCity { get; set; }        // OWNER_ZIP_AND_CITY (only if not empty)
    public string? OwnerPhone { get; set; }             // OWNER_PHONE (only if not empty)
    public string? OwnerMail { get; set; }              // OWNER_MAIL (only if not empty)
    
    // Payer Information (only include if different from customer)
    public int? PayerNo { get; set; }                   // PAYER_NO (only if different from customer)
    public string? PayerName { get; set; }              // PAYER_NAME
    public string? PayerAddress2 { get; set; }          // PAYER_ADRESS_2 (only if not empty)
    public string? PayerZipAndCity { get; set; }        // PAYER_ZIP_AND_CITY (only if not empty)
    public string? PayerPhone { get; set; }             // PAYER_PHONE (only if not empty)
    public string? PayerMail { get; set; }              // PAYER_MAIL (only if not empty)
    public string? PayerVatNo { get; set; }             // PAYER_VATNO (only if not empty)
    
    // Driver Information (only include if different from customer)
    public int? DriverNo { get; set; }                  // DRIVER_NO (only if different from customer)
    public string? DriverName { get; set; }             // DRIVER_NAME
    public string? DriverAddress2 { get; set; }         // DRIVER_ADRESS_2 (only if not empty)
    public string? DriverZipAndCity { get; set; }       // DRIVER_ZIP_AND_CITY (only if not empty)
    public string? DriverPhone { get; set; }            // DRIVER_PHONE (only if not empty)
    public string? DriverMail { get; set; }             // DRIVER_MAIL (only if not empty)
    
    // Invoice Information
    public int InvoiceNo { get; set; }                  // INVOICE_NO
    
    // Timestamp information
    public DateTime? MinFortnoxTimeStamp { get; set; }  // Earliest timestamp from FortnoxLogs for this invoice
    
    // Nested Fortnox Logs
    public List<FortnoxLogDto> FortnoxLogs { get; set; } = new List<FortnoxLogDto>();
}
