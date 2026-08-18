using System.Windows.Forms;

namespace service_tray_ng;

public enum ExternalServiceAction
{
    Attach,
    Kill,
    StartNew,
}

public sealed class ExternalProcessDialog : Form
{
    public ExternalServiceAction Action { get; private set; }
    public int NewPort { get; private set; }

    public ExternalProcessDialog(
        string serviceName,
        string hostname,
        int port,
        IReadOnlyList<ListeningProcess> processes,
        int defaultNewPort)
    {
        Text = Strings.Get("Dialog.ExternalProcess.Title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 620;
        Height = 390;

        var message = new Label
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.Message"), serviceName, hostname, port),
            Location = new Point(20, 18),
            AutoSize = true,
            MaximumSize = new Size(560, 0),
        };

        var processText = processes.Count == 0
            ? Strings.Get("Dialog.ExternalProcess.NoProcessDetails")
            : string.Join(Environment.NewLine, processes.Select(process =>
                string.Format(Strings.Get("Dialog.ExternalProcess.ProcessDetail"),
                    process.ProcessName, process.ProcessId, process.Endpoint)));

        var attach = new RadioButton
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.AttachOption"), processText),
            Location = new Point(24, 75),
            Size = new Size(550, 62),
            Checked = true,
            AutoSize = false,
        };
        var kill = new RadioButton
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.KillOption"), processText),
            Location = new Point(24, 143),
            Size = new Size(550, 62),
            AutoSize = false,
        };
        var startNew = new RadioButton
        {
            Text = Strings.Get("Dialog.ExternalProcess.StartNewOption"),
            Location = new Point(24, 211),
            Size = new Size(550, 32),
            AutoSize = false,
        };

        var newPortLabel = new Label
        {
            Text = Strings.Get("Dialog.ExternalProcess.NewPort"),
            Location = new Point(52, 250),
            AutoSize = true,
        };
        var newPort = new NumericUpDown
        {
            Location = new Point(155, 247),
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = Math.Clamp(defaultNewPort > 0 ? defaultNewPort : port, 1, 65535),
            Enabled = false,
        };

        var ok = new Button
        {
            Text = Strings.Get("Dialog.OK"),
            DialogResult = DialogResult.OK,
            Location = new Point(404, 302),
            Width = 90,
        };
        var cancel = new Button
        {
            Text = Strings.Get("Dialog.Cancel"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(500, 302),
            Width = 90,
        };

        startNew.CheckedChanged += (_, _) =>
        {
            newPort.Enabled = startNew.Checked;
            newPortLabel.Enabled = startNew.Checked;
        };
        ok.Click += (_, _) =>
        {
            Action = attach.Checked ? ExternalServiceAction.Attach
                : kill.Checked ? ExternalServiceAction.Kill
                : ExternalServiceAction.StartNew;
            NewPort = (int)newPort.Value;
        };

        Controls.AddRange([message, attach, kill, startNew, newPortLabel, newPort, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
