using System.Globalization;
using Xunit;

namespace service_tray_ng.Tests;

public class StringsTests : IDisposable
{
    private readonly CultureInfo _original;

    public StringsTests()
    {
        _original = CultureInfo.CurrentUICulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _original;
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("zh-CN")]
    [InlineData("zh-TW")]
    [InlineData("ja-JP")]
    [InlineData("ko-KR")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("it-IT")]
    [InlineData("pt-BR")]
    [InlineData("ru-RU")]
    public void Get_ReturnsNonEmptyTranslationForKnownKeys(string cultureName)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

        foreach (var key in KnownKeys)
        {
            var value = Strings.Get(key);
            Assert.False(string.IsNullOrWhiteSpace(value), $"{cultureName}: '{key}' empty");
            Assert.NotEqual(key, value);
        }
    }

    [Fact]
    public void Get_UnknownCulture_FallsBackToEnglish()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("xx-XX");
        Assert.Equal("Start", Strings.Get("Menu.Start"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKeyItself()
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Assert.Equal("no.such.key", Strings.Get("no.such.key"));
    }

    [Fact]
    public void GetLanguageCode_MapsChineseVariants()
    {
        Assert.Equal("zh", Strings.GetLanguageCode(new CultureInfo("zh-CN")));
        Assert.Equal("zh", Strings.GetLanguageCode(new CultureInfo("zh-TW")));
    }

    [Fact]
    public void GetLanguageCode_UnknownLanguage_FallsBackToEnglish()
    {
        Assert.Equal("en", Strings.GetLanguageCode(new CultureInfo("xx-XX")));
    }

    [Fact]
    public void FormatStrings_ReplacePlaceholders()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        Assert.Equal("Running on 127.0.0.1:4096", string.Format(Strings.Get("Menu.Status.Running"), "127.0.0.1", 4096));
        Assert.Equal("Port: 3080", string.Format(Strings.Get("Menu.Port"), 3080));
    }

    private static readonly string[] KnownKeys =
    [
        "App.Title", "App.AlreadyRunning.Message",
        "Menu.Status.Stopped", "Menu.Status.Error", "Menu.Status.Starting", "Menu.Status.Stopping", "Menu.Status.Running",
        "Menu.Start", "Menu.Stop", "Menu.Restart", "Menu.Exit", "Menu.StartOnLogin", "Menu.StartServiceOnLaunch",
        "Menu.Port", "Menu.AutoSwitchPort", "Menu.OpenLogFolder", "Menu.OpenConfig",
        "Balloon.Running", "Balloon.Stopped", "Balloon.Error", "Balloon.StateChanged", "Balloon.NotRunning",
        "Balloon.Attached", "Balloon.AttachFailed", "Balloon.KillFailed",
        "Dialog.ChangePort", "Dialog.ServerPort", "Dialog.OK", "Dialog.Cancel",
        "Dialog.ExternalProcess.Title", "Dialog.ExternalProcess.Message",
        "Dialog.ExternalProcess.Attach", "Dialog.ExternalProcess.Kill", "Dialog.ExternalProcess.StartNew",
        "Dialog.ExternalProcess.AttachOption", "Dialog.ExternalProcess.KillOption",
        "Dialog.ExternalProcess.StartNewOption", "Dialog.ExternalProcess.ProcessDetail",
        "Dialog.ExternalProcess.NoProcessDetails", "Dialog.ExternalProcess.NewPort",
        "ServiceName.OpenCode", "ServiceName.Dsh",
    ];
}
