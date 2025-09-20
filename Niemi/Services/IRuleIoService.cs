using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IRuleIoService
{
    Task<RuleIoSubscribersResponseDto> CreateSubscribersAsync(RuleIoSubscribersRequestDto request);
}
