using Xunit;

namespace service_tray_ng.Tests;

public class ServiceProfileTests
{
    [Fact]
    public void All_ContainsOpenCodeAndDsh()
    {
        var keys = ServiceProfiles.All.Select(p => p.Key).ToArray();
        Assert.Contains("opencode", keys);
        Assert.Contains("dsh", keys);
    }

    [Fact]
    public void OpenCodeProfile_HasExpectedDefaults()
    {
        var p = ServiceProfiles.OpenCode;
        Assert.Equal("OpenCode Service", p.DisplayName);
        Assert.Equal(4096, p.DefaultPort);
        Assert.Null(p.NpxPackage);
        Assert.Contains("{host}", p.ArgsTemplate);
        Assert.Contains("{port}", p.ArgsTemplate);
        Assert.Equal("serve --hostname {host} --port {port}", p.ArgsTemplate);
    }

    [Fact]
    public void DshProfile_HasExpectedDefaults()
    {
        var p = ServiceProfiles.Dsh;
        Assert.Equal("Dsh Service", p.DisplayName);
        Assert.Equal(3080, p.DefaultPort);
        Assert.Equal("@deepseek-ai/dsh", p.NpxPackage);
        Assert.Equal("web --host {host} --port {port}", p.ArgsTemplate);
    }

    [Fact]
    public void ArgsTemplate_ReplacesPlaceholders()
    {
        var args = ServiceProfiles.Dsh.ArgsTemplate
            .Replace("{host}", "127.0.0.1")
            .Replace("{port}", "3080");
        Assert.Equal("web --host 127.0.0.1 --port 3080", args);
    }
}
