namespace Niemi.Services;

using Niemi.Models;

public interface IOrdhuvService
{
    Task<IEnumerable<Ordhuv>> GetOrdhuvDataAsync(int skip, int take);
    Task<Ordhuv?> GetOrdhuvByIdAsync(int orderNr);
    Task<IEnumerable<Ordhuv>> GetInvoicedOrdersByDateAsync(DateTime fromDate, DateTime toDate, int skip, int take);
    Task<IEnumerable<Ordhuv>> GetOrdersWithInvoicesByDateAsync(DateTime fromDate, DateTime toDate);
    Task<string> GetOrdhuvTableStructureAsync();
    Task<string> GetInvoiceIndividualTableStructureAsync();
    Task<string> GetFortnoxLogTableStructureAsync();
    Task<string> GetKunregTableStructureAsync();
}
