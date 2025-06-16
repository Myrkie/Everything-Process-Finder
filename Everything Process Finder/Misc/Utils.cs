using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;
using Serilog;

namespace Everything_Process_Finder.Misc;

public static class Utils
{
    private static readonly ILogger Logger = Log.ForContext(typeof(TrayIcon));
    private const string AppName = "Everything Process Finder";
    
    public static NotifyIcon? NotifyIcon;
    public static Image? NotifyImage;

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

    public static void LoadResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            Logger.Information("Discovered Resources: {res}", resource);
        }
        var imageStream = assembly.GetManifestResourceStream("Everything_Process_Finder.res.Icon.ico");
        if (imageStream == null)
            throw new Exception("Couldn't find embedded resource");

        NotifyImage = Image.FromStream(imageStream);
        
        // reset seek and reuse stream
        imageStream.Seek(0, SeekOrigin.Begin);
        
        using var icon = new Icon(imageStream);
        NotifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = AppName
        };
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
        Logger.Information("The application requires elevated privileges.");
            
        Process.Start(startInfo);
        Environment.Exit(0);
    }
}