using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IOrdhuvOptimizedService
{
    Task<IEnumerable<OrdhuvDto>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate);
}
