using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HjvTak.LiveAndroid.Core;
using Office = Microsoft.Office.Core;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace HjvTak.LiveAndroid
{
    /// <summary>A slide-anchored external window, never native PowerPoint media. All COM calls run on the Office STA.</summary>
    public sealed class LiveAndroidController : IDisposable
    {
        public const string Version = "0.0.004";
        public const string AnchorName = "HJVTAK_ANDROID_LIVE";
        private readonly PowerPoint.Application app;
        private readonly ScrcpySession session = new ScrcpySession();
        private readonly Timer timer = new Timer { Interval = 100 };

        private readonly int uiThread = System.Threading.Thread.CurrentThread.ManagedThreadId;
        private bool enabled = true, disposed;
        private DateTime resumeAfter;
        private bool transitionPending;
        private string alignment = "Start Android, then run a slideshow containing an overlay marker.";
        public event EventHandler StatusChanged;
        public string Status => session.Status.Detail + "\n" + alignment;
        public string Diagnostics => session.Diagnostics;
        public bool IsActive => session.Status.State == DeviceState.Starting || session.Status.State == DeviceState.Running;
        public bool OverlayEnabled => enabled;

        public LiveAndroidController(PowerPoint.Application application)
        {
            app = application ?? throw new ArgumentNullException(nameof(application));
            app.SlideShowBegin += OnShowChanged;
            app.SlideShowNextSlide += OnShowChanged;
            app.SlideShowEnd += OnShowEnd;
            app.PresentationClose += OnPresentationClose;
            timer.Tick += OnTick;
            timer.Start();
        }
        public void StartDevice(string executable, string serial)
        {
            EnsureThread();
            session.Start(executable, serial);
            enabled = true;
            Changed();
        }
        public void StopDevice() { EnsureThread(); session.Stop(); Changed(); }
        public void ToggleOverlay() { EnsureThread(); enabled = !enabled; if (!enabled) session.Hide(); Changed(); }
        public void AddAnchorToCurrentSlide()
        {
            EnsureThread();
            PowerPoint.DocumentWindow document = null;
            PowerPoint.View view = null;
            PowerPoint.Slide slide = null;
            PowerPoint.Presentation presentation = null;
            PowerPoint.PageSetup page = null;
            PowerPoint.Shapes shapes = null;
            PowerPoint.Shape shape = null;
            PowerPoint.TextFrame text = null;
            PowerPoint.TextRange range = null;
            try
            {
                document = app.ActiveWindow;
                if (document == null || (document.ViewType != PowerPoint.PpViewType.ppViewNormal &&
                    document.ViewType != PowerPoint.PpViewType.ppViewSlide))
                    throw new InvalidOperationException("Open a slide in Normal editing view first.");
                view = document.View;
                slide = view.Slide as PowerPoint.Slide;
                if (slide == null) throw new InvalidOperationException("Select a slide first.");
                shapes = slide.Shapes;
                shape = FindAnchor(shapes);
                if (shape != null) { shape.Select(); return; }
                presentation = document.Presentation;
                page = presentation.PageSetup;
                shape = shapes.AddShape(Office.MsoAutoShapeType.msoShapeRectangle,
                    page.SlideWidth * .62f, page.SlideHeight * .12f, page.SlideWidth * .29f, page.SlideHeight * .72f);
                shape.Name = AnchorName;
                shape.AlternativeText = "Android live external window overlay. Not embedded video; not included in exports.";
                text = shape.TextFrame;
                range = text.TextRange;
                range.Text = "Android live overlay\n0.0.004\nNot embedded video";
                shape.Select();
            }
            finally { Release(range); Release(text); Release(shape); Release(shapes); Release(page); Release(presentation); Release(slide); Release(view); Release(document); }
        }
        private void OnShowChanged(PowerPoint.SlideShowWindow show)
        {
            // NextSlide fires before the transition. Do not flash the old slide's video over the transition.
            session.Hide();
            resumeAfter = DateTime.UtcNow.AddMilliseconds(250);
            transitionPending = true;
        }
        private void OnShowEnd(PowerPoint.Presentation presentation) { session.Hide(); NativeWindow.DetachOwner(session.Window); alignment = "Slideshow ended; overlay hidden."; }
        private void OnPresentationClose(PowerPoint.Presentation presentation) { session.Hide(); NativeWindow.DetachOwner(session.Window); }
        private void OnTick(object sender, EventArgs args)
        {
            if (disposed) return;
            string previous = Status;
            try { session.Poll(); Align(); }
            catch (COMException) { session.Hide(); alignment = "PowerPoint is busy; overlay hidden until it is ready."; }
            catch (InvalidComObjectException) { session.Hide(); alignment = "Slideshow window closed."; }
            catch (Exception ex) { session.Hide(); alignment = "Overlay hidden: " + ex.Message; }
            if (Status != previous) Changed();
        }
        private void Align()
        {
            if (session.Status.State != DeviceState.Running || !enabled || DateTime.UtcNow < resumeAfter)
            { session.Hide(); return; }
            PowerPoint.SlideShowWindows shows = null;
            PowerPoint.SlideShowWindow show = null;
            PowerPoint.SlideShowView view = null;
            PowerPoint.Slide slide = null;
            PowerPoint.Shapes shapes = null;
            PowerPoint.Shape anchor = null;
            PowerPoint.Presentation presentation = null;
            PowerPoint.PageSetup page = null;
            try
            {
                shows = app.SlideShowWindows;
                if (shows.Count != 1)
                {
                    session.Hide();
                    alignment = shows.Count == 0 ? "No slideshow; overlay hidden." : "Multiple slideshows; overlay hidden. Run one show at a time.";
                    return;
                }
                show = shows[1];
                var hwnd = new IntPtr(show.HWND);
                view = show.View;
                bool normal = view.State == PowerPoint.PpSlideShowState.ppSlideShowRunning;
                bool visible = NativeWindow.IsWindowVisible(hwnd) && !NativeWindow.IsIconic(hwnd);
                bool foreground = NativeWindow.IsHostForeground(hwnd);
                if (!normal || !visible || !foreground)
                { session.Hide(); alignment = !normal ? "Overlay hidden: slideshow is blanked or paused." : !visible ? "Overlay hidden: slideshow is minimized or not visible." : "Overlay hidden: PowerPoint is in the background."; return; }
                slide = view.Slide;
                if (transitionPending)
                {
                    PowerPoint.SlideShowTransition transition = null;
                    try
                    {
                        transition = slide.SlideShowTransition;
                        double duration = transition.EntryEffect == PowerPoint.PpEntryEffect.ppEffectNone ? 0 : transition.Duration;
                        resumeAfter = DateTime.UtcNow.AddSeconds(Math.Max(0, Math.Min(60, duration)));
                        transitionPending = false;
                        session.Hide();
                        return;
                    }
                    finally { Release(transition); }
                }
                shapes = slide.Shapes;
                anchor = FindAnchor(shapes);
                bool valid = anchor != null && anchor.Visible == Office.MsoTriState.msoTrue &&
                    Math.Abs(anchor.Rotation) < 0.01 && anchor.Type == Office.MsoShapeType.msoAutoShape &&
                    anchor.AutoShapeType == Office.MsoAutoShapeType.msoShapeRectangle;
                if (!OverlayPolicy.ShouldShow(true, enabled, true, normal, visible, foreground, valid))
                { session.Hide(); alignment = "No visible, unrotated rectangular marker on this slide."; return; }
                presentation = show.Presentation; // Never ActivePresentation: Presenter View can activate a different deck.
                page = presentation.PageSetup;
                Rectangle client, bounds;
                using (new NativeWindow.DpiScope())
                {
                    if (!NativeWindow.TryGetClient(hwnd, out client) ||
                        !OverlayGeometry.TryMap(client, page.SlideWidth, page.SlideHeight,
                            anchor.Left, anchor.Top, anchor.Width, anchor.Height, out bounds))
                    { session.Hide(); alignment = "Keep the marker fully inside the slide."; return; }
                    if (!NativeWindow.Place(session.Window, hwnd, bounds))
                    { session.Hide(); alignment = "Windows could not position the overlay."; return; }
                }
                alignment = "Overlay visible (external window; not embedded video).";
            }
            finally { Release(page); Release(presentation); Release(anchor); Release(shapes); Release(slide); Release(view); Release(show); Release(shows); }
        }
        private static PowerPoint.Shape FindAnchor(PowerPoint.Shapes shapes)
        {
            for (int i = 1; i <= shapes.Count; i++)
            {
                var shape = shapes[i];
                if (shape.Name == AnchorName) return shape;
                Release(shape);
            }
            return null;
        }
        private static void Release(object value)
        { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
        private void Changed() { StatusChanged?.Invoke(this, EventArgs.Empty); }
        private void EnsureThread()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LiveAndroidController));
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != uiThread)
                throw new InvalidOperationException("Call the controller on PowerPoint's UI thread.");
        }
        public void Dispose()
        {
            if (disposed) return;
            EnsureThread();
            disposed = true;
            timer.Stop(); timer.Tick -= OnTick; timer.Dispose();
            try
            {
                app.SlideShowBegin -= OnShowChanged;
                app.SlideShowNextSlide -= OnShowChanged;
                app.SlideShowEnd -= OnShowEnd;
                app.PresentationClose -= OnPresentationClose;
            }
            finally { session.Dispose(); }
        }
    }
}
