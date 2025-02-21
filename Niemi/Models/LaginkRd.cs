namespace Niemi.Models;

public class LaginkRd
{
    public long OrderNr { get; set; }
    public long RadNr { get; set; }
    public string? ArtNr { get; set; }
    public string? Ben { get; set; }
    public long Antal { get; set; }
    public decimal Pris { get; set; }
    public string? Lev { get; set; }
    public decimal Rad { get; set; }
    public long Rest { get; set; }
    public string? RadRef { get; set; }
    public string? Inlev { get; set; }
    public string? Best { get; set; }
    public string? BestFil { get; set; }
    public long Levererat { get; set; }
    public string? Lp { get; set; }
    public string? BestNr { get; set; }
    public decimal Summa { get; set; }
    public decimal Status { get; set; }
    public long OrdRadNr { get; set; }
    public string? Typ { get; set; }
    public long ItemExternalId { get; set; }
    public decimal Origin { get; set; }
    public string? LaginkrdCreatedBy { get; set; }
    public DateTime LaginkrdCreatedAt { get; set; }
    public string? LaginkrdUpdatedBy { get; set; }
    public DateTime LaginkrdUpdatedAt { get; set; }
} 