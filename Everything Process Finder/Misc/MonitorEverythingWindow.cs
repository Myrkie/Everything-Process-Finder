using Serilog;
using Timer = System.Windows.Forms.Timer;

namespace Everything_Process_Finder.Misc
{

    public static class MonitorEverythingWindow
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MonitorEverythingWindow));

        public static event EventHandler? EverythingWindowFound;
        public static event EventHandler? EverythingWindowClosed;

        private static IntPtr _lastEverythingHandle = IntPtr.Zero;
        private static Timer? _timer;

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
            IntPtr currentHandle = NativeMethods.FindWindow("EVERYTHING", null);

            if (currentHandle != IntPtr.Zero && currentHandle != _lastEverythingHandle)
            {
                _lastEverythingHandle = currentHandle;
                Logger.Information("Everything window found.");
                EverythingWindowFound?.Invoke(null, EventArgs.Empty);
            }
            else if (currentHandle == IntPtr.Zero && _lastEverythingHandle != IntPtr.Zero)
            {
                Logger.Information("Everything window closed.");
                _lastEverythingHandle = IntPtr.Zero;
                EverythingWindowClosed?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}