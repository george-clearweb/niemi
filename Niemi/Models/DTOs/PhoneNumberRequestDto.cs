namespace Niemi.Models.DTOs;

public class PhoneNumberRequestDto
{
    public string CallId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? OrhStat { get; set; }
    public string? CustomerType { get; set; }
    public bool? Invoiced { get; set; }
}
