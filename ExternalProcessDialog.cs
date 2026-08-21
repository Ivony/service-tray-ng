using System.Windows.Forms;

namespace service_tray_ng;

public sealed class ExternalProcessDialog : Form
{
    private readonly ExternalChoiceModel _model;

    public ExternalServiceAction Action => _model.Action;
    public int NewPort { get; private set; }

    /// <summary>The specific instance to take over when <see cref="Action"/> is <see cref="ExternalServiceAction.Attach"/>, or null.</summary>
    public ListeningProcess? SelectedProcess => _model.SelectedProcess;

    /// <summary>When attaching, also close every other detected instance. Only offered when several instances are running.</summary>
    public bool CloseOthers => _model.CloseOthers;

    /// <summary>The instances to close for the current selection (non-empty only when attaching with <see cref="CloseOthers"/> set).</summary>
    public IReadOnlyList<ListeningProcess> ProcessesToClose => _model.ProcessesToClose;

    public ExternalProcessDialog(
        string serviceName,
        string hostname,
        int port,
        IReadOnlyList<ListeningProcess> processes,
        int defaultNewPort)
    {
        _model = new ExternalChoiceModel(port, processes);
        var sorted = _model.SortedProcesses;

        Text = Strings.Get("Dialog.ExternalProcess.Title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 620;

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
        var attachOptions = new List<RadioButton>(sorted.Count);
        for (var i = 0; i < sorted.Count; i++)
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

        if (sorted.Count > 0)
            _model.SelectAttach((ListeningProcess)attachOptions[0].Tag!);
        else
            _model.SelectKill();

        // While taking over one instance, offer closing the remaining ones so the
        // configured port is freed up. Only meaningful (and offered) with multiple instances.
        CheckBox? closeOthers = null;
        if (_model.CanCloseOthers)
        {
            closeOthers = new CheckBox
            {
                Text = string.Format(Strings.Get("Dialog.ExternalProcess.CloseOthers"), sorted.Count - 1),
                Location = new Point(44, y + 2),
                Size = new Size(530, rowHeight - 4),
                Enabled = true,
            };
            y += rowHeight + 8;
        }

        var kill = new RadioButton
        {
            Text = string.Format(Strings.Get("Dialog.ExternalProcess.KillAllInstances"), sorted.Count),
            Location = new Point(24, y),
            Size = new Size(550, rowHeight),
            Checked = sorted.Count == 0, // fallback when nothing is listed
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
                if (option.Checked)
                {
                    _model.SelectAttach((ListeningProcess)option.Tag!);
                    if (closeOthers is not null)
                        closeOthers.Enabled = true;
                }
            };
        }
        if (closeOthers is not null)
        {
            closeOthers.CheckedChanged += (_, _) => _model.SetCloseOthers(closeOthers.Checked);

            kill.CheckedChanged += (_, _) =>
            {
                if (kill.Checked)
                {
                    _model.SelectKill();
                    closeOthers.Checked = false;
                    closeOthers.Enabled = false;
                }
            };
            startNew.CheckedChanged += (_, _) =>
            {
                if (startNew.Checked)
                {
                    _model.SelectStartNew();
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
                if (startNew.Checked)
                    _model.SelectStartNew();
                newPort.Enabled = startNew.Checked;
                newPortLabel.Enabled = startNew.Checked;
            };
        }

        ok.Click += (_, _) =>
        {
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
