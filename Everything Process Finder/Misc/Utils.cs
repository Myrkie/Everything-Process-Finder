using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace Everything_Process_Finder.Misc;

public class Utils
{
    public const string AppName = "Everything Process Finder";

    internal static void AutoStartup()
    {
        RegistryKey? rk =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (Config.Instance.RunOnStartup)
        {
            if (TrayIcon.CheckboxMenuItem != null) TrayIcon.CheckboxMenuItem.Checked = true;
            Config.Instance.RunOnStartup = true;
            rk?.SetValue(AppName, Application.ExecutablePath);
        }
        else
        {
            if (TrayIcon.CheckboxMenuItem != null) TrayIcon.CheckboxMenuItem.Checked = false;
            Config.Instance.RunOnStartup = false;
            rk?.DeleteValue(AppName, false);
        }
    }
    
    private static Mutex _mutex = null!;

    internal static void SingleInstanceCheck()
    {
        Thread.Sleep(2000); // let's wait a bit to let any previous ones close before checking.
        _mutex = new Mutex(true, AppName, out var createdNew);
        if (createdNew) return;
        var str = AppName + " is already running.";
        Console.WriteLine(str);
        MessageBox.Show(str);
        Environment.Exit(0);
    }
        
    internal static void EnsureElevatedPrivileges()
    {
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
        Console.WriteLine("The application requires elevated privileges.");
            
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}