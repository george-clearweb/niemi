namespace Niemi.Models.DTOs;

public class HttpBinResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Response { get; set; }
}
