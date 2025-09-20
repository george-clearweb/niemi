using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IOrdhuvOptimizedService
{
    Task<IEnumerable<OrdhuvDto>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null);
}
