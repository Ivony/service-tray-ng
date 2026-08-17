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
                Strings.Get("App.AlreadyRunning.Message"),
                Strings.Get("App.Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
