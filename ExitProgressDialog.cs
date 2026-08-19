namespace service_tray_ng;

internal sealed class ExitProgressDialog : Form
{
    private readonly IReadOnlyList<(string Name, Func<Task> Action)> _tasks;
    private readonly TableLayoutPanel _taskTable;
    private readonly ProgressBar _progress;
    private readonly Label _summary;
    private int _failedTasks;

    public ExitProgressDialog(IReadOnlyList<(string Name, Func<Task> Action)> tasks)
    {
        _tasks = tasks;
        Text = Strings.Get("Dialog.Exit.Title");
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(480, Math.Max(190, 92 + tasks.Count * 36));

        _summary = new Label
        {
            AutoSize = true,
            Text = Strings.Get("Dialog.Exit.Preparing"),
            Location = new Point(24, 20),
        };
        Controls.Add(_summary);

        _taskTable = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = tasks.Count,
            Location = new Point(24, 54),
            Size = new Size(432, Math.Max(30, tasks.Count * 32)),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        _taskTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        _taskTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        for (var i = 0; i < tasks.Count; i++)
        {
            _taskTable.Controls.Add(new Label
            {
                AutoSize = true,
                Text = tasks[i].Name,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 0, 0),
            }, 0, i);
            _taskTable.Controls.Add(new Label
            {
                AutoSize = true,
                Text = Strings.Get("Dialog.Exit.Pending"),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 0, 0),
            }, 1, i);
        }
        Controls.Add(_taskTable);

        _progress = new ProgressBar
        {
            Minimum = 0,
            Maximum = Math.Max(1, tasks.Count),
            Value = 0,
            Style = ProgressBarStyle.Continuous,
            Location = new Point(24, ClientSize.Height - 34),
            Size = new Size(432, 12),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        Controls.Add(_progress);
    }

    public async Task RunAsync()
    {
        Show();
        BringToFront();
        try
        {
            for (var i = 0; i < _tasks.Count; i++)
            {
                SetTaskState(i, Strings.Get("Dialog.Exit.InProgress"));
                _summary.Text = string.Format(Strings.Get("Dialog.Exit.Progress"), i + 1, _tasks.Count);
                try
                {
                    await _tasks[i].Action();
                    SetTaskState(i, Strings.Get("Dialog.Exit.Completed"));
                }
                catch (Exception ex)
                {
                    _failedTasks++;
                    SetTaskState(i, Strings.Get("Dialog.Exit.Failed"));
                    _summary.Text = ex.Message;
                }
                _progress.Value = i + 1;
            }

            _summary.Text = _failedTasks == 0
                ? Strings.Get("Dialog.Exit.Finished")
                : string.Format(Strings.Get("Dialog.Exit.FinishedWithFailures"), _failedTasks);
        }
        finally
        {
            Close();
        }
    }

    private void SetTaskState(int index, string state)
    {
        if (_taskTable.GetControlFromPosition(1, index) is Label label)
            label.Text = state;
    }
}
