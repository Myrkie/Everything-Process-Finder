using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace Everything_Process_Finder.Misc
{
    public static partial class ConsoleManager
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(ConsoleManager));
        
        private static bool _consoleVisible;
        
        public static void ToggleConsole(ToolStripMenuItem consoleMenuItem)
        {
            if (_consoleVisible)
            {
                FreeConsoleWindow();
                _consoleVisible = false;
                consoleMenuItem.Checked = false;
            }
            else
            {
                Logger.Information("Console enabled");
                AllocConsole();
                EnableAnsiSupport();
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));

                ShowConsole();
                _consoleVisible = true;
                consoleMenuItem.Checked = true;
            }
        }
        
        private static void FreeConsoleWindow()
        {
            if (GetConsoleWindow() == IntPtr.Zero) return;
            FreeConsole();
        }
        
        private static void ShowConsole()
        {
            SetForegroundWindow(GetConsoleWindow());
        }
        
        private static void EnableAnsiSupport()
        {
            var handle = GetStdHandle(WmStdOutputHandle);
            if (!GetConsoleMode(handle, out var mode)) return;
            mode |= WmEnableVirtualTerminalProcessing;
            SetConsoleMode(handle, mode);
        }
        
        // --- Win32 API Imports and Constants ---
        
        [LibraryImport("kernel32.dll", EntryPoint = "AllocConsole")]
        private static partial void AllocConsole();
        
        [LibraryImport("kernel32.dll", EntryPoint = "FreeConsole")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void FreeConsole();

        [LibraryImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleWindow")]
        private static partial IntPtr GetConsoleWindow();
        
        [LibraryImport("kernel32.dll", EntryPoint = "GetConsoleMode", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [LibraryImport("kernel32.dll", EntryPoint = "SetConsoleMode", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial void SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
        private static partial IntPtr GetStdHandle(int nStdHandle);

        private const int WmStdOutputHandle = -11;
        private const uint WmEnableVirtualTerminalProcessing = 0x0004;
    }
}