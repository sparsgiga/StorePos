using System.IO;
using System.Text.Json;

namespace StorePos.Desktop.Configuration;

public static class DesktopConfiguration
{
#if DEBUG
    private const string EnvironmentName = "Development";
#else
    private const string EnvironmentName = "Production";
#endif

    public static Uri LoadApiBaseAddress()
    {
        var configurationFileName = $"appsettings.{EnvironmentName}.json";
        var configurationPath = Path.Combine(AppContext.BaseDirectory, configurationFileName);

        if (!File.Exists(configurationPath))
        {
            throw new InvalidOperationException(
                $"Desktop configuration file '{configurationFileName}' was not found.");
        }

        using var configurationStream = File.OpenRead(configurationPath);
        var settings = JsonSerializer.Deserialize<DesktopSettings>(
            configurationStream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (!Uri.TryCreate(settings?.Api?.BaseUrl, UriKind.Absolute, out var apiBaseAddress))
        {
            throw new InvalidOperationException("API base URL is missing or invalid.");
        }

        return apiBaseAddress;
    }

    private sealed class DesktopSettings
    {
        public ApiSettings? Api { get; init; }
    }

    private sealed class ApiSettings
    {
        public string? BaseUrl { get; init; }
    }
}
