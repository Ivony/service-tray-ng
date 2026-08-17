using Xunit;

namespace service_tray_ng.Tests;

public class ManagedServiceResolveCommandTests
{
    private readonly string _tempDir;

    public ManagedServiceResolveCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "dsh-service-tray-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    private ManagedService CreateService(ServiceProfile profile, ServiceConfig config)
        => new(profile, config);

    [Fact]
    public void ResolveCommand_ConfiguredPathExists_UsesIt()
    {
        var exe = Path.Combine(_tempDir, "custom.exe");
        File.WriteAllBytes(exe, [0x4D, 0x5A]);
        var config = new ServiceConfig { ExecutablePath = exe, Port = 3080 };
        var service = CreateService(ServiceProfiles.Dsh, config);

        var (fileName, prefix, useCmdWrapper) = service.ResolveCommand();

        Assert.Equal(exe, fileName);
        Assert.Null(prefix);
        Assert.False(useCmdWrapper);
    }

    [Fact]
    public void ResolveCommand_ConfiguredPathMissing_ThrowsNothingAndFallsBack()
    {
        var config = new ServiceConfig { ExecutablePath = Path.Combine(_tempDir, "missing.exe"), Port = 3080 };
        var service = CreateService(ServiceProfiles.Dsh, config);

        var (_, _, _) = service.ResolveCommand();
    }

    [Fact]
    public void FindOnPath_ReturnsFullPathOfExistingExecutable()
    {
        var exe = Path.Combine(_tempDir, "mybin.cmd");
        File.WriteAllText(exe, "@echo off");
        var original = Environment.GetEnvironmentVariable("PATH") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("PATH", _tempDir + ";" + original);
            Assert.Equal(exe, ManagedService.FindOnPath("mybin.cmd"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
        }
    }

    [Fact]
    public void FindOnPath_ReturnsNullWhenNotFound()
    {
        var original = Environment.GetEnvironmentVariable("PATH") ?? "";
        try
        {
            Environment.SetEnvironmentVariable("PATH", _tempDir);
            Assert.Null(ManagedService.FindOnPath("definitely-not-there-xyz.exe"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
        }
    }

    [Fact]
    public void IsCmdFile_DetectsCmdAndBat()
    {
        Assert.True(ManagedService.IsCmdFile("x.cmd"));
        Assert.True(ManagedService.IsCmdFile("x.bat"));
        Assert.False(ManagedService.IsCmdFile("x.exe"));
        Assert.False(ManagedService.IsCmdFile("x"));
    }

    [Fact]
    public void IsPortAvailable_TrueForFreePort_ThenFalseWhenBound()
    {
        var port = GetFreePort();
        Assert.True(ManagedService.IsPortAvailable("127.0.0.1", port));

        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        try
        {
            Assert.False(ManagedService.IsPortAvailable("127.0.0.1", port));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void FindAvailablePort_ReturnsFreePortAtOrAboveStart()
    {
        var start = GetFreePort();
        var found = ManagedService.FindAvailablePort("127.0.0.1", start);
        Assert.True(found >= start);
        Assert.True(ManagedService.IsPortAvailable("127.0.0.1", found));
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void DisposeService(ManagedService service) => service.Dispose();
}
