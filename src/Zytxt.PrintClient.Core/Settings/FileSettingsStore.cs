using System.Text.Json;

namespace Zytxt.PrintClient.Core.Settings;

public sealed class FileSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public FileSettingsStore(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public PrintClientSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new PrintClientSettings();
        }

        var json = File.ReadAllText(_settingsPath);
        return JsonSerializer.Deserialize<PrintClientSettings>(json, JsonOptions) ?? new PrintClientSettings();
    }

    public void Save(PrintClientSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
