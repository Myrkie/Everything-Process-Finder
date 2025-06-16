using System.Runtime.InteropServices;

namespace Everything_Process_Finder
{
    public sealed class FindWindowButton : Button
    {
        private bool _dragging;
        private readonly Cursor _finderCursor;

        public event Action<IntPtr, string>? WindowFound;
        // ReSharper disable once UnusedParameter.Local
        public FindWindowButton(bool debug = false)
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;

            MouseDown += StartDrag;
            MouseUp += StopDrag;
            MouseMove += Drag;

            _finderCursor = Cursors.Cross;
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
            Point pos = Cursor.Position;
            IntPtr hWnd = WindowFromPoint(pos);
            
            if (hWnd == IntPtr.Zero) return;
            string title = GetWindowText(hWnd);
            WindowFound?.Invoke(hWnd, title);
        }

        private void Drag(object? sender, MouseEventArgs e)
        {
            if (_dragging)
            {
            }
        }

        // --- Win32 API Imports ---

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
    }
}
