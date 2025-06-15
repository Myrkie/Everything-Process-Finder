using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace Everything_Process_Finder.Misc;

public class Utils
{
    const string appName = "Everything Process Finder";

    internal static void AutoStartup()
    {
        RegistryKey? rk =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (Config.Instance.RunOnStartup)
        {
            TrayIcon.CheckboxMenuItem.Checked = true;
            Config.Instance.RunOnStartup = true;
            rk?.SetValue("Everything Process Finder", Application.ExecutablePath);
        }
        else
        {
            TrayIcon.CheckboxMenuItem.Checked = false;
            Config.Instance.RunOnStartup = false;
            rk?.DeleteValue("Everything Process Finder", false);
        }
    }
    
    private static Mutex _mutex = null!;

    internal static void SingleInstanceCheck()
    {
        Thread.Sleep(2000); // let's wait a bit to let any previous ones close before checking.
        _mutex = new Mutex(true, appName, out var createdNew);
        if (createdNew) return;
        var str = appName + " is already running.";
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