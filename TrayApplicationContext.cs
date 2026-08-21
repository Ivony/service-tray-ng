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
        public required ToolStripMenuItem RememberPortItem { get; init; }
        public required ToolStripMenuItem AutoStartItem { get; init; }
        public required ToolStripMenuItem StartOnLoginItem { get; init; }
        public required ContextMenuStrip Menu { get; init; }
    }

    private readonly List<ServiceUi> _services = [];
    private readonly AppConfig _config;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly Mutex _singleInstance;
    private bool _disposed;
    private bool _exitStarted;

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public TrayApplicationContext()
    {
        _singleInstance = new Mutex(true, Edition.MutexName, out var isNew);
        if (!isNew)
        {
            throw new InvalidOperationException(Strings.Get("App.AlreadyRunning.Message"));
        }

        _config = ConfigStore.LoadForAllProfiles();
        _config.Services ??= new Dictionary<string, ServiceConfig>();

        foreach (var profile in ServiceProfiles.All)
        {
            if (!_config.Services.TryGetValue(profile.Key, out var serviceConfig) || serviceConfig is null)
            {
                serviceConfig = new ServiceConfig { Hostname = "127.0.0.1", Port = profile.DefaultPort };
                _config.Services[profile.Key] = serviceConfig;
            }

            var service = new ManagedService(profile, serviceConfig);
            service.StatusChanged += (_, status) => OnStatusChanged(profile, serviceConfig, status);
            service.PortChanged += (_, args) => OnServicePortChanged(profile, serviceConfig, args);

            var statusItem = new ToolStripMenuItem(Strings.Get("Menu.Status.Stopped"), null, (_, _) => OpenServer(profile, serviceConfig, service));
            var startItem = new ToolStripMenuItem(Strings.Get("Menu.Start"));
            var stopItem = new ToolStripMenuItem(Strings.Get("Menu.Stop"), null, (_, _) => RunTask(service.StopAsync));
            var restartItem = new ToolStripMenuItem(Strings.Get("Menu.Restart"));
            var autoStartItem = new ToolStripMenuItem(Strings.Get("Menu.StartServiceOnLaunch"), null, (_, _) => OnToggleAutoStart(serviceConfig))
            {
                Checked = serviceConfig.AutoStartService,
            };
            var portItem = new ToolStripMenuItem(string.Format(Strings.Get("Menu.Port"), serviceConfig.Port), null, (_, _) => OnChangePort(serviceConfig, service));
            var autoPortItem = new ToolStripMenuItem(Strings.Get("Menu.AutoSwitchPort"), null, (_, _) => OnToggleAutoPort(serviceConfig))
            {
                Checked = serviceConfig.AutoChangePort,
            };
            var rememberPortItem = new ToolStripMenuItem(Strings.Get("Menu.RememberChangedPort"), null, (_, _) => OnToggleRememberPort(serviceConfig))
            {
                Checked = serviceConfig.RememberChangedPort,
                Enabled = serviceConfig.AutoChangePort,
            };
            var openLogsItem = new ToolStripMenuItem(Strings.Get("Menu.OpenLogFolder"), null, (_, _) => OpenFolder(service.LogDirectory));
            var openConfigItem = new ToolStripMenuItem(Strings.Get("Menu.OpenConfig"), null, (_, _) => OpenFolder(Path.GetDirectoryName(ConfigStore.ConfigFilePath)!));
            var startOnLoginItem = new ToolStripMenuItem(Strings.Get("Menu.StartOnLogin"), null, OnToggleStartOnLogin)
            {
                Checked = IsStartOnLoginEnabled(),
            };
            var exitItem = new ToolStripMenuItem(Strings.Get("Menu.Exit"), null, (_, _) => BeginExit());

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
                rememberPortItem,
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

            var serviceUi = new ServiceUi
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
                RememberPortItem = rememberPortItem,
                AutoStartItem = autoStartItem,
                StartOnLoginItem = startOnLoginItem,
                Menu = menu,
            };
            _services.Add(serviceUi);
            startItem.Click += (_, _) => RunTask(() => StartServiceAsync(serviceUi));
            restartItem.Click += (_, _) => RunTask(() => RestartServiceAsync(serviceUi));
        }

        _pollTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _pollTimer.Tick += async (_, _) => await PollAsync();
        _pollTimer.Start();

        foreach (var ui in _services)
        {
            if (ui.Config.AutoStartService)
            {
                RunTask(() => StartServiceAsync(ui));
            }
        }
    }

    private async Task PollAsync()
    {
        if (_disposed)
            return;

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

    private async Task StartServiceAsync(ServiceUi ui)
    {
        if (ui.Service.Status == ServiceStatus.Running)
            return;

        var externalProcesses = await ui.Service.FindExternalProcessesAsync();
        if (externalProcesses.Count > 0)
        {
            var action = await HandleExternalProcessAsync(ui, externalProcesses);
            if (action is null or ExternalServiceAction.Attach)
                return;

            if (ManagedService.FindListeningProcesses(ui.Config.Port).Count > 0)
                return;
        }

        await ui.Service.StartAsync();
    }

    private async Task RestartServiceAsync(ServiceUi ui)
    {
        await ui.Service.StopAsync();
        await StartServiceAsync(ui);
    }

    private async Task<ExternalServiceAction?> HandleExternalProcessAsync(
        ServiceUi ui,
        IReadOnlyList<ListeningProcess> processes)
    {
        var defaultNewPort = ManagedService.FindAvailablePort(
            ui.Config.Hostname, ui.Config.Port < 65535 ? ui.Config.Port + 1 : 1);
        using var dialog = new ExternalProcessDialog(
            ServiceName(ui.Profile), ui.Config.Hostname, ui.Config.Port, processes, defaultNewPort);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            BeginExit();
            return null;
        }

        switch (dialog.Action)
        {
            case ExternalServiceAction.Attach:
                // The dialog exposes the exact instance the user picked to take over.
                var selected = dialog.SelectedProcess;
                if (selected is not null && ui.Service.TryAttach(selected.ProcessId))
                {
                    // Optionally close every other detected instance so the ports they
                    // hold are freed. Only after a successful takeover, so a failed
                    // attach never takes other instances down.
                    foreach (var other in dialog.ProcessesToClose)
                    {
                        ManagedService.KillProcessTree(other.ProcessId);
                    }
                    ui.Config.Port = selected.Port;
                    ui.PortItem.Text = string.Format(Strings.Get("Menu.Port"), ui.Config.Port);
                    ConfigStore.Save(_config);
                    ui.Icon.ShowBalloonTip(2000, ServiceName(ui.Profile),
                        string.Format(Strings.Get("Balloon.Attached"), ui.Config.Hostname, ui.Config.Port),
                        ToolTipIcon.Info);
                }
                else
                {
                    ui.Icon.ShowBalloonTip(2000, ServiceName(ui.Profile), Strings.Get("Balloon.AttachFailed"), ToolTipIcon.Warning);
                    return null;
                }
                return ExternalServiceAction.Attach;

            case ExternalServiceAction.Kill:
                var killFailed = false;
                foreach (var process in processes)
                {
                    killFailed |= !ManagedService.KillProcessTree(process.ProcessId);
                }

                for (var attempt = 0; attempt < 10 && ManagedService.FindListeningProcesses(ui.Config.Port).Count > 0; attempt++)
                {
                    await Task.Delay(100);
                }

                if (killFailed || ManagedService.FindListeningProcesses(ui.Config.Port).Count > 0)
                {
                    ui.Icon.ShowBalloonTip(2500, ServiceName(ui.Profile),
                        Strings.Get("Balloon.KillFailed"), ToolTipIcon.Warning);
                }
                else
                {
                    // A failed or interrupted startup can still hold a process reference.
                    // Clear it so killing an external server cannot leave a stale Error state.
                    try
                    {
                        await ui.Service.StopAsync();
                    }
                    catch
                    {
                        // The external process has already been handled; the next poll will show its state.
                    }
                }
                return killFailed ? null : ExternalServiceAction.Kill;

            case ExternalServiceAction.StartNew:
                ui.Config.Port = dialog.NewPort;
                ui.PortItem.Text = string.Format(Strings.Get("Menu.Port"), ui.Config.Port);
                ConfigStore.Save(_config);
                return ExternalServiceAction.StartNew;
        }

        return null;
    }

    private void OnStatusChanged(ServiceProfile profile, ServiceConfig config, ServiceStatus status)
    {
        if (_disposed || status is ServiceStatus.Starting or ServiceStatus.Stopping)
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
        foreach (var ui in _services)
        {
            ui.StartOnLoginItem.Checked = enable;
        }
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
        RunTask(() => ChangePortAsync(config, service, dialog.Port));
    }

    private async Task ChangePortAsync(ServiceConfig config, ManagedService service, int port)
    {
        await service.ChangePortAsync(port);
        ConfigStore.Save(_config);
    }

    private void OnToggleAutoPort(ServiceConfig config)
    {
        config.AutoChangePort = !config.AutoChangePort;
        var ui = _services.First(u => u.Config == config);
        ui.AutoPortItem.Checked = config.AutoChangePort;
        // Remembering the switched port only makes sense while auto-switching is on.
        ui.RememberPortItem.Enabled = config.AutoChangePort;
        ConfigStore.Save(_config);
    }

    private void OnToggleRememberPort(ServiceConfig config)
    {
        config.RememberChangedPort = !config.RememberChangedPort;
        var ui = _services.First(u => u.Config == config);
        ui.RememberPortItem.Checked = config.RememberChangedPort;
        ConfigStore.Save(_config);
    }

    private void OnServicePortChanged(ServiceProfile profile, ServiceConfig config, PortChangedEventArgs args)
    {
        var ui = _services.First(ui => ui.Profile == profile);
        void UpdatePortItem()
        {
            ui.PortItem.Text = string.Format(Strings.Get("Menu.Port"), config.Port);
        }

        if (ui.Menu.IsHandleCreated && ui.Menu.InvokeRequired)
            ui.Menu.BeginInvoke(UpdatePortItem);
        else
            UpdatePortItem();

        // An auto-switched port is only persisted when the user opted in; a manual
        // change (ChangePortAsync) always persists.
        if (args.Persist)
            ConfigStore.Save(_config);
    }

    private static bool IsStartOnLoginEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(Edition.RunValueName) is not null;
    }

    private static void SetStartOnLogin(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null)
            return;

        var exe = Environment.ProcessPath;
        if (enable && exe is not null)
        {
            key.SetValue(Edition.RunValueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(Edition.RunValueName, throwOnMissingValue: false);
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

    private async void BeginExit()
    {
        if (_exitStarted || _disposed)
            return;
        _exitStarted = true;

        foreach (var ui in _services)
        {
            ui.Menu.Enabled = false;
            ui.Icon.ShowBalloonTip(1000, ServiceName(ui.Profile), Strings.Get("Dialog.Exit.Starting"), ToolTipIcon.Info);
        }

        var tasks = _services
            .Select(ui => (ServiceName(ui.Profile), (Func<Task>)ui.Service.StopAsync))
            .ToArray();
        using var dialog = new ExitProgressDialog(tasks);
        await dialog.RunAsync();
        ExitThread();
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
            }

            foreach (var ui in _services)
            {
                ui.Icon.Visible = false;
                ui.Icon.Dispose();
                ui.Menu.Dispose();
            }

            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.Dispose(disposing);
    }
}
