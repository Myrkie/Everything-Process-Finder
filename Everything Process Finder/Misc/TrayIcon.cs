using Everything_Process_Finder.Configuration;
using Everything_Process_Finder.Utils;
using Serilog;

namespace Everything_Process_Finder.Misc
{
    public class TrayIcon
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(TrayIcon));

        private static readonly NotifyIcon? NotifyIcon;
        
        // ReSharper disable MemberCanBePrivate.Global
        public static readonly ToolStripMenuItem? ConnectionStatusItem;
        public static readonly ToolStripMenuItem? CheckboxAutoStartMenuItem;
        public static readonly ToolStripMenuItem? CheckBoxShowConsoleMenuItem;
        public static readonly ToolStripMenuItem? RestartAppMenuItem;

        static TrayIcon()
        {
            Logger.Information("Building tray icon.");
            NotifyIcon = AppResources.NotifyIcon;
            if (NotifyIcon == null) return;
            NotifyIcon.DoubleClick += (_, _) => { Utilities.FocusEverything(); };

            // context menu
            var contextMenu = new ContextMenuStrip();

            // connection status
            ConnectionStatusItem = new ToolStripMenuItem("Connection Status")
            {
                Image = CreateStatusIconImage(false),
                CheckOnClick = false
            };
            ConnectionStatusItem.Click += (_, _) => { Utilities.FocusEverything(); };
            contextMenu.Items.Add(ConnectionStatusItem);


            // auto start
            CheckboxAutoStartMenuItem = new ToolStripMenuItem("Start with Windows");
            CheckboxAutoStartMenuItem.CheckOnClick = true;
            CheckboxAutoStartMenuItem.CheckedChanged += (_, _) =>
            {
                Utilities.AutoStartup();
                Config.Instance.SaveConfig();
            };
            contextMenu.Items.Add(CheckboxAutoStartMenuItem);


            // show hide console option
            CheckBoxShowConsoleMenuItem = new ToolStripMenuItem("Show Console", AppResources.ConsoleImage);
            CheckBoxShowConsoleMenuItem.CheckOnClick = true;
            CheckBoxShowConsoleMenuItem.CheckedChanged += (_, _) =>
            {
                ConsoleManager.ToggleConsole(CheckBoxShowConsoleMenuItem);
            };
            contextMenu.Items.Add(CheckBoxShowConsoleMenuItem);
            
            RestartAppMenuItem = new ToolStripMenuItem("Restart");
            RestartAppMenuItem.Click += (_, _) => { Utilities.RestartApp(); };
            contextMenu.Items.Add(RestartAppMenuItem);
            
            contextMenu.Items.Add("Exit", AppResources.NotifyImage, (_, _) => { Application.Exit(); });
            
            NotifyIcon.ContextMenuStrip = contextMenu;
            Logger.Information("Tray icon built.");
        }
        
        public static void SetConState(bool isConnected)
        {
            Logger.Information("Setting connection state to {connection}.", isConnected);
            if (ConnectionStatusItem != null) ConnectionStatusItem.Image = CreateStatusIconImage(isConnected);
        }

        private static Image CreateStatusIconImage(bool isConnected)
        {
            int size = 256;
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            Brush brush = isConnected ? Brushes.LimeGreen : Brushes.Red;
            g.FillEllipse(brush, 2, 2, size - 4, size - 4);
            return bitmap;
        }
    }
}