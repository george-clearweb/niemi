namespace Niemi.Models.DTOs;

public class PhoneNumberOrderResultDto
{
    // Input tracking information
    public string CallId { get; set; } = string.Empty;
    public string InputPhoneNumber { get; set; } = string.Empty;
    
    // Order data
    public OrdhuvDto Order { get; set; } = new OrdhuvDto();
}
