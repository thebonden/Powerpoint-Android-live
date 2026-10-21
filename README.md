# HJVTAK PowerPoint Android Live

**Version: 0.0.001 — development prototype (NOT tested or approved)**

Goal: Display an Android device's live screen within the apparent bounds of a PowerPoint slide during a Windows slideshow, with a resizeable/moveable placement marker on the slide.

## Important technical distinction

PowerPoint's object model does **not** expose an API for injecting arbitrary decoded video frames as a native, continuously rendered slide object. This prototype therefore uses a **slide-anchored native window overlay** displaying scrcpy's real-time Android video. It *looks* as if the live feed occupies a designated area of the slide, but it is NOT an embedded PowerPoint media object, and won't be embedded in the PPTX or exported to PDF/video. Compatibility with overlays, screen recording, presenter view, and protected devices must be verified.

## Initial prototype design

* Windows desktop PowerPoint add-in written in C# targeting .NET Framework 4.8, using PowerPoint COM interop.
* An identified PowerPoint rectangle represents the live-video position and dimensions.
* External `scrcpy` mirrors the Android device over ADB/USB (or an explicitly configured ADB connection).
* A Windows overlay window is positioned over the selected rectangle while the slideshow is running.
* Updates will handle slide changes and show/hide, plus device rotation and DPI/multiple monitor scaling.

## Milestones

1. `0.0.001`: source scaffold, overlay anchor prototype, device launch commands; **Windows build and actual slideshow are unverified**.
2. `0.0.xxx`: test and harden: slide/window coordinates, DPI, changing slides, reconnection, multiple displays, rotation and sound.
3. Only after **all requirements are tested and explicitly accepted**, release `0.1.001`.

## Prerequisites for a Windows test

- Desktop Microsoft PowerPoint for Windows, Visual Studio with .NET Framework 4.8 developer pack and Office interop references.
- Android device with USB debugging enabled and authorized.
- Official scrcpy and Android platform-tools available locally. The add-in must not silently install them.
- Permission to run this software on the target Windows computer.

Do not use in a production presentation until validated. **No working installer or signed release exists at version 0.0.001.**
