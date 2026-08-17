namespace service_tray_ng;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            using var context = new TrayApplicationContext();
            Application.Run(context);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(
                "Service Tray is already running in the notification area.",
                "Service Tray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
