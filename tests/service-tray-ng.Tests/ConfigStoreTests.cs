using Xunit;

namespace service_tray_ng.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _tempFile;

    public ConfigStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), "dsh-service-tray-tests", "cfg", $"{Guid.NewGuid():N}.json");
        ConfigStore.ConfigPathOverride = _tempFile;
    }

    public void Dispose()
    {
        ConfigStore.ConfigPathOverride = null;
    }

    [Fact]
    public void Load_WhenNoFile_ReturnsEmptyAppConfig()
    {
        var config = ConfigStore.Load();
        Assert.NotNull(config.Services);
        Assert.Empty(config.Services);
    }

    [Fact]
    public void Load_WhenInvalidJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFile)!);
        File.WriteAllText(_tempFile, "{ not valid json !!!");
        var config = ConfigStore.Load();
        Assert.NotNull(config.Services);
        Assert.Empty(config.Services);
    }

    [Fact]
    public void LoadForAllProfiles_CreatesEntriesWithDefaultPorts()
    {
        var config = ConfigStore.LoadForAllProfiles();

        var opencode = Assert.Single(config.Services, kv => kv.Key == "opencode");
        Assert.Equal(4096, opencode.Value.Port);
        Assert.Equal("127.0.0.1", opencode.Value.Hostname);

        var dsh = Assert.Single(config.Services, kv => kv.Key == "dsh");
        Assert.Equal(3080, dsh.Value.Port);
        Assert.Equal("127.0.0.1", dsh.Value.Hostname);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsValues()
    {
        var original = new AppConfig
        {
            Services = new Dictionary<string, ServiceConfig>
            {
                ["dsh"] = new ServiceConfig { Port = 5555, Hostname = "0.0.0.0", AutoChangePort = false, RememberChangedPort = true },
            },
        };

        ConfigStore.Save(original);

        var loaded = ConfigStore.Load();
        var dsh = loaded.Services["dsh"];
        Assert.Equal(5555, dsh.Port);
        Assert.Equal("0.0.0.0", dsh.Hostname);
        Assert.False(dsh.AutoChangePort);
        Assert.True(dsh.RememberChangedPort);
    }

    [Fact]
    public void Load_WhenRememberChangedPortMissing_DefaultsToFalse()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tempFile)!);
        File.WriteAllText(_tempFile, """{ "Services": { "dsh": { "Port": 5555 } } }""");

        var loaded = ConfigStore.Load();
        Assert.False(loaded.Services["dsh"].RememberChangedPort);
    }

    [Fact]
    public void Save_ThenLoadForAllProfiles_PreservesSavedPortButAddsMissingProfiles()
    {
        var original = new AppConfig
        {
            Services = new Dictionary<string, ServiceConfig>
            {
                ["dsh"] = new ServiceConfig { Port = 7777, Hostname = "127.0.0.1" },
            },
        };
        ConfigStore.Save(original);

        var loaded = ConfigStore.LoadForAllProfiles();

        Assert.Equal(7777, loaded.Services["dsh"].Port);
        Assert.True(loaded.Services.ContainsKey("opencode"));
        Assert.Equal(4096, loaded.Services["opencode"].Port);
    }
}
