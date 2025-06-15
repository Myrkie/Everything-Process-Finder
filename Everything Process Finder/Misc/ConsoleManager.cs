using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace Everything_Process_Finder.Misc
{
    public static class ConsoleManager
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(ConsoleManager));

        [DllImport("kernel32.dll")]
        private static extern int AllocConsole();
        
        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        
        private static void ShowConsole()
        {
            SetForegroundWindow(GetConsoleWindow());
        }
        
        private static void EnableAnsiSupport()
        {
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(handle, out var mode)) return;
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            SetConsoleMode(handle, mode);
        }

        private static bool _consoleVisible;

        public static void ToggleConsole()
        {
            if (_consoleVisible)
            {
                FreeConsoleWindow();
                _consoleVisible = false;
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
            }
        }


        
        private static void FreeConsoleWindow()
        {
            if (GetConsoleWindow() == IntPtr.Zero) return;
            FreeConsole();
        }
    }
}