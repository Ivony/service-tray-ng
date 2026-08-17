using System.Windows.Forms;

namespace service_tray_ng;

public sealed class PortDialog : Form
{
    public int Port { get; private set; }

    public PortDialog(int currentPort)
    {
        Text = Strings.Get("Dialog.ChangePort");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 300;
        Height = 145;

        var label = new Label
        {
            Text = Strings.Get("Dialog.ServerPort"),
            Location = new Point(20, 18),
            AutoSize = true,
        };

        var input = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Value = currentPort,
            Location = new Point(20, 45),
            Width = 120,
        };

        var ok = new Button
        {
            Text = Strings.Get("Dialog.OK"),
            DialogResult = DialogResult.OK,
            Location = new Point(20, 80),
            Width = 80,
        };

        var cancel = new Button
        {
            Text = Strings.Get("Dialog.Cancel"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(110, 80),
            Width = 80,
        };

        ok.Click += (_, _) => Port = (int)input.Value;

        Controls.AddRange([label, input, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
