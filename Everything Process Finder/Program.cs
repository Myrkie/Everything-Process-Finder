using System.Diagnostics;
using System.Net;
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
            Utilities.SingleInstanceCheck();
            AppResources.LoadResources();

            var trayIcon = new TrayIcon();
            trayIcon.Build();
            Utilities.AutoStartup();

            var findButton = new FindWindowButton
            {
                Left = 400,
                Top = 0,
                Width = 22,
                Height = 22,
            };

            findButton.WindowFound += (handle, title) =>
            {
                if (handle == IntPtr.Zero) return;

                bool shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
                bool ctrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;

                var processPathByWindowHandle = MiscNativeMethods.GetProcessPathByWindowHandle(handle);
                var process = Path.GetFileNameWithoutExtension(processPathByWindowHandle);

                Logger.Information($"Found window: Handle: {handle} Title: {title} Process: {process}", handle, title,
                    process);

                string targetPath = "";

                Structs.Modifier modifier = Structs.Modifier.None;

                if (shiftPressed)
                    modifier = Structs.Modifier.Shift;
                else if (ctrlPressed)
                    modifier = Structs.Modifier.Control;

                switch (modifier)
                {
                    case Structs.Modifier.Shift:
                    {
                        string? folder = Path.GetDirectoryName(processPathByWindowHandle);
                        targetPath = !string.IsNullOrEmpty(folder) ? folder : processPathByWindowHandle;
                        Logger.Information("Shift: searching by process folder.");
                        break;
                    }

                    case Structs.Modifier.Control:
                    {
                        string? folder = Path.GetDirectoryName(processPathByWindowHandle);
                        targetPath = !string.IsNullOrEmpty(folder)
                            ? folder.EndsWith("\\") ? folder : folder + "\\"
                            : processPathByWindowHandle + "\\";
                        Logger.Information("Ctrl held: searching by folder path.");
                        break;
                    }

                    case Structs.Modifier.None:
                    {
                        targetPath = processPathByWindowHandle;
                        Logger.Information("No modifiers: searching by process executable.");
                        break;
                    }

                }

                string encodedQuery = WebUtility.UrlEncode($"\"{targetPath}\"");
                string uri = $"es:{encodedQuery}";
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

                Logger.Information("Search sent to Everything UI via es: protocol.");
            };

            // Start polling for Void Tools Everything window
            MonitorEverythingWindow.EverythingWindowFound += (_, e) =>
            {
                trayIcon.SetConState(true);
                _assemble(e.HEverything, findButton.Handle);
            };
            MonitorEverythingWindow.EverythingWindowClosed += (_, _) => { trayIcon.SetConState(false); };

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
