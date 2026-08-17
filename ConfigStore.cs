using System.Text.Json;

namespace service_tray_ng;

public static class ConfigStore
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "service-tray-ng", "config.json");

    internal static string? ConfigPathOverride { get; set; }

    internal static string ResolveConfigPath() => ConfigPathOverride ?? ConfigPath;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppConfig Load()
    {
        return LoadFrom(ResolveConfigPath());
    }

    internal static AppConfig LoadFrom(string configPath)
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, Options);
                if (config is not null)
                    return config;
            }
        }
        catch
        {
            // fall through to defaults
        }
        return new AppConfig();
    }

    /// <summary>Returns the saved config with a ServiceConfig entry present for every known profile.</summary>
    public static AppConfig LoadForAllProfiles()
    {
        var config = Load();
        foreach (var profile in ServiceProfiles.All)
        {
            if (!config.Services.TryGetValue(profile.Key, out var service) || service is null)
            {
                service = new ServiceConfig
                {
                    Hostname = "127.0.0.1",
                    Port = profile.DefaultPort,
                };
                config.Services[profile.Key] = service;
            }
            else if (service.Port == 0)
            {
                service.Port = profile.DefaultPort;
            }
        }
        return config;
    }

    public static void Save(AppConfig config)
    {
        SaveTo(config, ResolveConfigPath());
    }

    internal static void SaveTo(AppConfig config, string configPath)
    {
        var dir = Path.GetDirectoryName(configPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, Options));
    }

    public static string ConfigFilePath => ResolveConfigPath();
}
