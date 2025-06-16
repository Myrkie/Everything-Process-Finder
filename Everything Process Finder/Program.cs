using System.Diagnostics;
using System.Net;
using Everything_Process_Finder.Misc;
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
                .CreateLogger();
            // be nice to debugger
            if (!Console.IsOutputRedirected)
            {
                Utils.EnsureElevatedPrivileges();
            }

            Utils.SingleInstanceCheck();
            Utils.LoadResources();

            var trayIcon = new TrayIcon();
            trayIcon.Build();
            Utils.AutoStartup();

            var findButton = new FindWindowButton(true)
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

                var processPathByWindowHandle = NativeMethods.GetProcessPathByWindowHandle(handle);
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
                            ? (folder.EndsWith("\\") ? folder : folder + "\\")
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

                string encodedQuery = WebUtility.UrlEncode(targetPath);
                string uri = $"es:{encodedQuery}";
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

                Logger.Information("Search sent to Everything UI via es: protocol.");

            };

            // Start polling for Void Tools Everything window
            MonitorEverythingWindow.EverythingWindowFound += (_, _) =>
            {
                trayIcon.SetConState(true);
                _assemble(findButton.Handle);
            };
            MonitorEverythingWindow.EverythingWindowClosed += (_, _) => { trayIcon.SetConState(false); };

            MonitorEverythingWindow.Init();

            Application.Run();
        }

        private static void _assemble(IntPtr handle)
        {
            IntPtr hEverything = NativeMethods.FindWindow("EVERYTHING", null);
            if (hEverything == IntPtr.Zero)
            {
                Logger.Error("Everything window not found.");
                return;
            }

            // this is overkill?
            const int maxRetries = 20;
            int retryDelayMs = 100;

            IntPtr hToolbar = IntPtr.Zero;
            for (int i = 0; i < maxRetries; i++)
            {
                hToolbar = NativeMethods.FindWindowEx(hEverything, IntPtr.Zero, "EVERYTHING_MENUBAR", null);
                if (hToolbar != IntPtr.Zero)
                    break;

                Thread.Sleep(retryDelayMs);
            }

            if (hToolbar == IntPtr.Zero)
            {
                Logger.Error("EVERYTHING_MENUBAR control not found after waiting.");
                return;
            }

            Logger.Information("Attaching button to Everything's toolbar.");
            NativeMethods.SetParent(handle, hToolbar);
        }
    }
}
