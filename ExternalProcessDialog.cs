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

    /// <summary>The specific instance to take over when <see cref="Action"/> is <see cref="ExternalServiceAction.Attach"/>, or null.</summary>
    public ListeningProcess? SelectedProcess { get; private set; }

    /// <summary>When attaching, also close every other detected instance. Only offered when several instances are running.</summary>
    public bool CloseOthers { get; private set; }

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

        // Instances matching the configured port first, then by port number, so the
        // default selection is the least surprising one.
        var sorted = processes
            .OrderByDescending(process => process.Port == port)
            .ThenBy(process => process.Port)
            .ToArray();

        var message = new Label
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.Message"), serviceName, hostname, port),
            Location = new Point(20, 18),
            AutoSize = true,
            MaximumSize = new Size(560, 0),
        };

        const int rowHeight = 36;
        var y = 70;

        // One "take over" option per detected instance, so the user picks exactly
        // which instance to manage when several are running.
        var attachOptions = new List<RadioButton>(sorted.Length);
        for (var i = 0; i < sorted.Length; i++)
        {
            var process = sorted[i];
            var option = new RadioButton
            {
                Text = string.Format(Strings.Get("Dialog.ExternalProcess.AttachInstance"),
                    process.ProcessName, process.ProcessId, process.Endpoint),
                Location = new Point(24, y),
                Size = new Size(550, rowHeight),
                AutoEllipsis = true,
                Checked = i == 0,
                Tag = process,
            };
            attachOptions.Add(option);
            y += rowHeight;
        }

        // While taking over one instance, offer closing the remaining ones so the
        // configured port is freed up. Only meaningful (and offered) with multiple instances.
        CheckBox? closeOthers = null;
        if (sorted.Length > 1)
        {
            closeOthers = new CheckBox
            {
                Text = string.Format(Strings.Get("Dialog.ExternalProcess.CloseOthers"), sorted.Length - 1),
                Location = new Point(44, y + 2),
                Size = new Size(530, rowHeight - 4),
                Enabled = true,
            };
            y += rowHeight + 8;
        }

        var kill = new RadioButton
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.KillAllInstances"), sorted.Length),
            Location = new Point(24, y),
            Size = new Size(550, rowHeight),
            Checked = sorted.Length == 0, // fallback when nothing is listed
        };
        y += rowHeight + 8;

        var startNew = new RadioButton
        {
            Text = Strings.Get("Dialog.ExternalProcess.StartNewOption"),
            Location = new Point(24, y),
            Size = new Size(550, rowHeight),
        };
        y += rowHeight + 6;

        var newPortLabel = new Label
        {
            Text = Strings.Get("Dialog.ExternalProcess.NewPort"),
            Location = new Point(52, y),
            AutoSize = true,
        };
        var newPort = new NumericUpDown
        {
            Location = new Point(155, y - 3),
            Width = 100,
            Minimum = 1,
            Maximum = 65535,
            Value = Math.Clamp(defaultNewPort > 0 ? defaultNewPort : port, 1, 65535),
            Enabled = false,
        };
        y += 34;

        var ok = new Button
        {
            Text = Strings.Get("Dialog.OK"),
            DialogResult = DialogResult.OK,
            Location = new Point(404, y),
            Width = 90,
        };
        var cancel = new Button
        {
            Text = Strings.Get("Dialog.Cancel"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(500, y),
            Width = 90,
        };

        // "Close others" only makes sense while attaching to a specific instance.
        foreach (var option in attachOptions)
        {
            option.CheckedChanged += (_, _) =>
            {
                if (option.Checked && closeOthers is not null)
                    closeOthers.Enabled = true;
            };
        }
        if (closeOthers is not null)
        {
            kill.CheckedChanged += (_, _) =>
            {
                if (kill.Checked)
                {
                    closeOthers.Checked = false;
                    closeOthers.Enabled = false;
                }
            };
            startNew.CheckedChanged += (_, _) =>
            {
                if (startNew.Checked)
                {
                    closeOthers.Checked = false;
                    closeOthers.Enabled = false;
                }
                newPort.Enabled = startNew.Checked;
                newPortLabel.Enabled = startNew.Checked;
            };
        }
        else
        {
            startNew.CheckedChanged += (_, _) =>
            {
                newPort.Enabled = startNew.Checked;
                newPortLabel.Enabled = startNew.Checked;
            };
        }

        ok.Click += (_, _) =>
        {
            var checkedAttach = attachOptions.FirstOrDefault(option => option.Checked);
            if (checkedAttach is not null)
            {
                Action = ExternalServiceAction.Attach;
                SelectedProcess = (ListeningProcess)checkedAttach.Tag!;
                CloseOthers = closeOthers?.Checked ?? false;
            }
            else if (kill.Checked)
            {
                Action = ExternalServiceAction.Kill;
                SelectedProcess = null;
                CloseOthers = false;
            }
            else
            {
                Action = ExternalServiceAction.StartNew;
                SelectedProcess = null;
                CloseOthers = false;
            }
            NewPort = (int)newPort.Value;
        };

        var controls = new List<Control> { message };
        controls.AddRange(attachOptions);
        if (closeOthers is not null)
            controls.Add(closeOthers);
        controls.AddRange([kill, startNew, newPortLabel, newPort, ok, cancel]);
        Controls.AddRange(controls.ToArray());
        AcceptButton = ok;
        CancelButton = cancel;

        Height = y + 44;
    }
}
