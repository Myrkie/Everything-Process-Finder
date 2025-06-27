using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Everything_Process_Finder.Configuration;
using Serilog;

// ReSharper disable IdentifierTypo
namespace Everything_Process_Finder.Forms
{ 
    public sealed partial class WindowHighlighter : Form
    {
        private static readonly ILogger Logger = Log.ForContext<WindowHighlighter>();

        public WindowHighlighter()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Config.Instance.Highlighter.HighlightColor;
            Opacity = 0.5;
            Enabled = false;
            var exStyle = WmWsExTransparent | WmWsExLayered;
            SetWindowLongPtrW(Handle, WmGwlExstyle, exStyle);

            var cornerPreference = DwmWindowCornerPreference.DwmwcpRound;
            DwmSetWindowAttribute(Handle, Dwmwindowattribute.DwmwaWindowCornerPreference, ref cornerPreference, sizeof(uint));
            Logger.Information("Created Window Highlighter.");
        }

        public void HighlightWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            int result = DwmGetWindowAttribute(hWnd, Dwmwindowattribute.DwmwaExtendedFrameBounds, out var rect, Unsafe.SizeOf<Rect>());

            if (result != 0 || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            {
                if (!GetWindowRect(hWnd, out rect)) return;
            }

            SetBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            Show();
        }

        public void Clear()
        {
            Hide();
        }

        // --- Win32 API Imports and Constants ---
        
        [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        private static partial int DwmGetWindowAttribute(
            IntPtr hwnd,
            Dwmwindowattribute dwAttribute,
            out Rect pvAttribute,
            int cbAttribute);
        
        [LibraryImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute")]
        private static partial void DwmSetWindowAttribute(IntPtr hwnd,
            Dwmwindowattribute attribute,
            ref DwmWindowCornerPreference pvAttribute,
            uint cbAttribute);
        
        
        // ReSharper disable UnusedMember.Local
        private enum Dwmwindowattribute
        {
            DwmwaWindowCornerPreference = 33,
            DwmwaExtendedFrameBounds = 9
        }

        private enum DwmWindowCornerPreference
        {
            DwmwcpDefault      = 0,
            DwmwcpDonotround   = 1,
            DwmwcpRound        = 2,
            DwmwcpRoundsmall   = 3
        }

        [LibraryImport("user32.dll", EntryPoint = "GetWindowRect")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static partial void SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int WmGwlExstyle = -20;
        private const int WmWsExTransparent = 0x00000020;
        private const int WmWsExLayered = 0x00080000;

        private struct Rect
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public int Left, Top, Right, Bottom;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
        }
    }
}