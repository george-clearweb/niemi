namespace Niemi.Models.DTOs;

public class KunregDto
{
    public int KunKunr { get; set; }                     // KUN_KUNR - Customer Number (Primary Key)
    public string? KunNamn { get; set; }                // KUN_NAMN - Customer Name
    public string? KunAdr2 { get; set; }                // KUN_ADR2 - Address 2 (only if not empty)
    public string? KunOrgn { get; set; }                // KUN_ORGN - Organization Number (only if not empty)
    public string? KunTel1 { get; set; }                // KUN_TEL1 - Phone 1 (only if not empty)
    public string? KunTel2 { get; set; }                // KUN_TEL2 - Phone 2 (only if not empty)
    public string? KunEpostadress { get; set; }         // KUN_EPOSTADRESS - Email Address (only if not empty)
    public string? KunMomsnr { get; set; }              // KUN_MOMSNR - VAT Number (only if not empty)
    
    // Only include these if they have meaningful values (not 0)
    public short? KunKrti { get; set; }                 // KUN_KRTI - Credit Terms (only if not 0)
    public int? KunFleetpayer { get; set; }             // KUN_FLEETPAYER (only if not 0)
    public int? ReminderDisable { get; set; }           // REMINDER_DISABLE (only if not 0)
}
