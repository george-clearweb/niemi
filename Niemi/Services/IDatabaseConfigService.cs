namespace Niemi.Services;

public interface IDatabaseConfigService
{
    string GetConnectionString(string? environment = null);
    string[] GetAvailableEnvironments();
    string GetCurrentEnvironment();
}
