using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IHttpBinService
{
    Task<HttpBinResponseDto> SendToHttpBinAsync(RuleIoSubscribersRequestDto request);
}
