namespace service_tray_ng;

/// <summary>
/// Build-time edition of this tray app. Selected via the MSBuild <c>Edition</c>
/// property at publish time (<c>dsh</c>, <c>opencode</c> or <c>all</c>).
/// Used to keep single-instance locks, registry Run entries and config paths
/// isolated per edition so the three variants can run side by side.
/// </summary>
public static class Edition
{
    /// <summary>Short identifier of the built edition: "dsh", "opencode" or "all".</summary>
    public const string Key =
#if EDITION_DSH
        "dsh";
#elif EDITION_OPENCODE
        "opencode";
#else
        "all";
#endif

    /// <summary>Unique single-instance mutex name for this edition.</summary>
    public static string MutexName => $"service-tray-ng-{Key}-single-instance";

    /// <summary>Value name used under the registry Run key for this edition.</summary>
    public static string RunValueName => $"ServiceTrayNg-{Key}";

    /// <summary>Config sub-directory name (under %LOCALAPPDATA%) for this edition.</summary>
    public static string ConfigDirName => $"service-tray-ng-{Key}";
}
