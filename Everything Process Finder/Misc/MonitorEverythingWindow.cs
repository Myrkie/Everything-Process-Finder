using Serilog;

namespace Everything_Process_Finder.Misc
{
    public static class MonitorEverythingWindow
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MonitorEverythingWindow));

        private static IntPtr _lastEverythingHandle = IntPtr.Zero;
        private static IntPtr _winEventHook = IntPtr.Zero;
        private static MiscNativeMethods.WinEventDelegate? _winEventDelegate;

        public static event EventHandler<IntPtr>? EverythingWindowFound;
        public static event EventHandler? EverythingWindowClosed;

        public static void Init()
        {
            _winEventDelegate = OnWinEvent;

            _winEventHook = MiscNativeMethods.SetWinEventHook(
                MiscNativeMethods.EventObjectCreate,
                MiscNativeMethods.EventObjectDestroy,
                IntPtr.Zero,
                _winEventDelegate,
                0,
                0,
                MiscNativeMethods.WineventOutofcontext
            );

            Logger.Information("Window monitor initialized.");

            IntPtr hwnd = MiscNativeMethods.FindWindowW("EVERYTHING", null);
            if (hwnd == IntPtr.Zero)
                hwnd = MiscNativeMethods.FindWindowW("EVERYTHING_(1.5a)", null);

            HandleEverythingWindowFound(hwnd);
        }
        private static void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != 0 || hwnd == IntPtr.Zero)
                return;

            switch (eventType)
            {
                case MiscNativeMethods.EventObjectCreate:
                    HandleEverythingWindowFound(hwnd);
                    break;
                case MiscNativeMethods.EventObjectDestroy when _lastEverythingHandle != hwnd:
                    return;
                case MiscNativeMethods.EventObjectDestroy:
                    var className = MiscNativeMethods.GetClassName(hwnd);
                    bool isAlphaInstance = className == "EVERYTHING_(1.5a)";
                    Logger.Information("Everything window closed. Class: {Class} | AlphaInstance: {AlphaInstance}", className, isAlphaInstance);
                    _lastEverythingHandle = IntPtr.Zero;
                    EverythingWindowClosed?.Invoke(null, EventArgs.Empty);
                    break;
            }
        }
        private static void HandleEverythingWindowFound(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            var className = MiscNativeMethods.GetClassName(hwnd);
            if (className is not ("EVERYTHING" or "EVERYTHING_(1.5a)"))
                return;

            bool isAlphaInstance = className == "EVERYTHING_(1.5a)";
            _lastEverythingHandle = hwnd;

            Logger.Information("Everything window found. Class: {Class} | AlphaInstance: {AlphaInstance}", className, isAlphaInstance);
            EverythingWindowFound?.Invoke(null, hwnd);
        }
    
        public static void Dispose()
        {
            if (_winEventHook == IntPtr.Zero) return;
            MiscNativeMethods.UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
            Logger.Information("Window monitor hook disposed.");
        }
    }
}