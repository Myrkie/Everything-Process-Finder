using Everything_Process_Finder.Configuration;
using Serilog;
using Timer = System.Windows.Forms.Timer;

namespace Everything_Process_Finder.Misc
{
    public static class MonitorEverythingWindow
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MonitorEverythingWindow));

        private static IntPtr _lastEverythingHandle = IntPtr.Zero;
        private static Timer? _timer;
        
        public static event EventHandler<EverythingEventArgs>? EverythingWindowFound;
        public static event EventHandler? EverythingWindowClosed;
        
        public class EverythingEventArgs(IntPtr hEverything) : EventArgs
        {
            public IntPtr HEverything { get; } = hEverything;
        }

        public static void Init()
        {
            _timer = new Timer
            {
                Interval = Config.Instance.WindowCheckLoopInterval * 1000
            };
            _timer.Tick += (_, _) => MonitorEverything();
            _timer.Start();
        }

        private static void MonitorEverything()
        {
            var (currentHandle, alphaInstance) = MiscNativeMethods.FindEverythingWindowHandle();

            if (currentHandle != IntPtr.Zero && _lastEverythingHandle == IntPtr.Zero)
            {
                _lastEverythingHandle = currentHandle;
                Logger.Information("Everything window found. AlphaInstance:{Instance}", alphaInstance);
                EverythingWindowFound?.Invoke(null, new EverythingEventArgs(currentHandle));
            }
            else if (currentHandle == IntPtr.Zero && _lastEverythingHandle != IntPtr.Zero)
            {
                _lastEverythingHandle = IntPtr.Zero; 
                Logger.Information("Everything window lost. AlphaInstance:{Instance}", alphaInstance);
                EverythingWindowClosed?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}