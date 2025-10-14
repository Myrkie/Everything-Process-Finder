using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Serilog;

namespace Everything_Process_Finder.Misc
{
    public static partial class ProcessHelper
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(ProcessHelper));

        // --- Win32 constants ---
        private const uint WmProcessQueryLimitedInformation = 0x1000;
        private const int WmSecurityImpersonation = 2;
        private const int WmTokenPrimary = 1;
        private const int WmCreateNewConsole = 0x00000010;

        [Flags]
        private enum TokenAccess : uint
        {
            AssignPrimary = 0x0001,
            Duplicate = 0x0002,
            Query = 0x0008,
            AdjustDefault = 0x0080,
            AdjustSessionId = 0x0100
        }

        // --- Structs ---
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        // --- Safe handles ---
        private sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeProcessHandle(IntPtr handle, bool ownsHandle = true) : base(ownsHandle) => SetHandle(handle);
            protected override bool ReleaseHandle() => CloseHandle(handle) != 0;
        }

        private sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeTokenHandle(IntPtr handle, bool ownsHandle = true) : base(ownsHandle) => SetHandle(handle);
            protected override bool ReleaseHandle() => CloseHandle(handle) != 0;
        }

        // --- Win32 imports ---
        [LibraryImport("user32.dll")]
        private static partial IntPtr GetShellWindow();

        [LibraryImport("user32.dll")]
        private static partial void GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenProcessToken(IntPtr processHandle, TokenAccess desiredAccess, out IntPtr tokenHandle);

        [LibraryImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DuplicateTokenEx(
            IntPtr existingToken,
            TokenAccess desiredAccess,
            IntPtr tokenAttributes,
            int impersonationLevel,
            int tokenType,
            out IntPtr newToken);

        [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateProcessWithTokenW(
            IntPtr hToken,
            uint dwLogonFlags,
            string? lpApplicationName,
            string lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial int CloseHandle(IntPtr handle);

        public static void StartAsStandardUser(string target, string? arguments = null)
        {
            IntPtr shellWindow = GetShellWindow();
            if (shellWindow == IntPtr.Zero)
                throw new InvalidOperationException("Cannot find the shell window");

            GetWindowThreadProcessId(shellWindow, out var explorerPid);
            if (explorerPid == 0)
                throw new InvalidOperationException("Failed to get Explorer process ID.");

            IntPtr hProcessRaw = OpenProcess(WmProcessQueryLimitedInformation, false, explorerPid);
            using SafeProcessHandle hProcess = new(hProcessRaw);
            if (hProcess.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open Explorer process.");

            if (!OpenProcessToken(hProcess.DangerousGetHandle(),
                TokenAccess.Duplicate | TokenAccess.AssignPrimary | TokenAccess.Query,
                out var hShellTokenRaw))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open process token.");
            }
            
            using SafeTokenHandle hShellToken = new(hShellTokenRaw);

            if (!DuplicateTokenEx(
                    hShellToken.DangerousGetHandle(),
                    TokenAccess.AssignPrimary | TokenAccess.Duplicate | TokenAccess.Query | TokenAccess.AdjustDefault | TokenAccess.AdjustSessionId,
                    IntPtr.Zero,
                    WmSecurityImpersonation,
                    WmTokenPrimary,
                    out var hUserTokenRaw))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to duplicate token.");
            }

            using SafeTokenHandle hUserToken = new(hUserTokenRaw);

            StartupInfo si = new() { cb = Marshal.SizeOf<StartupInfo>() };
            string cmdLine = arguments is { Length: > 0 } ? $"explorer.exe \"{target}\" {arguments}" : $"explorer.exe \"{target}\"";

            if (!CreateProcessWithTokenW(hUserToken.DangerousGetHandle(), 0, null, cmdLine, WmCreateNewConsole, IntPtr.Zero, null, ref si, out var pi))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to start process with token.");

            Logger.Information("Started '{Target}' as standard user (PID={Pid})", target, pi.dwProcessId);
        }
    }
}
