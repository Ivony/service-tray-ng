namespace service_tray_ng;

/// <summary>Describes one managed CLI service that the tray knows how to run.</summary>
public sealed class ServiceProfile
{
    /// <summary>Config dictionary key and log-file prefix, e.g. "opencode".</summary>
    public required string Key { get; init; }

    /// <summary>Display name shown in tray tooltips and balloon messages.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Executable names to probe on PATH, in order.</summary>
    public required string[] CommandNames { get; init; }

    /// <summary>npm package used through `npx`, or null when the command is a plain executable.</summary>
    public string? NpxPackage { get; init; }

    /// <summary>Command-line template. Placeholders: {host}, {port}.</summary>
    public required string ArgsTemplate { get; init; }

    public int DefaultPort { get; init; }

    /// <summary>Embedded resource names for the logo, light and dark variants.</summary>
    public required string LogoLightResource { get; init; }

    public required string LogoDarkResource { get; init; }
}

public static class ServiceProfiles
{
    public static readonly ServiceProfile OpenCode = new()
    {
        Key = "opencode",
        DisplayName = "OpenCode Service",
        CommandNames = ["opencode.exe", "opencode"],
        NpxPackage = null,
        ArgsTemplate = "serve --hostname {host} --port {port}",
        DefaultPort = 4096,
        LogoLightResource = "service_tray_ng.Assets.opencode-logo-light.png",
        LogoDarkResource = "service_tray_ng.Assets.opencode-logo-dark.png",
    };

    public static readonly ServiceProfile Dsh = new()
    {
        Key = "dsh",
        DisplayName = "Dsh Service",
        CommandNames = ["dsh.cmd", "dsh"],
        NpxPackage = "@deepseek-ai/dsh",
        ArgsTemplate = "web --host {host} --port {port}",
        DefaultPort = 3080,
        LogoLightResource = "service_tray_ng.Assets.dsh-logo-light.png",
        LogoDarkResource = "service_tray_ng.Assets.dsh-logo-dark.png",
    };

    public static readonly IReadOnlyList<ServiceProfile> All =
#if EDITION_DSH
        [Dsh];
#elif EDITION_OPENCODE
        [OpenCode];
#else
        [OpenCode, Dsh];
#endif
}
