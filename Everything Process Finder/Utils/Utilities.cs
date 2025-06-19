using System.Diagnostics;
using System.Security.Principal;
using Everything_Process_Finder.Configuration;
using Everything_Process_Finder.Misc;
using Microsoft.Win32;
using Serilog;

namespace Everything_Process_Finder.Utils
{
    public static class Utilities
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(Utilities));
        internal const string AppName = "Everything Process Finder";

        internal static void AutoStartup()
        {
            try
            {
                RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (Config.Instance.RunOnStartup)
                {
                    if (TrayIcon.CheckboxAutoStartMenuItem != null) TrayIcon.CheckboxAutoStartMenuItem.Checked = true;
                    Config.Instance.RunOnStartup = true;
                    rk?.SetValue(AppName, Application.ExecutablePath);
                }
                else
                {
                    if (TrayIcon.CheckboxAutoStartMenuItem != null) TrayIcon.CheckboxAutoStartMenuItem.Checked = false;
                    Config.Instance.RunOnStartup = false;
                    rk?.DeleteValue(AppName, false);
                }
            }
            catch (Exception exception)
            {
                Logger.Error("An error has occured {ex}", exception);
                throw;
            }
        }

        // ReSharper disable once NotAccessedField.Local
        private static Mutex _mutex = null!;

        internal static void SingleInstanceCheck()
        {
            Thread.Sleep(2000); // let's wait a bit to let any previous ones close before checking.
            _mutex = new Mutex(true, AppName, out var createdNew);
            if (createdNew) return;
            var str = AppName + " is already running.";
            Logger.Information(str);
            MessageBox.Show(str, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Environment.Exit(0);
        }

        public static void FocusEverything()
        {
            string uri = "es:";
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            Logger.Information("Everything window focused.");
        }

        internal static void RestartApp()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            };
            Logger.Information("Restarting.");
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        internal static void EnsureElevatedPrivileges()
        {
            if (Debugger.IsAttached)
            {
                Logger.Information("Debugger attached, not elevating, selecting elevated processes will fail.");
                return;
            }
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return;

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            };
            Logger.Information("The application requires elevated privileges.");

            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}