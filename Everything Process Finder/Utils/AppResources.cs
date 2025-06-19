using System.Reflection;
using Serilog;

namespace Everything_Process_Finder.Utils
{
    public static class AppResources
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(AppResources));

        public static NotifyIcon? NotifyIcon;
        public static Image? NotifyImage;
        public static Image? ConsoleImage;
        private static Icon? _appIcon;
        
        public static void LoadResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                Logger.Information("Discovered Resources: {res}", resource);
            }

            var mainIconStream = assembly.GetManifestResourceStream("Everything_Process_Finder.res.Icon.ico");
            var consoleIconStream = assembly.GetManifestResourceStream("Everything_Process_Finder.res.Console.ico");
            
            if (mainIconStream == null)
            {
                Logger.Error("Could not find mainIconStream resource");
                throw new Exception("Couldn't find mainIconStream resource");
            }
            
            if (consoleIconStream == null)
            {
                Logger.Error("Could not find consoleIconStream resource");
                throw new Exception("Couldn't find consoleIconStream resource");
            }

            NotifyImage = Image.FromStream(mainIconStream);
            ConsoleImage =  Image.FromStream(consoleIconStream);

            mainIconStream.Seek(0, SeekOrigin.Begin);

            _appIcon = new Icon(mainIconStream);
            NotifyIcon = new NotifyIcon
            {
                Icon = _appIcon,
                Visible = true,
                Text = Utilities.AppName
            };
        }
    }
}