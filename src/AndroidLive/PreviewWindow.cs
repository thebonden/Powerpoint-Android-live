using System;
using System.Drawing;
using System.Windows.Forms;

namespace AndroidLive
{
    internal sealed class PreviewWindow : Form
    {
        private readonly Panel video = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
        internal event EventHandler Dismissed;
        internal PreviewWindow()
        {
            Text = "Tablet · Forhåndsvisning";
            ClientSize = new Size(640, 430); MinimumSize = new Size(320, 240);
            StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = false;
            Controls.Add(video);
            Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 45, TextAlign = ContentAlignment.MiddleCenter,
                Text = "Live forhåndsvisning · Indsæt en tabletramme på sliden for at vise billedet under slideshow." });
            FormClosing += (s,e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); Dismissed?.Invoke(this,EventArgs.Empty); } };
        }
        internal Rectangle VideoBounds => video.RectangleToScreen(video.ClientRectangle);
        protected override bool ShowWithoutActivation => true;
    }
}