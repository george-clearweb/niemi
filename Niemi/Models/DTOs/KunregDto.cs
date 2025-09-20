namespace Niemi.Models.DTOs;

public class KunregDto
{
    // Original database fields
    public int KunKunr { get; set; }                     // KUN_KUNR - Customer Number (Primary Key)
    public string? KunNamn { get; set; }                // KUN_NAMN - Customer Name
    public string? KunAdr1 { get; set; }                // KUN_ADR1 - Address 1
    public string? KunAdr2 { get; set; }                // KUN_ADR2 - Address 2
    public string? KunPadr { get; set; }                // KUN_PADR - Postal Address
    public string? KunOrgn { get; set; }                // KUN_ORGN - Organization Number
    public string? KunTel1 { get; set; }                // KUN_TEL1 - Phone 1
    public string? KunTel2 { get; set; }                // KUN_TEL2 - Phone 2
    public string? KunTel3 { get; set; }                // KUN_TEL3 - Phone 3
    public string? KunEpostadress { get; set; }         // KUN_EPOSTADRESS - Email Address
    
    // Calculated/parsed fields
    public string? FirstName { get; set; }              // Extracted first name from KUN_NAMN (after comma)
    public string? LastName { get; set; }               // Extracted last name from KUN_NAMN (before comma)
    public string? CompanyName { get; set; }             // Company name for company customers
    public string? ZipCode { get; set; }                // Extracted zip code from KUN_PADR (numbers only)
    public string? City { get; set; }                   // Extracted city from KUN_PADR (letters only)
    public string? MobilePhone { get; set; }            // First mobile phone number found, formatted as +467##
    public string? CustomerType { get; set; }           // "Company" or "Private" based on KUN_ORGN format
    public DateTime? BirthDate { get; set; }            // Extracted birthdate from KUN_ORGN for private persons (yyMMdd-#### format)
}
