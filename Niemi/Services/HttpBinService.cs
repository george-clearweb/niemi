using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Niemi.Models.DTOs;

namespace Niemi.Services;

public class HttpBinService : IHttpBinService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpBinService> _logger;
    private readonly string _baseUrl = "https://httpbin.org";

    public HttpBinService(
        HttpClient httpClient,
        ILogger<HttpBinService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<HttpBinResponseDto> SendToHttpBinAsync(RuleIoSubscribersRequestDto request)
    {
        try
        {
            _logger.LogInformation("Sending request to httpbin.org/anything with {Count} subscribers", request.Subscribers.Count);

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("HttpBin request payload: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/anything", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("HttpBin API response: {StatusCode} - {Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<object>(responseContent);

                return new HttpBinResponseDto
                {
                    Success = true,
                    Message = "Request sent successfully to httpbin.org",
                    Response = result
                };
            }
            else
            {
                _logger.LogError("HttpBin API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                
                return new HttpBinResponseDto
                {
                    Success = false,
                    Message = $"API error: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed when calling httpbin.org");
            return new HttpBinResponseDto
            {
                Success = false,
                Message = $"HTTP request failed: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout when calling httpbin.org");
            return new HttpBinResponseDto
            {
                Success = false,
                Message = "Request timeout"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when calling httpbin.org");
            return new HttpBinResponseDto
            {
                Success = false,
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
