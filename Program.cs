using System;
using System.Threading;
using System.Windows.Forms;

namespace SmoothScroll;

internal static class Program
{
    private const string MutexName = "SmoothScroll_SingleInstance_Mutex";
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "SmoothScroll is already running.",
                "SmoothScroll",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            using var app = new SmoothScrollApp();
            Application.Run(new TrayApplicationContext(app));
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }
}

internal class TrayApplicationContext : ApplicationContext
{
    private readonly SmoothScrollApp _app;

    public TrayApplicationContext(SmoothScrollApp app)
    {
        _app = app;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _app.Dispose();
        }
        base.Dispose(disposing);
    }
}
