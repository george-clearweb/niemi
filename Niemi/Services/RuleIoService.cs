using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class RuleIoService : IRuleIoService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RuleIoService> _logger;
    private readonly string _baseUrl;
    private readonly string _bearerToken;

    public RuleIoService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RuleIoService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        _baseUrl = _configuration["RuleIo:BaseUrl"] ?? throw new InvalidOperationException("RuleIo:BaseUrl not configured");
        _bearerToken = _configuration["RuleIo:BearerToken"] ?? throw new InvalidOperationException("RuleIo:BearerToken not configured");
        
        // Configure HttpClient
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<RuleIoSubscribersResponseDto> CreateSubscribersAsync(RuleIoSubscribersRequestDto request)
    {
        try
        {
            _logger.LogInformation("Creating {Count} subscribers in Rule.io", request.Subscribers.Count);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Rule.io request payload: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_baseUrl}/subscribers", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Rule.io API response: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RuleIoSubscribersResponseDto>(responseContent);

                return result ?? new RuleIoSubscribersResponseDto
                {
                    Success = true,
                    Message = "Subscribers created successfully"
                };
            }
            else
            {
                _logger.LogError("Rule.io API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                
                return new RuleIoSubscribersResponseDto
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when calling Rule.io API");
            return new RuleIoSubscribersResponseDto
            {
                Success = false,
                Message = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout when calling Rule.io API");
            return new RuleIoSubscribersResponseDto
        {
            Success = false,
            Message = "Request timeout"
        };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when calling Rule.io API");
            return new RuleIoSubscribersResponseDto
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
