# Windows development instructions — version 0.0.001

**Status: unbuilt and untested source prototype.**

1. On a Windows development PC, install Visual Studio with the **Office/SharePoint development** workload, .NET Framework 4.8 developer pack, and desktop PowerPoint.
2. In Visual Studio create a **PowerPoint VSTO Add-in** project (.NET Framework 4.8). Set its namespace to `HjvTak.LiveAndroid`.
3. Add `src/HjvTak.LiveAndroid/LiveAndroidController.cs` to that project, referencing **Microsoft PowerPoint Object Library** and **Microsoft Office Object Library**. Do not assume you can build the source directly from this repository yet; the VS-generated VSTO designer, manifests and signing settings are not checked in.
4. In generated `ThisAddIn_Startup`, construct `new LiveAndroidController(this.Application, @"C:\\path\\to\\scrcpy.exe")`. Start scrcpy using `StartDevice()` only after USB debugging has been authorized. In `ThisAddIn_Shutdown`, dispose it.
5. For the initial manual test, call `AddAnchorToCurrentSlide()`, enter presentation mode, and call `AlignToSlideShow()`. Hook `Application.SlideShowNextSlide` and `Application.SlideShowBegin` after the basic test. Call `HideVideo()` on slideshow termination.
6. Test the video window's exact slide placement, display scaling, screen recording and focus behavior. **It may disappear behind PowerPoint or occlude other applications.** It is an overlay, not embedded PowerPoint media.
7. Never enable USB debugging on an organizational device without authorization. Do not mirror protected or classified material to an unapproved computer.

## Exit criteria for 0.0.xxx
- A repeatable Windows build/install.
- Connect/disconnect USB, scrcpy crash/reconnect, basic start/stop.
- Continuous real-time screen during slideshow and slide changes.
- Verify normal editing view, projector/presenter view, dual monitors, mixed-DPI and slideshow aspect ratios.
- Verify portrait/landscape rotation, window focus, audio, latency and system permissions.
- Confirm whether an actual **native slide-contained video object** is achievable; current implementation does NOT satisfy that stronger requirement.

No automatic promotions to version 0.1.001 without complete testing and approval.
