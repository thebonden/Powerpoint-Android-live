# HJVTAK PowerPoint Android Live

**0.0.002 — development build, not accepted for production use.**

A Windows PowerPoint VSTO add-in that positions a live Android screen over a rectangular marker in a slideshow. The repository now contains a complete .NET Framework 4.8 solution, VSTO bootstrap, ribbon, event wiring, build scripts, and automated Windows tests.

**This is an external scrcpy window overlay, not a video object embedded in a slide.** Only the marker is saved in the PPTX. PowerPoint PDF/video exports and slide-only sharing do not include the live feed. Whole-display capture must be tested with your recording/sharing software.

## Build and try

Requires Windows, Visual Studio 2022/2026 with Office/SharePoint (Microsoft 365) development tools and the .NET Framework 4.8 targeting pack, plus PowerShell 7. Desktop PowerPoint and the VSTO runtime are required to run the add-in. Office interop assemblies supplied by Visual Studio are used for compilation; PowerPoint does not need to run during a build.

```powershell
pwsh ./scripts/build.ps1
# Close PowerPoint before registering the local development build:
pwsh ./scripts/register-dev.ps1
```

The build creates the DLL, application manifest and `.vsto` deployment manifest, signs them with an ignored local development certificate, and runs tests. It does not register the add-in automatically. See [development setup](docs/DEVELOPMENT.md) for certificate behavior, Visual Studio, installation, removal, and troubleshooting.

1. Download and extract the [official scrcpy Windows distribution](https://github.com/Genymobile/scrcpy/blob/master/doc/windows.md). Keep its support files together. Use scrcpy 3.x or 4.x; hardware compatibility still needs validation.
2. Authorize Android USB debugging on your phone. If several ADB devices are connected, enter the desired serial in Settings.
3. Open a presentation and choose **Android Live → Settings** to select `scrcpy.exe`.
4. Choose **Add overlay marker**, then move/resize the rectangle in Normal view. Repeat on each slide that needs the feed.
5. Choose **Start Android**, then start one slideshow. Use **Show overlay** to hide/show it and **Stop Android** to disconnect.
6. **Status / diagnostics** shows connection state and recent scrcpy output. Reconnect explicitly with Start after an error.

Audio and computer-to-phone input are disabled in this build. The phone remains the input device. Mouse clicks on the overlay are not forwarded to PowerPoint; use the keyboard/presenter controls to advance slides.

## Implemented behavior and limits

- Nonblocking startup, 20-second startup timeout, bounded diagnostics, process/window ownership checks, restart and bounded process cleanup. Never kills unrelated scrcpy processes or the shared ADB server.
- SlideShowBegin, SlideShowNextSlide, SlideShowEnd and PresentationClose events, plus a 100 ms UI-thread timer for window movement, resize, foreground changes and blank/pause states.
- Coordinates come from the slideshow's own presentation and window. Letterboxing, portrait slides and negative monitor coordinates are handled; thread DPI context is restored after placement.
- Hides without a valid marker, on black/white/pause, when minimized/backgrounded, and while changing slides. Multiple simultaneous slideshows deliberately hide the overlay rather than choosing one arbitrarily.
- Supports a visible, unrotated, ungrouped rectangle wholly inside the slide. Marker animations, masks, shape cropping, z-order relative to slide artwork and click-through are not supported. A window overlay is always above slide artwork.
- Windows scheduling means visibility changes can lag a timer tick, or longer while Office is blocked by a modal dialog. Mixed-DPI, projector, Presenter View, actual Android rotation and capture remain manual acceptance items.

[Validation status and test matrix](docs/TESTING.md) · [Native embedding investigation](docs/NATIVE-EMBEDDING.md) · [Change log](CHANGELOG.md)

Version `0.0.2.0` is the numeric .NET/ClickOnce representation of development version **0.0.002**. The existing branch remains `feature/live-android-0.0.001`. **0.1.001 is reserved for completed testing and explicit acceptance.**
