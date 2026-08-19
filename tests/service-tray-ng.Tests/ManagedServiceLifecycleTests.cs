using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace service_tray_ng.Tests;

public class ManagedServiceLifecycleTests
{
    private static string? FindNode()
    {
        var fromPath = ManagedService.FindOnPath("node.exe");
        if (fromPath is not null)
            return fromPath;
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidate = Path.Combine(programFiles, "nodejs", "node.exe");
        return File.Exists(candidate) ? candidate : null;
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task StartStop_BecomesHealthyAndStops()
    {
        var node = FindNode();
        if (node is null)
        {
            Assert.True(true, "node.exe not found; skipping integration test");
            return;
        }

        var port = GetFreePort();
        var profile = new ServiceProfile
        {
            Key = "test-node",
            DisplayName = "Test Node",
            CommandNames = ["node.exe"],
            ArgsTemplate = "-e \"require('net').createServer(s => s.end()).listen({port}, '{host}')\"",
            DefaultPort = port,
            LogoLightResource = "x",
            LogoDarkResource = "x",
        };
        var config = new ServiceConfig
        {
            Hostname = "127.0.0.1",
            Port = port,
            AutoChangePort = false,
            ExecutablePath = node,
        };

        using var service = new ManagedService(profile, config);
        var statuses = new List<ServiceStatus>();
        service.StatusChanged += (_, status) => statuses.Add(status);

        await service.StopAsync();
        Assert.Empty(statuses);

        await service.StartAsync();

        Assert.Equal(ServiceStatus.Running, service.Status);
        Assert.True(await service.IsHealthyAsync());

        var pid = ManagedService.FindListeningPid(port);
        Assert.True(pid > 0);
        Assert.True(ManagedService.KillProcessTree(pid));
        await Task.Delay(100);
        Assert.False(await service.IsHealthyAsync());

        await service.StopAsync();

        Assert.Equal(ServiceStatus.Stopped, service.Status);
        Assert.False(await service.IsHealthyAsync());
    }

    [Fact]
    public async Task Restart_StopsThenStarts()
    {
        var node = FindNode();
        if (node is null)
        {
            Assert.True(true, "node.exe not found; skipping integration test");
            return;
        }

        var port = GetFreePort();
        var profile = new ServiceProfile
        {
            Key = "test-node",
            DisplayName = "Test Node",
            CommandNames = ["node.exe"],
            ArgsTemplate = "-e \"require('net').createServer(s => s.end()).listen({port}, '{host}')\"",
            DefaultPort = port,
            LogoLightResource = "x",
            LogoDarkResource = "x",
        };
        var config = new ServiceConfig
        {
            Hostname = "127.0.0.1",
            Port = port,
            AutoChangePort = false,
            ExecutablePath = node,
        };

        using var service = new ManagedService(profile, config);

        await service.StartAsync();
        Assert.Equal(ServiceStatus.Running, service.Status);

        await service.RestartAsync();
        Assert.Equal(ServiceStatus.Running, service.Status);
        Assert.True(await service.IsHealthyAsync());

        await service.StopAsync();
        Assert.Equal(ServiceStatus.Stopped, service.Status);
    }

    [Fact]
    public async Task FindExternalProcesses_ScansPortsByProcessNameAndHttpResponse()
    {
        var node = FindNode();
        if (node is null)
        {
            Assert.True(true, "node.exe not found; skipping integration test");
            return;
        }

        var port = GetFreePort();
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = node,
            Arguments = $"-e \"require('http').createServer((req,res)=>res.end('ok')).listen({port}, '127.0.0.1')\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        Assert.NotNull(process);

        var profile = new ServiceProfile
        {
            Key = "test-dsh",
            DisplayName = "Test Dsh",
            CommandNames = ["dsh.cmd"],
            NpxPackage = "test-package",
            ArgsTemplate = "web --host {host} --port {port}",
            DefaultPort = 3080,
            LogoLightResource = "x",
            LogoDarkResource = "x",
        };
        using var service = new ManagedService(profile, new ServiceConfig { Port = GetFreePort() });

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            IReadOnlyList<ListeningProcess> found = [];
            while (DateTime.UtcNow < deadline && found.All(item => item.ProcessId != process.Id))
            {
                found = await service.FindExternalProcessesAsync();
                if (found.All(item => item.ProcessId != process.Id))
                    await Task.Delay(100);
            }

            Assert.Contains(found, item => item.ProcessId == process.Id && item.Port == port);
        }
        finally
        {
            ManagedService.KillProcessTree(process.Id);
        }
    }
}
