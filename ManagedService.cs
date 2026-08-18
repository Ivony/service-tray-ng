using System.Diagnostics;
using System.Net.Sockets;

namespace service_tray_ng;

public enum ServiceStatus
{
    Stopped,
    Running,
    Starting,
    Stopping,
    Error,
}

public sealed record ListeningProcess(int ProcessId, string ProcessName, string Endpoint);

public sealed class ManagedService : IDisposable
{
    private readonly ServiceProfile _profile;
    private readonly ServiceConfig _config;
    private Process? _process;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private ServiceStatus _status = ServiceStatus.Stopped;

    public string LogDirectory { get; }

    public event EventHandler<ServiceStatus>? StatusChanged;
    public event EventHandler? PortChanged;

    public ServiceConfig Config => _config;

    public ServiceStatus Status
    {
        get
        {
            lock (_lock)
            {
                if (_process is { HasExited: false } && _status == ServiceStatus.Running)
                    return ServiceStatus.Running;
                return _status;
            }
        }
    }

    public ManagedService(ServiceProfile profile, ServiceConfig config)
    {
        _profile = profile;
        _config = config;
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "service-tray-ng", "logs");
        Directory.CreateDirectory(LogDirectory);
        Log("Service controller initialized.");
    }

    private void Log(string message)
    {
        try
        {
            var path = Path.Combine(LogDirectory, $"{_profile.Key}-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId}] {message}{Environment.NewLine}");
        }
        catch
        {
            // logging must never break the service
        }
    }

    internal (string FileName, string? Prefix, bool UseCmdWrapper) ResolveCommand()
    {
        var configured = _config.ExecutablePath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (File.Exists(configured))
                return (configured, null, IsCmdFile(configured));
            var fromPath = FindOnPath(configured + ".cmd") ?? FindOnPath(configured);
            if (fromPath is not null)
                return (fromPath, null, IsCmdFile(fromPath));
        }

        foreach (var name in _profile.CommandNames)
        {
            var found = FindOnPath(name);
            if (found is not null)
                return (found, null, IsCmdFile(found));
        }

        if (!string.IsNullOrWhiteSpace(_profile.NpxPackage))
        {
            var npx = FindOnPath("npx.cmd") ?? FindOnPath("npx");
            if (npx is not null)
                return (npx, _profile.NpxPackage, true);
            return ("npx", _profile.NpxPackage, true);
        }

        var fallback = _profile.CommandNames[0];
        return (fallback, null, IsCmdFile(fallback));
    }

    internal static bool IsCmdFile(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase);
    }

    internal static string? FindOnPath(string exe)
    {
        foreach (var dir in Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [])
        {
            if (string.IsNullOrWhiteSpace(dir))
                continue;
            var candidate = Path.Combine(dir.Trim('"'), exe);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    internal static bool IsPortAvailable(string hostname, int port)
    {
        try
        {
            using var listener = new TcpListener(ResolveHost(hostname), port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static int FindAvailablePort(string hostname, int start)
    {
        for (var port = start; port <= 65535; port++)
        {
            if (IsPortAvailable(hostname, port))
                return port;
        }
        return 0;
    }

    internal static System.Net.IPAddress ResolveHost(string hostname)
    {
        return System.Net.IPAddress.TryParse(hostname, out var ip) ? ip : System.Net.IPAddress.Any;
    }

    /// <summary>
    /// Returns the process ID of the process currently listening on <paramref name="port"/>,
    /// or 0 when no process is listening there.
    /// </summary>
    internal static int FindListeningPid(int port)
    {
        return FindListeningProcesses(port).FirstOrDefault()?.ProcessId ?? 0;
    }

    internal static IReadOnlyList<ListeningProcess> FindListeningProcesses(int port)
    {
        var processes = new Dictionary<int, ListeningProcess>();
        try
        {
            var psi = new ProcessStartInfo("netstat", "-ano -p tcp")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null)
                return Array.Empty<ListeningProcess>();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            foreach (var line in output.Split('\n'))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    continue;
                if (!parts[0].Equals("TCP", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;
                var local = parts[1];
                var colon = local.LastIndexOf(':');
                if (colon < 0)
                    continue;
                if (int.TryParse(local[(colon + 1)..], out var linePort) && linePort == port
                    && int.TryParse(parts[4], out var pid) && !processes.ContainsKey(pid))
                {
                    var name = "Unknown process";
                    try
                    {
                        using var process = Process.GetProcessById(pid);
                        name = process.ProcessName;
                    }
                    catch
                    {
                        // The process may exit between netstat and process lookup.
                    }
                    processes[pid] = new ListeningProcess(pid, name, local);
                }
            }
        }
        catch
        {
            // ignore
        }
        return processes.Values.ToArray();
    }

    /// <summary>
    /// Takes over an already-running process identified by <paramref name="pid"/> so the
    /// tray can manage (stop/restart) it. Returns false when the process no longer exists.
    /// </summary>
    public bool TryAttach(int pid)
    {
        try
        {
            var proc = Process.GetProcessById(pid);
            if (proc.HasExited)
                return false;

            lock (_lock)
            {
                _process = proc;
            }
            proc.Exited += (_, _) =>
            {
                Log($"Attached process exited with code {proc.ExitCode}.");
                SetStatus(proc.ExitCode == 0 ? ServiceStatus.Stopped : ServiceStatus.Error);
                CleanupProcess(proc);
            };
            proc.EnableRaisingEvents = true;
            SetStatus(ServiceStatus.Running);
            Log($"Attached to existing process {pid}.");
            return true;
        }
        catch (Exception ex)
        {
            Log($"Failed to attach to process {pid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Force-kills the process <paramref name="pid"/> and its entire process tree.</summary>
    internal static void KillProcessTree(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
    }

    public async Task StartAsync()
    {
        Process? existing;
        lock (_lock)
        {
            existing = _process;
            if (_process is { HasExited: false })
                return;
        }
        existing?.Dispose();

        SetStatus(ServiceStatus.Starting);

        var (exe, prefix, useCmdWrapper) = ResolveCommand();
        var workDir = !string.IsNullOrWhiteSpace(_config.WorkingDirectory)
            ? _config.WorkingDirectory
            : AppContext.BaseDirectory;

        var port = _config.Port;
        if (_config.AutoChangePort && !IsPortAvailable(_config.Hostname, port))
        {
            var next = FindAvailablePort(_config.Hostname, port + 1);
            if (next > 0)
            {
                Log($"Port {_config.Port} is occupied, switching to {next}.");
                _config.Port = next;
                port = next;
                PortChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        var args = _profile.ArgsTemplate
            .Replace("{host}", _config.Hostname)
            .Replace("{port}", port.ToString());
        var command = prefix is null ? $"\"{exe}\"" : $"\"{exe}\" {prefix}";

        ProcessStartInfo psi;
        if (useCmdWrapper)
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/s /c \"{command} {args}\"",
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = prefix is null ? args : $"{prefix} {args}",
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }

        Log($"Starting: exe={exe} workDir={workDir} args={args}");

        _cts = new CancellationTokenSource();

        try
        {
            var proc = Process.Start(psi);
            if (proc is null)
                throw new InvalidOperationException("Failed to start process.");

            lock (_lock)
            {
                _process = proc;
            }
            proc.Exited += (_, _) =>
            {
                Log($"Process exited with code {proc.ExitCode}.");
                SetStatus(proc.ExitCode == 0 ? ServiceStatus.Stopped : ServiceStatus.Error);
                CleanupProcess(proc);
            };
            proc.EnableRaisingEvents = true;

            var healthy = await WaitForHealthyAsync(TimeSpan.FromSeconds(30), _cts.Token);
            Log(healthy
                ? $"Service is healthy on {_config.Hostname}:{_config.Port}."
                : "Service did not become healthy within 30s.");
            SetStatus(healthy ? ServiceStatus.Running : ServiceStatus.Error);
        }
        catch (Exception ex)
        {
            Log($"Failed to start: {ex}");
            SetStatus(ServiceStatus.Error);
            throw new InvalidOperationException($"Failed to start {_profile.DisplayName}: {ex.Message}", ex);
        }
    }

    public async Task StopAsync()
    {
        Process? proc;
        lock (_lock)
        {
            proc = _process;
            _process = null;
        }

        if (proc is { HasExited: false })
        {
            Log("Stopping service...");
            SetStatus(ServiceStatus.Stopping);
            _cts?.Cancel();

            try
            {
                proc.Kill(entireProcessTree: true);
                await proc.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Log($"Stop failed: {ex}");
                Debug.WriteLine($"Stop failed: {ex.Message}");
            }

            proc.Dispose();
            SetStatus(ServiceStatus.Stopped);
            Log("Service stopped.");
        }
        else
        {
            proc?.Dispose();
            SetStatus(ServiceStatus.Stopped);
        }
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var tcp = new TcpClient();
            var connect = tcp.ConnectAsync(_config.Hostname, _config.Port);
            if (await Task.WhenAny(connect, Task.Delay(1000)) != connect)
                return false;
            await connect;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitForHealthyAsync(TimeSpan timeout, CancellationToken token)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            token.ThrowIfCancellationRequested();
            if (await IsHealthyAsync())
                return true;
            await Task.Delay(300, token);
        }
        return false;
    }

    private void CleanupProcess(Process proc)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_process, proc))
                _process = null;
        }
        proc.Dispose();
    }

    private void SetStatus(ServiceStatus status)
    {
        lock (_lock)
        {
            if (_status == status)
                return;
            _status = status;
        }
        StatusChanged?.Invoke(this, status);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        lock (_lock)
        {
            var proc = _process;
            _process = null;
            if (proc is { HasExited: false })
            {
                try
                {
                    proc.Kill(entireProcessTree: true);
                    if (!proc.WaitForExit(TimeSpan.FromSeconds(5)) && !proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(TimeSpan.FromSeconds(2));
                    }
                }
                catch
                {
                    // ignore
                }
            }
            proc?.Dispose();
        }
    }
}
