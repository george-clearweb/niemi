using System.ComponentModel.DataAnnotations;

namespace Niemi.Models.DTOs;

public class RuleIoSubscriberDto
{
    [Required]
    public string Email { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public string? Language { get; set; }
    
    public List<RuleIoFieldDto> Fields { get; set; } = new();
}

public class RuleIoFieldDto
{
    [Required]
    public string Key { get; set; } = string.Empty;
    
    public object Value { get; set; } = string.Empty;
    
    [Required]
    public string Type { get; set; } = string.Empty;
}

public class RuleIoSubscribersRequestDto
{
    public bool UpdateOnDuplicate { get; set; } = true;
    
    public List<string> Tags { get; set; } = new();
    
    [Required]
    public List<RuleIoSubscriberDto> Subscribers { get; set; } = new();
}

public class RuleIoSubscribersResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<RuleIoSubscriberResultDto>? Results { get; set; }
}

public class RuleIoSubscriberResultDto
{
    public string? Email { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
