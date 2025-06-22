using System.Runtime.InteropServices;
using Everything_Process_Finder.Configuration;
using Everything_Process_Finder.Misc.MouseHook;
using Serilog;

namespace Everything_Process_Finder.Forms
{
    public sealed class FindWindowButton : Button
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

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        private static string GetWindowText(IntPtr hWnd)
        {
            const int nChars = 256;
            var buff = new System.Text.StringBuilder(nChars);
            GetWindowText(hWnd, buff, nChars);
            return buff.ToString();
        }
        private static IntPtr GetWindowHandleUnderCursor()
        {
            Point pos = Cursor.Position;
            return WindowFromPoint(pos);
        }
    }
}