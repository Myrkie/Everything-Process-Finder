using Everything_Process_Finder.Forms;
using Everything_Process_Finder.Misc;
using Everything_Process_Finder.Utils;
using Serilog;

namespace Everything_Process_Finder
{
    class Program
    {
        private static readonly ILogger Logger = Log.ForContext<Program>();

        [STAThread]
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File(
                    path: "logs/log-.txt",
                    outputTemplate:
                    "[{Timestamp:MM-dd-yyyy HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 4,
                    shared: true)
                .CreateLogger();

#if !DEBUG
            Utilities.EnsureElevatedPrivileges();
#endif
            AppResources.LoadResources();
            Utilities.SingleInstanceCheck();

            Utilities.RegisterAutoStart();

            var trayIcon = new TrayIcon();

            var findButton = new FindWindowButton
            {
                Left = 400,
                Top = 0,
                Width = 22,
                Height = 22,
            };
            findButton.WindowFound += Utilities.QueryEverything;

            MonitorEverythingWindow.EverythingWindowFound += (_, hEverything) =>
            {
                trayIcon.SetConState(true);
                
                MiscNativeMethods.SetParent(findButton.Handle, hEverything);
            };
            MonitorEverythingWindow.EverythingWindowClosed += (_, _) => { trayIcon.SetConState(false); };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => MonitorEverythingWindow.Dispose();

            MonitorEverythingWindow.Init();

            Application.Run();
        }
    }
}
