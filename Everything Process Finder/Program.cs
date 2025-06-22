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

            Utilities.AutoStartup();

            var findButton = new FindWindowButton
            {
                Left = 400,
                Top = 0,
                Width = 22,
                Height = 22,
            };
            findButton.WindowFound += Utilities.QueryEverything;

            // Start polling for Void Tools Everything window
            MonitorEverythingWindow.EverythingWindowFound += (_, hEverything) =>
            {
                TrayIcon.SetConState(true);
                _assemble(hEverything, findButton.Handle);
            };
            MonitorEverythingWindow.EverythingWindowClosed += (_, _) => { TrayIcon.SetConState(false); };

            MonitorEverythingWindow.Init();

            Application.Run();
        }

        private static void _assemble(IntPtr hEverything, IntPtr findButtonHandle)
        {
            Logger.Information("Attaching button to Everything's toolbar.");

            var hToolbar = MiscNativeMethods.FindEverythingToolbar(hEverything);
            if (hToolbar == IntPtr.Zero) return;
            
            Logger.Information("Attached button to Everything's toolbar.");
            MiscNativeMethods.SetParent(findButtonHandle, hToolbar);
        }
    }
}
