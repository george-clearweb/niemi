using Niemi.Models.DTOs;

namespace Niemi.Services;

public interface IOrdhuvOptimizedService
{
    Task<IEnumerable<OrdhuvDto>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null);
    Task<IEnumerable<OrdhuvDto>> GetOrdersByLicensePlatesAsync(List<LicensePlateItem> licensePlates, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null, bool? invoiced = null, DateTime? fromDate = null);
    Task<IEnumerable<OrdhuvDto>> GetOrdersByDateAsync(DateTime fromDate, DateTime toDate, string? environment = null, string[]? environments = null, string? orhStat = null, string? customerType = null, bool? invoiced = null);
}
