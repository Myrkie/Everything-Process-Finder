using Everything_Process_Finder.Forms;
using Everything_Process_Finder.Misc;
using Everything_Process_Finder.Utils;
using Serilog;

namespace Everything_Process_Finder
{
    class Program
    {
        private const int WmBaseLeft = 400;
        private const int WmBaseTop = 0;
        private const int WmBaseWidth = 22;
        private const int WmBaseHeight = 22;
        
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

            var scale = Utilities.GetSystemScaleFactor();
            var findButton = new FindWindowButton
            {
                Top = (int)(WmBaseTop * scale),
                Left = (int)(WmBaseLeft * scale),
                Width = (int)(WmBaseWidth * scale),
                Height = (int)(WmBaseHeight * scale),
            };


            findButton.WindowFound += Utilities.QueryEverything;

            MonitorEverythingWindow.EverythingWindowFound += (_, hEverything) =>
            {
                trayIcon.SetConState(true);

                var windowScaleFactor = MiscNativeMethods.GetWindowScaleFactor(hEverything);

                findButton.Top = (int)(WmBaseTop * windowScaleFactor);
                findButton.Left = (int)(WmBaseLeft * windowScaleFactor);
                findButton.Width = (int)(WmBaseWidth * windowScaleFactor);
                findButton.Height = (int)(WmBaseHeight * windowScaleFactor);

                MiscNativeMethods.SetParent(findButton.Handle, hEverything);
            };

            MonitorEverythingWindow.EverythingWindowClosed += (_, _) => { trayIcon.SetConState(false); };
            AppDomain.CurrentDomain.ProcessExit += (_, _) => MonitorEverythingWindow.Dispose();

            MonitorEverythingWindow.Init();

            Application.Run();
        }
    }
}
