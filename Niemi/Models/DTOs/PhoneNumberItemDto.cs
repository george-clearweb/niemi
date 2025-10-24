namespace Niemi.Models.DTOs;

public class PhoneNumberItemDto
{
    public string CallId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? OverrideDatabase { get; set; }
    public DateTime? OverrideFromDate { get; set; }
    public DateTime? OverrideToDate { get; set; }
    public string? OverrideOrhStat { get; set; }
    public string? OverrideCustomerType { get; set; }
    public bool? OverrideInvoiced { get; set; }
}
