namespace service_tray_ng;

public sealed class AppConfig
{
    public Dictionary<string, ServiceConfig> Services { get; set; } = new();
}
