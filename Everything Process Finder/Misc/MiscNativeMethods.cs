using System.Runtime.InteropServices;
using Serilog;

// ReSharper disable IdentifierTypo
namespace Everything_Process_Finder.Misc
{ 
    public static partial class MiscNativeMethods
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MiscNativeMethods));

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr FindWindowW(string lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string? lpszWindow);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        private static partial void GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [LibraryImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
        private static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void CloseHandle(IntPtr hObject);

        private const uint WmProcessQueryLimitedInformation = 0x1000;

        [LibraryImport("user32.dll", EntryPoint = "SetParent", SetLastError = true)]
        internal static partial void SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool QueryFullProcessImageNameW(
            IntPtr hProcess,
            int dwFlags,
            Span<char> lpExeName,
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

            IntPtr hProcess = OpenProcess(WmProcessQueryLimitedInformation, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                Logger.Error("Open process failed");
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                const int maxPath = 260;
                Span<char> buffer = stackalloc char[maxPath];
                int size = buffer.Length;

                bool success = QueryFullProcessImageNameW(hProcess, 0, buffer, ref size);
                if (success)
                    return new string(buffer.Slice(0, size));
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
            var handle = FindWindowW("EVERYTHING", null);

            if (handle != IntPtr.Zero)
            {
                _lastAlphaInstance = false;
                return (handle, _lastAlphaInstance);
            }
            handle = FindWindowW("EVERYTHING_(1.5a)", null);
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