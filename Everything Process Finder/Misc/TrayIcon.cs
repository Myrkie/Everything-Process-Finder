using Serilog;

namespace Everything_Process_Finder.Misc
{
    public class TrayIcon
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(TrayIcon));

        private static NotifyIcon? _trayIcon;
        private ToolStripMenuItem? _connectionStatusItem;
        
        public static ToolStripMenuItem? CheckboxAutoStartMenuItem => _checkboxAutoStartMenuItem;
        private static ToolStripMenuItem? _checkboxAutoStartMenuItem;
        
        public static ToolStripMenuItem? CheckBoxShowConsoleMenuItem => _checkboxShowConsoleMenuItem;
        private static ToolStripMenuItem? _checkboxShowConsoleMenuItem;



        public void Build()
        {
            _trayIcon = Utils.NotifyIcon;
            if (_trayIcon == null) return;
            _trayIcon.DoubleClick += (_, _) => { Utils.FocusEverything(); };

            // context menu
            var contextMenu = new ContextMenuStrip();

            // connection status
            _connectionStatusItem = new ToolStripMenuItem("Connection Status")
            {
                Image = CreateStatusIconImage(false),
                CheckOnClick = false
            };
            _connectionStatusItem.Click += (_, _) => { Utils.FocusEverything(); };
            contextMenu.Items.Add(_connectionStatusItem);


            // auto start
            _checkboxAutoStartMenuItem = new ToolStripMenuItem("Start with Windows");
            _checkboxAutoStartMenuItem.CheckOnClick = true;
            _checkboxAutoStartMenuItem.CheckedChanged += (_, _) =>
            {
                Utils.AutoStartup();
                Config.Instance.SaveConfig();
            };
            contextMenu.Items.Add(_checkboxAutoStartMenuItem);


            // show hide console option
            _checkboxShowConsoleMenuItem = new ToolStripMenuItem("Show Console");
            _checkboxShowConsoleMenuItem.CheckOnClick = true;
            _checkboxShowConsoleMenuItem.CheckedChanged += (_, _) =>
            {
                ConsoleManager.ToggleConsole(_checkboxShowConsoleMenuItem);
                Logger.Information("showing window");
            };
            contextMenu.Items.Add(_checkboxShowConsoleMenuItem);

            contextMenu.Items.Add("Exit", Utils.NotifyImage, (_, _) => { Application.Exit(); });

            _trayIcon.ContextMenuStrip = contextMenu;
        }
        
        public void SetConState(bool isConnected)
        {
            if (_connectionStatusItem != null) _connectionStatusItem.Image = CreateStatusIconImage(isConnected);
        }

        private Image CreateStatusIconImage(bool isConnected)
        {
            int size = 16;
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            Brush brush = isConnected ? Brushes.LimeGreen : Brushes.Red;
            g.FillEllipse(brush, 2, 2, size - 4, size - 4);
            return bitmap;
        }
    }
}