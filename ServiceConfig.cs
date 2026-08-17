namespace service_tray_ng;

public sealed class ServiceConfig
{
    public string ExecutablePath { get; set; } = "";
    public string Hostname { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public bool AutoStartService { get; set; } = false;
    public bool AutoChangePort { get; set; } = true;
    public string WorkingDirectory { get; set; } = "";
}
