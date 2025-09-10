namespace Niemi.Models;

public class InvoiceIndividual
{
    // Invoice Individual Data
    public string? VehicleNo { get; set; }           // VEHICLE_NO
    public string? Manufacturer { get; set; }        // MANUFACTURER
    public string? Model { get; set; }               // MODEL
    public string? Vin { get; set; }                 // VIN
    public DateTime? RegistrationDate { get; set; }  // REGISTRATION_DATE
    public short? ModelYear { get; set; }            // MODEL_YEAR
    
    // Owner Information
    public int OwnerNo { get; set; }                 // OWNER_NO
    public string? OwnerName { get; set; }           // OWNER_NAME
    public string? OwnerAddress1 { get; set; }       // OWNER_ADRESS_1
    public string? OwnerAddress2 { get; set; }       // OWNER_ADRESS_2
    public string? OwnerZipAndCity { get; set; }     // OWNER_ZIP_AND_CITY
    public string? OwnerPhone { get; set; }          // OWNER_PHONE
    public string? OwnerMail { get; set; }           // OWNER_MAIL
    
    // Payer Information
    public int PayerNo { get; set; }                 // PAYER_NO
    public string? PayerName { get; set; }           // PAYER_NAME
    public string? PayerAddress1 { get; set; }       // PAYER_ADRESS_1
    public string? PayerAddress2 { get; set; }       // PAYER_ADRESS_2
    public string? PayerZipAndCity { get; set; }     // PAYER_ZIP_AND_CITY
    public string? PayerPhone { get; set; }          // PAYER_PHONE
    public string? PayerMail { get; set; }           // PAYER_MAIL
    public string? PayerVatNo { get; set; }          // PAYER_VATNO
    
    // Driver Information
    public int DriverNo { get; set; }                // DRIVER_NO
    public string? DriverName { get; set; }          // DRIVER_NAME
    public string? DriverAddress1 { get; set; }      // DRIVER_ADRESS_1
    public string? DriverAddress2 { get; set; }      // DRIVER_ADRESS_2
    public string? DriverZipAndCity { get; set; }    // DRIVER_ZIP_AND_CITY
    public string? DriverPhone { get; set; }         // DRIVER_PHONE
    public string? DriverMail { get; set; }          // DRIVER_MAIL
    
    // Invoice Information
    public int InvoiceNo { get; set; }               // INVOICE_NO
    
    // Nested Fortnox Logs
    public List<FortnoxLog> FortnoxLogs { get; set; } = new List<FortnoxLog>();
}