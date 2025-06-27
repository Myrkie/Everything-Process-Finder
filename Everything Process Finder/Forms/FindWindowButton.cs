using System.Runtime.InteropServices;
using Everything_Process_Finder.Configuration;
using Everything_Process_Finder.Misc.MouseHook;
using Serilog;

namespace Everything_Process_Finder.Forms
{
    public sealed partial class FindWindowButton : Button
    {
        private static readonly ILogger Logger = Log.ForContext<FindWindowButton>();

        private bool _dragging;
        private readonly Cursor _finderCursor = Cursors.Cross;
        private readonly MouseHook? _mouseHook;
        private static WindowHighlighter? _highlighter;

        public event Action<IntPtr, string>? WindowFound;
        public FindWindowButton()
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;

            if (Config.Instance.Highlighter.DrawHighlighter)
            {
                _highlighter = new WindowHighlighter();
            }
            
            _mouseHook = new MouseHook();
            MouseDown += StartDrag;
            _mouseHook.MouseMove += Drag;
            _mouseHook.MouseUp += StopDrag;
            _mouseHook.Start();
            
            Logger.Information("Created FindWindowButton.");
        }
        
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            pe.Graphics.Clear(BackColor);

            int centerX = Width / 2;
            int centerY = Height / 2;

            int margin = Math.Min(Width, Height) / 6; // dynamic margin

            using Pen pen = new Pen(ForeColor, 1);
            
            // Vertical line
            pe.Graphics.DrawLine(pen, centerX, margin, centerX, Height - margin);

            // Horizontal line
            pe.Graphics.DrawLine(pen, margin, centerY, Width - margin, centerY);

            // Draw question mark in top-right corner
            string questionMark = "?";
            using Font font = new Font(Font.FontFamily, 6, FontStyle.Regular);
            SizeF textSize = pe.Graphics.MeasureString(questionMark, font);
            using Brush brush = new SolidBrush(ForeColor);
            pe.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            pe.Graphics.DrawString(questionMark, font, brush, Width - textSize.Width - 2, 0);
        }

        private void StartDrag(object? sender, MouseEventArgs e)
        {
            _dragging = true;
            Cursor.Current = _finderCursor;
        }

        private void StopDrag(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            Cursor.Current = Cursors.Default;
            IntPtr hWnd = GetWindowHandleUnderCursor();
            _highlighter?.Clear();
            
            if (hWnd == IntPtr.Zero | hWnd == Handle) return;
            string title = GetWindowText(hWnd);
            WindowFound?.Invoke(hWnd, title);
        }

        private void Drag(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            if (_highlighter == null) return;
            
            IntPtr hWnd = GetWindowHandleUnderCursor();
            if (hWnd != IntPtr.Zero && hWnd != Handle && hWnd != _highlighter.Handle)
                _highlighter.HighlightWindow(hWnd);
            else
                _highlighter.Clear();
        }

        // --- Win32 API Imports and Constants ---

        [LibraryImport("user32.dll", EntryPoint = "WindowFromPoint")]
        private static partial IntPtr WindowFromPoint(Point pt);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial int GetWindowTextW(IntPtr hWnd, Span<char> lpString, int nMaxCount);

        private static string GetWindowText(IntPtr hWnd)
        {
            Span<char> buffer = stackalloc char[256];
            var charsCopied = GetWindowTextW(hWnd, buffer, buffer.Length);

            return new string(buffer.Slice(0, charsCopied));
        }

        private static IntPtr GetWindowHandleUnderCursor()
        {
            return WindowFromPoint(Cursor.Position);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
            public static implicit operator Point(System.Drawing.Point p) => new() { X = p.X, Y = p.Y };
            public static implicit operator System.Drawing.Point(Point p) => new(p.X, p.Y);
        }
    }
}