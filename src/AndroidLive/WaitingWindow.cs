using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AndroidLive
{
    // A temporary cover; never rewrite text, shapes, animations or saved state in the deck.
    internal sealed class WaitingWindow : Form
    {
        internal static readonly Color SurfaceColor = Color.FromArgb(24, 35, 45);
        private bool indicator;
        private string label = "";
        internal bool IndicatorVisible => indicator;
        internal string WaitingText => "Venter på forbindelse til\n" + label + "…";
        internal WaitingWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(-32000, -32000, 320, 240);
            BackColor = SurfaceColor;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams
        {
            get { var value = base.CreateParams; value.ExStyle |= 0x08000000 | 0x80 | 0x20; return value; }
        }
        protected override void WndProc(ref Message message)
        {
            // Same UI thread as PowerPoint: let mouse navigation reach the slide below.
            if (message.Msg == 0x0084) { message.Result = new IntPtr(-1); return; }
            if (message.Msg == 0x0021) { message.Result = new IntPtr(3); return; }
            base.WndProc(ref message);
        }
        internal bool Place(IntPtr host, Rectangle bounds, bool showIndicator, string deviceLabel)
        {
            SetContent(showIndicator, deviceLabel);
            // SetWindowPos(SHOWWINDOW) alone does not set WinForms' managed Visible
            // state or run Load/Shown. Such an HWND reports visible but does not paint.
            // Use the Form lifecycle first, without activation, then enforce owner/z-order.
            Bounds = bounds;
            if (!Visible) {
                if (host == IntPtr.Zero) Show();
                else Show(new OwnerWindow(host));
            }
            bool placed = NativeWindow.Place(Handle, host, bounds);
            if (placed) Update();
            else Conceal();
            return placed;
        }
        internal void SetContent(bool showIndicator, string deviceLabel)
        {
            string next = deviceLabel ?? "denne enhed";
            if (indicator == showIndicator && label == next) return;
            indicator = showIndicator; label = next; Invalidate();
        }
        internal void Conceal()
        {
            if (!IsHandleCreated) return;
            Hide(); NativeWindow.DetachOwner(Handle);
        }
        private sealed class OwnerWindow : IWin32Window
        {
            public IntPtr Handle { get; }
            internal OwnerWindow(IntPtr handle) { Handle = handle; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!indicator || ClientSize.Width < 40 || ClientSize.Height < 40) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Max(.35f, Math.Min(2f, Math.Min(ClientSize.Width / 400f, ClientSize.Height / 250f)));
            float cx = ClientSize.Width / 2f, top = ClientSize.Height * .18f;
            BrandArtwork.Draw(g,new RectangleF(cx-32*scale,top-7*scale,64*scale,64*scale),Color.FromArgb(189,211,225));
            float textTop = top + 68 * scale;
            var rect = new RectangleF(12 * scale, textTop, ClientSize.Width - 24 * scale,
                Math.Max(1, ClientSize.Height - textTop - 10 * scale));
            using (var font = new Font("Segoe UI", 17 * scale, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(Color.FromArgb(228, 237, 243)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(WaitingText, font, brush, rect, format);
        }
    }
}