namespace LoginGuardUI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show($"An unexpected UI error occurred: {e.Exception.Message}",
                "LoginGuard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"An unhandled error occurred: {ex.Message}",
                    "LoginGuard Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.Run(new MainForm());
    }
}
