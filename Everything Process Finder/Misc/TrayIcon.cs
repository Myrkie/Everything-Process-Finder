using Serilog;

namespace Everything_Process_Finder.Misc
{
    public class TrayIcon
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(TrayIcon));

        public static ToolStripMenuItem? CheckboxMenuItem => _checkboxMenuItem;
        private static NotifyIcon? _trayIcon;
        private static ToolStripMenuItem? _checkboxMenuItem;

        public void Build()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.Visible = true;
            _trayIcon.Text = "Everything Process Finder";
            var contextMenu = new ContextMenuStrip();
            _checkboxMenuItem = new ToolStripMenuItem("Start with Windows");
            _checkboxMenuItem.CheckOnClick = true;
            _checkboxMenuItem.CheckedChanged += CheckboxMenuItem_CheckedChanged;
            contextMenu.Items.Add(_checkboxMenuItem);
            contextMenu.Items.Add("Show/Hide", null, (s, e) =>
            {
                Logger.Information("showing window");
                ConsoleManager.ToggleConsole();
            });
            contextMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });


            _trayIcon.ContextMenuStrip = contextMenu;
        }

        private static void CheckboxMenuItem_CheckedChanged(object? sender, EventArgs e)
        {
            switch (sender)
            {
                case ToolStripMenuItem { Checked: true }:
                    Utils.AutoStartup();
                    break;
            }

        }
    }
}