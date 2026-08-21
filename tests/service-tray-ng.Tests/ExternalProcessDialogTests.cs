using System.Globalization;
using System.Windows.Forms;
using Xunit;

namespace service_tray_ng.Tests;

/// <summary>
/// Integration tests for the real <see cref="ExternalProcessDialog"/> control behavior
/// (radio grouping, default selection, close-others checkbox wiring). WinForms controls
/// must live on an STA thread, so every test body runs through <see cref="RunOnSta"/>.
/// Note: dialogs are briefly shown to create a window handle, so running the suite may
/// flash small windows on screen.
/// </summary>
public class ExternalProcessDialogTests
{
    private static T RunOnSta<T>(Func<T> action)
    {
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null)
            throw new InvalidOperationException("STA UI thread failed.", error);
        return result;
    }

    private static void RunOnSta(Action action) => RunOnSta(() => { action(); return true; });

    private static List<ListeningProcess> TwoInstances() =>
    [
        new(111, "dsh", "127.0.0.1:4096", 4096),
        new(222, "dsh", "127.0.0.1:3080", 3080),
    ];

    private static Button OkButton(Form dialog) =>
        dialog.Controls.OfType<Button>().Single(button => button.DialogResult == DialogResult.OK);

    [Fact]
    public void MultiInstance_DefaultSelectsConfiguredPortInstance_AndOffersCloseOthers()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, TwoInstances(), 3081);
            dialog.Show();

            var closeOthers = Assert.Single(dialog.Controls.OfType<CheckBox>());
            Assert.False(closeOthers.Checked, "close-others should be unchecked by default");
            Assert.True(closeOthers.Enabled, "close-others should be enabled by default");

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Attach, dialog.Action);
            Assert.Equal(222, dialog.SelectedProcess?.ProcessId);
            Assert.False(dialog.CloseOthers);

            dialog.Close();
        });
    }

    [Fact]
    public void MultiInstance_SelectingOtherInstance_TakesOverThatOne()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, TwoInstances(), 3081);
            dialog.Show();

            var attachOptions = dialog.Controls.OfType<RadioButton>()
                .Where(radio => radio.Text.StartsWith("Take over", StringComparison.Ordinal))
                .ToArray();
            Assert.Equal(2, attachOptions.Length);
            attachOptions.Single(radio => radio.Text.Contains("4096")).Checked = true;

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Attach, dialog.Action);
            Assert.Equal(111, dialog.SelectedProcess?.ProcessId);

            dialog.Close();
        });
    }

    [Fact]
    public void MultiInstance_AttachWithCloseOthers_SetsFlagAndExposesOthers()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, TwoInstances(), 3081);
            dialog.Show();

            var closeOthers = Assert.Single(dialog.Controls.OfType<CheckBox>());
            closeOthers.Checked = true;

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Attach, dialog.Action);
            Assert.True(dialog.CloseOthers);
            var toClose = Assert.Single(dialog.ProcessesToClose);
            Assert.Equal(111, toClose.ProcessId); // the other instance, not the selected one

            dialog.Close();
        });
    }

    [Fact]
    public void SelectingKill_ResetsAndDisablesCloseOthers()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, TwoInstances(), 3081);
            dialog.Show();

            var closeOthers = Assert.Single(dialog.Controls.OfType<CheckBox>());
            closeOthers.Checked = true;
            var kill = dialog.Controls.OfType<RadioButton>()
                .Single(radio => radio.Text.StartsWith("Close all", StringComparison.Ordinal));
            kill.Checked = true;

            Assert.False(closeOthers.Checked, "close-others should be reset when kill is selected");
            Assert.False(closeOthers.Enabled, "close-others should be disabled when kill is selected");

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Kill, dialog.Action);
            Assert.False(dialog.CloseOthers);
            Assert.Null(dialog.SelectedProcess);
            Assert.Empty(dialog.ProcessesToClose);

            dialog.Close();
        });
    }

    [Fact]
    public void SelectingStartNew_SetsActionAndSuggestedPort()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, TwoInstances(), 3081);
            dialog.Show();

            var closeOthers = Assert.Single(dialog.Controls.OfType<CheckBox>());
            closeOthers.Checked = true;
            var startNew = dialog.Controls.OfType<RadioButton>()
                .Single(radio => radio.Text.Contains("another port", StringComparison.Ordinal));
            startNew.Checked = true;

            Assert.False(closeOthers.Enabled, "close-others should be disabled when start-new is selected");

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.StartNew, dialog.Action);
            Assert.Equal(3081, dialog.NewPort);
            Assert.False(dialog.CloseOthers);

            dialog.Close();
        });
    }

    [Fact]
    public void SingleInstance_NoCloseOthersCheckbox_AndDefaultsToThatInstance()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog(
                "Dsh Service", "127.0.0.1", 3080,
                [new ListeningProcess(999, "dsh", "127.0.0.1:3080", 3080)], 3081);
            dialog.Show();

            Assert.Empty(dialog.Controls.OfType<CheckBox>());

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Attach, dialog.Action);
            Assert.Equal(999, dialog.SelectedProcess?.ProcessId);
            Assert.False(dialog.CloseOthers);

            dialog.Close();
        });
    }

    [Fact]
    public void NoInstances_DefaultsToKill()
    {
        RunOnSta(() =>
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            using var dialog = new ExternalProcessDialog("Dsh Service", "127.0.0.1", 3080, [], 3081);
            dialog.Show();

            OkButton(dialog).PerformClick();

            Assert.Equal(ExternalServiceAction.Kill, dialog.Action);
            Assert.Null(dialog.SelectedProcess);
            Assert.False(dialog.CloseOthers);

            dialog.Close();
        });
    }
}
