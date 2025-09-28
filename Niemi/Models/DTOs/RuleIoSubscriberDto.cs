using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Niemi.Models.DTOs;

public class RuleIoSubscriberDto
{
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
    
    [JsonPropertyName("language")]
    public string? Language { get; set; }
    
    [JsonPropertyName("fields")]
    public List<RuleIoFieldDto> Fields { get; set; } = new();
}

public class RuleIoFieldDto
{
    [Required]
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;
    
    [Required]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class RuleIoSubscribersRequestDto
{
    [JsonPropertyName("update_on_duplicate")]
    public bool UpdateOnDuplicate { get; set; } = true;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [Required]
    [JsonPropertyName("subscribers")]
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
