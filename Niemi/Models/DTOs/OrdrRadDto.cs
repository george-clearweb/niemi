namespace Niemi.Models.DTOs;

public class OrdrRadDto
{
    public int OrdDokn { get; set; }                     // ORD_DOKN - Order Number
    public int OrdRadnr { get; set; }                    // ORD_RADNR - Row Number
    public string? OrdArtn { get; set; }                 // ORD_ARTN - Article Number
    public string? OrdArtb { get; set; }                 // ORD_ARTB - Article Description
    public double OrdAnta { get; set; }                  // ORD_ANTA - Quantity
    public double OrdInpris { get; set; }                // ORD_INPRIS - Unit Price
    public double OrdRaba { get; set; }                  // ORD_RABA - Discount
    public double OrdMoms { get; set; }                  // ORD_MOMS - VAT
    public string? OrdTyp { get; set; }                   // ORD_TYP - Type
    public string? OrdKod { get; set; }                  // ORD_KOD - Code
    public double OrdSummaexkl { get; set; }             // ORD_SUMMAEXKL - Sum Excluding VAT
    public DateTime? OrdCreatedAt { get; set; }          // ORD_CREATED_AT - Created At
    public DateTime? OrdUpdatedAt { get; set; }          // ORD_UPDATED_AT - Updated At
    
    // Matched keyword and category from ORD_ARTB analysis
    public string? MatchedKeyword { get; set; }          // Matched keyword from keyword categories
    public string? MatchedCategory { get; set; }         // Matched category from keyword categories
}
