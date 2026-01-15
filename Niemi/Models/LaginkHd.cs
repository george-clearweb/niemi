namespace Niemi.Models;

public class LaginkHd
{
    public string Database { get; set; } = string.Empty; // Database environment (NIE2V, NIEM3, etc.)
    public long OrderNr { get; set; }
    public DateTime OrderDatum { get; set; }
    public string? Lev { get; set; }
    public DateTime InlevDatum { get; set; }
    public decimal Summa { get; set; }
    public string? OrderTyp { get; set; }
    public string? LevRef { get; set; }
    public string? KundRef { get; set; }
    public string? Inlev { get; set; }
    public string? Best { get; set; }
    public string? CorrelationId { get; set; }
    public long EOrderId { get; set; }
    public long DeliveryCode { get; set; }
    public string? LaginkCreatedBy { get; set; }
    public DateTime LaginkCreatedAt { get; set; }
    public string? LaginkUpdatedBy { get; set; }
    public DateTime LaginkUpdatedAt { get; set; }
    public List<LaginkRd> Rows { get; set; } = new List<LaginkRd>();
} 