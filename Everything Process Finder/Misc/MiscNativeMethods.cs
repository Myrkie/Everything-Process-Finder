using System.Runtime.InteropServices;
using Serilog;

// ReSharper disable IdentifierTypo
namespace Everything_Process_Finder.Misc
{ 
    public static partial class MiscNativeMethods
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(MiscNativeMethods));

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr FindWindowW(string lpClassName, string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string? lpszWindow);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
        private static partial void GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [LibraryImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
        private static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        private static partial void CloseHandle(IntPtr hObject);

        private const uint WmProcessQueryLimitedInformation = 0x1000;

        [LibraryImport("user32.dll", EntryPoint = "SetParent", SetLastError = true)]
        internal static partial void SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool QueryFullProcessImageNameW(IntPtr hProcess, int dwFlags, Span<char> lpExeName, ref int lpdwSize);

        
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsWindow(IntPtr hWnd);

        
        [LibraryImport("user32.dll", EntryPoint = "SetWinEventHook", SetLastError = true)]
        public static partial IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [LibraryImport("user32.dll", EntryPoint = "UnhookWinEvent", SetLastError = true)]
        public static partial void UnhookWinEvent(IntPtr hWinEventHook);

        [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetClassName(IntPtr hWnd, Span<char> lpClassName, int nMaxCount);

        public static string? GetClassName(IntPtr hWnd)
        {
            Span<char> buffer = stackalloc char[256];
            int length = GetClassName(hWnd, buffer, buffer.Length);
            return length > 0 ? new string(buffer[..length]) : null;
        }

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        public const uint WineventOutofcontext = 0x0000;
        public const uint EventObjectCreate = 0x8000;
        public const uint EventObjectDestroy = 0x8001;


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
        
        public static IntPtr FindEverythingToolbar(IntPtr hEverything)
        {
            const int maxRetries = 10;
            const int retryDelayMs = 100;
            const int requiredStableCount = 2;

            IntPtr previousHandle = IntPtr.Zero;
            int stableCounter = 0;

            for (int i = 0; i < maxRetries; i++)
            {
                IntPtr hToolbar = FindWindowEx(hEverything, IntPtr.Zero,"EVERYTHING_MENUBAR", null);

                if (hToolbar != IntPtr.Zero && IsWindow(hToolbar))
                {
                    if (hToolbar == previousHandle)
                    {
                        stableCounter++;
                        if (stableCounter >= requiredStableCount)
                            return hToolbar;
                    }
                    else
                    {
                        previousHandle = hToolbar;
                        stableCounter = 1;
                    }
                }
                else
                {
                    previousHandle = IntPtr.Zero;
                    stableCounter = 0;
                }
                Thread.Sleep(retryDelayMs);
            }

            Logger.Error("Failed to find a stable EVERYTHING_MENUBAR control.");
            return IntPtr.Zero;
        }
    }
}