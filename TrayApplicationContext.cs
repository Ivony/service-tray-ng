using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace service_tray_ng;

public sealed class TrayApplicationContext : ApplicationContext
{
    private sealed class ServiceUi
    {
        public required ServiceProfile Profile { get; init; }
        public required ServiceConfig Config { get; init; }
        public required ManagedService Service { get; init; }
        public required NotifyIcon Icon { get; init; }
        public required ToolStripMenuItem StatusItem { get; init; }
        public required ToolStripMenuItem StartItem { get; init; }
        public required ToolStripMenuItem StopItem { get; init; }
        public required ToolStripMenuItem RestartItem { get; init; }
        public required ToolStripMenuItem PortItem { get; init; }
        public required ToolStripMenuItem AutoPortItem { get; init; }
        public required ToolStripMenuItem AutoStartItem { get; init; }
        public required ContextMenuStrip Menu { get; init; }
    }

    private readonly List<ServiceUi> _services = [];
    private readonly AppConfig _config;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly Mutex _singleInstance;
    private bool _disposed;

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "ServiceTrayNg";

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public TrayApplicationContext()
    {
        _singleInstance = new Mutex(true, "service-tray-ng-single-instance", out var isNew);
        if (!isNew)
        {
            throw new InvalidOperationException(Strings.Get("App.AlreadyRunning.Message"));
        }

        _config = ConfigStore.LoadForAllProfiles();
        _config.Services ??= new Dictionary<string, ServiceConfig>();

        var startOnLoginItem = new ToolStripMenuItem(Strings.Get("Menu.StartOnLogin"), null, OnToggleStartOnLogin)
        {
            Checked = IsStartOnLoginEnabled(),
        };
        var exitItem = new ToolStripMenuItem(Strings.Get("Menu.Exit"), null, (_, _) => ExitThread());

        foreach (var profile in ServiceProfiles.All)
        {
            if (!_config.Services.TryGetValue(profile.Key, out var serviceConfig) || serviceConfig is null)
            {
                serviceConfig = new ServiceConfig { Hostname = "127.0.0.1", Port = profile.DefaultPort };
                _config.Services[profile.Key] = serviceConfig;
            }

            var service = new ManagedService(profile, serviceConfig);
            service.StatusChanged += (_, status) => OnStatusChanged(profile, serviceConfig, status);
            service.PortChanged += (_, _) => OnServicePortChanged(profile, serviceConfig);

            var statusItem = new ToolStripMenuItem(Strings.Get("Menu.Status.Stopped"), null, (_, _) => OpenServer(profile, serviceConfig, service));
            var startItem = new ToolStripMenuItem(Strings.Get("Menu.Start"), null, (_, _) => RunTask(service.StartAsync));
            var stopItem = new ToolStripMenuItem(Strings.Get("Menu.Stop"), null, (_, _) => RunTask(service.StopAsync));
            var restartItem = new ToolStripMenuItem(Strings.Get("Menu.Restart"), null, (_, _) => RunTask(service.RestartAsync));
            var autoStartItem = new ToolStripMenuItem(Strings.Get("Menu.StartServiceOnLaunch"), null, (_, _) => OnToggleAutoStart(serviceConfig))
            {
                Checked = serviceConfig.AutoStartService,
            };
            var portItem = new ToolStripMenuItem(string.Format(Strings.Get("Menu.Port"), serviceConfig.Port), null, (_, _) => OnChangePort(serviceConfig, service));
            var autoPortItem = new ToolStripMenuItem(Strings.Get("Menu.AutoSwitchPort"), null, (_, _) => OnToggleAutoPort(serviceConfig))
            {
                Checked = serviceConfig.AutoChangePort,
            };
            var openLogsItem = new ToolStripMenuItem(Strings.Get("Menu.OpenLogFolder"), null, (_, _) => OpenFolder(service.LogDirectory));
            var openConfigItem = new ToolStripMenuItem(Strings.Get("Menu.OpenConfig"), null, (_, _) => OpenFolder(Path.GetDirectoryName(ConfigStore.ConfigFilePath)!));

            var menu = new ContextMenuStrip();
            menu.Items.AddRange(
            [
                statusItem,
                new ToolStripSeparator(),
                startItem,
                stopItem,
                restartItem,
                new ToolStripSeparator(),
                autoStartItem,
                portItem,
                autoPortItem,
                new ToolStripSeparator(),
                openLogsItem,
                openConfigItem,
                new ToolStripSeparator(),
                startOnLoginItem,
                exitItem,
            ]);

            var icon = new NotifyIcon
            {
                Text = ServiceName(profile),
                Icon = MakeStatusIcon(profile, ServiceStatus.Stopped),
                ContextMenuStrip = menu,
                Visible = true,
            };
            icon.DoubleClick += (_, _) => OpenServer(profile, serviceConfig, service);

            _services.Add(new ServiceUi
            {
                Profile = profile,
                Config = serviceConfig,
                Service = service,
                Icon = icon,
                StatusItem = statusItem,
                StartItem = startItem,
                StopItem = stopItem,
                RestartItem = restartItem,
                PortItem = portItem,
                AutoPortItem = autoPortItem,
                AutoStartItem = autoStartItem,
                Menu = menu,
            });
        }

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();

        foreach (var ui in _services)
        {
            if (ui.Config.AutoStartService)
            {
                RunTask(ui.Service.StartAsync);
            }
        }
    }

    private async Task PollAsync()
    {
        foreach (var ui in _services)
        {
            var healthy = await ui.Service.IsHealthyAsync();
            var running = ui.Service.Status == ServiceStatus.Running && healthy;
            ui.StartItem.Enabled = !running;
            ui.StopItem.Enabled = running;
            ui.RestartItem.Enabled = true;
            ui.StatusItem.Text = running
                ? string.Format(Strings.Get("Menu.Status.Running"), ui.Config.Hostname, ui.Config.Port)
                : ui.Service.Status == ServiceStatus.Error
                    ? Strings.Get("Menu.Status.Error")
                    : ui.Service.Status == ServiceStatus.Starting
                        ? Strings.Get("Menu.Status.Starting")
                        : ui.Service.Status == ServiceStatus.Stopping
                            ? Strings.Get("Menu.Status.Stopping")
                            : Strings.Get("Menu.Status.Stopped");

            ui.Icon.Icon = MakeStatusIcon(
                ui.Profile,
                running ? ServiceStatus.Running
                : ui.Service.Status == ServiceStatus.Error ? ServiceStatus.Error
                : ui.Service.Status);
        }
    }

    private static void RunTask(Func<Task> action)
    {
        _ = Task.Run(action);
    }

    private void OnStatusChanged(ServiceProfile profile, ServiceConfig config, ServiceStatus status)
    {
        if (_disposed)
            return;
        _trayIconSafe(() =>
        {
            _services.FirstOrDefault(ui => ui.Profile == profile)?.Icon.ShowBalloonTip(2000, ServiceName(profile),
                status switch
                {
                    ServiceStatus.Running => string.Format(Strings.Get("Balloon.Running"), config.Hostname, config.Port),
                    ServiceStatus.Stopped => Strings.Get("Balloon.Stopped"),
                    ServiceStatus.Error => Strings.Get("Balloon.Error"),
                    _ => null,
                } ?? Strings.Get("Balloon.StateChanged"),
                ToolTipIcon.Info);
        });
    }

    private void _trayIconSafe(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // balloon tips can race with dispose; ignore
        }
    }

    private void OnToggleStartOnLogin(object? sender, EventArgs e)
    {
        var item = (ToolStripMenuItem)sender!;
        var enable = !item.Checked;
        SetStartOnLogin(enable);
        item.Checked = enable;
    }

    private void OnToggleAutoStart(ServiceConfig config)
    {
        config.AutoStartService = !config.AutoStartService;
        var ui = _services.First(u => u.Config == config);
        ui.AutoStartItem.Checked = config.AutoStartService;
        ConfigStore.Save(_config);
    }

    private void OnChangePort(ServiceConfig config, ManagedService service)
    {
        using var dialog = new PortDialog(config.Port);
        if (dialog.ShowDialog() != DialogResult.OK)
            return;
        config.Port = dialog.Port;
        _services.First(u => u.Config == config).PortItem.Text = string.Format(Strings.Get("Menu.Port"), config.Port);
        ConfigStore.Save(_config);
        if (service.Status == ServiceStatus.Running)
        {
            RunTask(service.RestartAsync);
        }
    }

    private void OnToggleAutoPort(ServiceConfig config)
    {
        config.AutoChangePort = !config.AutoChangePort;
        var ui = _services.First(u => u.Config == config);
        ui.AutoPortItem.Checked = config.AutoChangePort;
        ConfigStore.Save(_config);
    }

    private void OnServicePortChanged(ServiceProfile profile, ServiceConfig config)
    {
        _services.First(ui => ui.Profile == profile).PortItem.Text = string.Format(Strings.Get("Menu.Port"), config.Port);
        ConfigStore.Save(_config);
    }

    private static bool IsStartOnLoginEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) is not null;
    }

    private static void SetStartOnLogin(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
            return;

        var exe = Environment.ProcessPath;
        if (enable && exe is not null)
        {
            key.SetValue(RunValue, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(RunValue, throwOnMissingValue: false);
        }
    }

    private static void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignore
        }
    }

    private void OpenServer(ServiceProfile profile, ServiceConfig config, ManagedService service)
    {
        if (service.Status != ServiceStatus.Running)
        {
            _services.First(ui => ui.Profile == profile).Icon.ShowBalloonTip(2000, ServiceName(profile), Strings.Get("Balloon.NotRunning"), ToolTipIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"http://{config.Hostname}:{config.Port}",
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignore
        }
    }

    private static string ServiceName(ServiceProfile profile)
    {
        return profile.Key switch
        {
            "opencode" => Strings.Get("ServiceName.OpenCode"),
            "dsh" => Strings.Get("ServiceName.Dsh"),
            _ => profile.DisplayName,
        };
    }

    private static Icon MakeStatusIcon(ServiceProfile profile, ServiceStatus status)
    {
        const int size = 64;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        const int margin = 4;
        using var logo = LoadLogo(profile);
        g.DrawImage(logo, new Rectangle(margin, margin, size - margin * 2, size - margin * 2));

        var dark = IsSystemDarkMode();
        var (lightR, lightG, lightB) = status switch
        {
            ServiceStatus.Running => dark ? (74, 222, 128) : (22, 101, 52),
            ServiceStatus.Starting or ServiceStatus.Stopping => dark ? (250, 204, 21) : (161, 98, 7),
            ServiceStatus.Error => dark ? (248, 113, 113) : (185, 28, 28),
            _ => dark ? (226, 232, 240) : (100, 116, 139),
        };

        const float dotRadius = 10f;
        var dotCenter = new PointF(size - dotRadius - 4f, size - dotRadius - 4f);
        using (var ringBrush = new SolidBrush(dark ? Color.FromArgb(40, 40, 40) : Color.White))
        {
            g.FillEllipse(ringBrush, dotCenter.X - dotRadius, dotCenter.Y - dotRadius, dotRadius * 2, dotRadius * 2);
        }
        using (var dotBrush = new SolidBrush(Color.FromArgb(lightR, lightG, lightB)))
        {
            g.FillEllipse(dotBrush, dotCenter.X - dotRadius + 2.4f, dotCenter.Y - dotRadius + 2.4f, (dotRadius - 2.4f) * 2, (dotRadius - 2.4f) * 2);
        }

        var hIcon = bmp.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(hIcon);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static Bitmap LoadLogo(ServiceProfile profile)
    {
        var resourceName = IsSystemDarkMode()
            ? profile.LogoDarkResource
            : profile.LogoLightResource;
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded logo resource not found: {resourceName}");
        return new Bitmap(stream);
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("SystemUsesLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;

        if (disposing)
        {
            _pollTimer.Stop();
            _pollTimer.Dispose();
            foreach (var ui in _services)
            {
                ui.Service.Dispose();
                ui.Icon.Visible = false;
                ui.Icon.Dispose();
            }
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.Dispose(disposing);
    }
}
