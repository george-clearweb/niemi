using Microsoft.Extensions.Configuration;

namespace Niemi.Services;

public class DatabaseConfigService : IDatabaseConfigService
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _connectionStrings;

    public DatabaseConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionStrings = new Dictionary<string, string>
        {
            { "NIE2V", _configuration.GetConnectionString("ConnectionStringNIE2V") ?? throw new ArgumentNullException("ConnectionStringNIE2V is missing") },
            { "NIEM3", _configuration.GetConnectionString("ConnectionStringNIEM3") ?? throw new ArgumentNullException("ConnectionStringNIEM3 is missing") },
            { "NIEM4", _configuration.GetConnectionString("ConnectionStringNIEM4") ?? throw new ArgumentNullException("ConnectionStringNIEM4 is missing") },
            { "NIEM5", _configuration.GetConnectionString("ConnectionStringNIEM5") ?? throw new ArgumentNullException("ConnectionStringNIEM5 is missing") },
            { "NIEM6", _configuration.GetConnectionString("ConnectionStringNIEM6") ?? throw new ArgumentNullException("ConnectionStringNIEM6 is missing") },
            { "NIEM7", _configuration.GetConnectionString("ConnectionStringNIEM7") ?? throw new ArgumentNullException("ConnectionStringNIEM7 is missing") },
            { "NIEMI", _configuration.GetConnectionString("ConnectionStringNIEMI") ?? throw new ArgumentNullException("ConnectionStringNIEMI is missing") }
        };
    }

    public string GetConnectionString(string? environment = null)
    {
        if (string.IsNullOrEmpty(environment))
        {
            // Default to NIE2V if no environment specified
            environment = "NIE2V";
        }

        if (!_connectionStrings.ContainsKey(environment))
        {
            throw new ArgumentException($"Database environment '{environment}' is not configured or available.");
        }

        return _connectionStrings[environment];
    }

    public string[] GetAvailableEnvironments()
    {
        // Return the environments that are actually configured in the connection strings
        return _connectionStrings.Keys.ToArray();
    }

    public string GetCurrentEnvironment()
    {
        // Since we're now querying all environments by default, return the first available
        return GetAvailableEnvironments().First();
    }
}
