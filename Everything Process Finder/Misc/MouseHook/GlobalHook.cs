using System.Runtime.InteropServices;


// ReSharper disable IdentifierTypo
namespace Everything_Process_Finder.Misc.MouseHook
{
    /// <summary>
    /// Abstract base class for Mouse hooks
    /// </summary>
    public abstract partial class GlobalHook
    {
        #region Windows API Code

        [StructLayout(LayoutKind.Sequential)]
        // ReSharper disable once ClassNeverInstantiated.Global
        protected class Point
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected class MouseLlHookStruct
        {
            public required Point pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
        protected static partial int SetWindowsHookExW(
            int idHook,
            HookProc? lpfn,
            IntPtr hMod,
            int dwThreadId);

        [LibraryImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", SetLastError = true)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
        private static partial void UnhookWindowsHookEx(int idHook);


        [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
        protected static partial int CallNextHookEx(
            int idHook,
            int nCode,
            int wParam,
            IntPtr lParam);
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        protected delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        protected const int WhMouseLl = 14;
        protected const int WmMousemove = 0x200;
        protected const int WmLbuttondown = 0x201;
        protected const int WmRbuttondown = 0x204;
        protected const int WmMbuttondown = 0x207;
        protected const int WmLbuttonup = 0x202;
        protected const int WmRbuttonup = 0x205;
        protected const int WmMbuttonup = 0x208;
        protected const int WmLbuttondblclk = 0x203;
        protected const int WmRbuttondblclk = 0x206;
        protected const int WmMbuttondblclk = 0x209;
        protected const int WmMousewheel = 0x020A;

        #endregion

        #region Private Variables

        protected int HookType;
        protected int HandleToHook;
        // ReSharper disable once MemberCanBePrivate.Global
        protected bool IsStarted;
        protected HookProc? HookCallback;

        #endregion

        #region Constructor

        protected GlobalHook()
        {
            AppDomain.CurrentDomain.ProcessExit += Application_ProcessExit;
        }

        #endregion

        #region Methods

        public void Start()
        {
            if (IsStarted || HookType == 0) return;
            // Make sure we keep a reference to this delegate!
            // If not, GC randomly collects it, and a NullReference exception is thrown
            HookCallback = HookCallbackProcedure;

            HandleToHook = SetWindowsHookExW(HookType, HookCallback, IntPtr.Zero, 0);


            // Were we able to successfully start hook?
            if (HandleToHook != 0)
            {
                IsStarted = true;
            }
        }

        private void Stop()
        {
            if (!IsStarted) return;
            UnhookWindowsHookEx(HandleToHook);

            IsStarted = false;
        }

        protected virtual int HookCallbackProcedure(int nCode, Int32 wParam, IntPtr lParam)
        {
            // This method must be overriden by each extending hook
            return 0;
        }

        private void Application_ProcessExit(object? sender, EventArgs e)
        {
            if (IsStarted) Stop();
        }
        #endregion
    }
}
