namespace Niemi.Services;

using Niemi.Models;

public interface ILaginkService
{
    Task<IEnumerable<LaginkHd>> GetLaginkDataAsync(DateTime fromDate, DateTime toDate, int skip, int take);
    Task<IEnumerable<LaginkHd>> GetInvoicedLaginkDataAsync(DateTime fromDate, DateTime toDate, int skip, int take);
} 