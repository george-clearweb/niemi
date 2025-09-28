namespace Niemi.Services;

public interface IScheduledOrderService
{
    Task ProcessDailyOrdersAsync(CancellationToken cancellationToken = default);
}
