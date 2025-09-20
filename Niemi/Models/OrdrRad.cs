namespace Niemi.Models;

public class OrdrRad
{
    public int OrrDokn { get; set; }                    // ORR_DOKN - Order Number
    public int OrrRadnr { get; set; }                   // ORR_RADNR - Row Number
    public string? OrrArtkod { get; set; }              // ORR_ARTKOD - Article Code
    public string? OrrBeskr { get; set; }               // ORR_BESKR - Description
    public double OrrAntal { get; set; }                // ORR_ANTAL - Quantity
    public double OrrPris { get; set; }                 // ORR_PRIS - Price
    public double OrrRabatt { get; set; }               // ORR_RABATT - Discount
    public double OrrMoms { get; set; }                 // ORR_MOMS - VAT
    public string? OrrEnhet { get; set; }               // ORR_ENHET - Unit
    public string? OrrKod { get; set; }                 // ORR_KOD - Code
    public string? OrrKategori { get; set; }             // ORR_KATEGORI - Category
    public string? OrrKeyword { get; set; }             // ORR_KEYWORD - Keyword
    public DateTime? OrrCreatedAt { get; set; }         // ORR_CREATED_AT - Created At
    public DateTime? OrrUpdatedAt { get; set; }         // ORR_UPDATED_AT - Updated At
}
