using System.Runtime.InteropServices;
using System.Text;
using Serilog;

// ReSharper disable IdentifierTypo
namespace Everything_Process_Finder.Misc
{ 
    public static class MiscNativeMethods
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MiscNativeMethods));

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string? lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        const uint ProcessQueryLimitedInformation = 0x1000;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName,
            ref int lpdwSize);

        public static string GetProcessPathByWindowHandle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                Logger.Error("Invalid window handle");
                throw new ArgumentException("Invalid window handle.");
            }

            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == 0)
            {
                Logger.Error("Invalid process id");
                throw new InvalidOperationException("Failed to get process ID from window handle.");
            }

            IntPtr hProcess = OpenProcess(ProcessQueryLimitedInformation, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                Logger.Error("Open process failed");
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                const int maxPath = 260;
                StringBuilder buffer = new StringBuilder(maxPath);
                int size = buffer.Capacity;

                bool success = QueryFullProcessImageName(hProcess, 0, buffer, ref size);
                if (success) return buffer.ToString(0, size);
                Logger.Error("QueryFullProcessImageName failed");
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                Logger.Information("closing process handle");
                CloseHandle(hProcess);
            }
        }
        
        // tuple resets the value and we want to keep it
        private static bool _lastAlphaInstance;
        public static (IntPtr, bool) FindEverythingWindowHandle()
        {
            var handle = FindWindow("EVERYTHING", null);

            if (handle != IntPtr.Zero)
            {
                _lastAlphaInstance = false;
                return (handle, _lastAlphaInstance);
            }
            handle = FindWindow("EVERYTHING_(1.5a)", null);
            if (handle != IntPtr.Zero)
            {
                _lastAlphaInstance = true;
            }
            return (handle, _lastAlphaInstance);
        }

        public static IntPtr FindEverythingToolbar(IntPtr hEverything)
        {
            const int maxRetries = 20;
            int retryDelayMs = 100;

            IntPtr hToolbar = IntPtr.Zero;
            for (int i = 0; i < maxRetries; i++)
            {
                hToolbar = FindWindowEx(hEverything, IntPtr.Zero, "EVERYTHING_MENUBAR", null);
                if (hToolbar != IntPtr.Zero)
                    break;

                Thread.Sleep(retryDelayMs);
            }

            if (hToolbar != IntPtr.Zero) return hToolbar;
            Logger.Error("EVERYTHING_MENUBAR control not found after waiting.");
            return IntPtr.Zero;
        }
    }
}