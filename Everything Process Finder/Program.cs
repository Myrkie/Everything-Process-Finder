using System.Diagnostics;
using System.Net;
using Everything_Process_Finder.Misc;
using Serilog;

namespace Everything_Process_Finder;

class Program
{
    private static readonly ILogger Logger = Log.ForContext(typeof(Program));
    static IntPtr _lastEverythingHandle = IntPtr.Zero;

    [STAThread]
    static void Main()
    { 
        // be nice to debugger
        if (!Console.IsOutputRedirected)
        {
            Utils.EnsureElevatedPrivileges(); 
        }
   
        Utils.SingleInstanceCheck();
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
            .CreateLogger();
        
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

            var procpath = NativeMethods.GetProcessPathByWindowHandle(handle);
            var process = Path.GetFileNameWithoutExtension(procpath);

            Logger.Information($"Found window: Handle: {handle} Title: {title} Process: {process}", handle, title, process);

            string targetPath = "";

            Structs.Modifier modifier = Structs.Modifier.None;

            if (shiftPressed)
                modifier = Structs.Modifier.Shift;
            else if (ctrlPressed)
                modifier = Structs.Modifier.Control;

            switch (modifier)
            {
                
                case Structs.Modifier.Shift:{
                    string? folder = Path.GetDirectoryName(procpath);
                    targetPath = !string.IsNullOrEmpty(folder) ? folder : procpath;
                    Logger.Information("Shift: searching by process folder.");
                    break; 
                } 

                case Structs.Modifier.Control:
                {
                    string? folder = Path.GetDirectoryName(procpath);
                    targetPath = !string.IsNullOrEmpty(folder)
                        ? (folder.EndsWith("\\") ? folder : folder + "\\")
                        : procpath + "\\";
                    Logger.Information("Ctrl held: searching by folder path.");
                    break;
                }
                
                case Structs.Modifier.None:
                {
                    targetPath = procpath;
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
        var timer = new System.Windows.Forms.Timer
        {
            Interval = 1000 
        };
        timer.Tick += (_, _) => MonitorEverythingWindow(findButton);
        timer.Start();

        Application.Run();
    }

    private static void MonitorEverythingWindow(Control button)
    {
        IntPtr currentHandle = NativeMethods.FindWindow("EVERYTHING", null);

        if (currentHandle != IntPtr.Zero && currentHandle != _lastEverythingHandle)
        {
            _lastEverythingHandle = currentHandle;
            Logger.Information("Everything window found. Reattaching button...");
            _assemble(button.Handle);
        }
        else if (currentHandle == IntPtr.Zero && _lastEverythingHandle != IntPtr.Zero)
        {
            Logger.Information("Everything window closed.");
            _lastEverythingHandle = IntPtr.Zero;
        }
    }

    private static void _assemble(IntPtr handle)
    {
        IntPtr hEverything = NativeMethods.FindWindow("EVERYTHING", null);
        if (hEverything == IntPtr.Zero)
        {
            Logger.Error("Everything window not found.");
            return;
        }

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
