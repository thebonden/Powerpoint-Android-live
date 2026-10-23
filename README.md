# HJVTAK PowerPoint Android Live

**0.0.004 — development build, not accepted for production use.**

A standalone Windows PowerPoint VSTO add-in for teaching from slides while showing a live Android tablet screen. Pair over **Wi-Fi with a QR code directly from PowerPoint**. No USB connection, OBS Studio, OBS plugin, camera bridge or Android Studio installation is required.

The Wi-Fi helper reuses the pairing code and ideas from the user's working OBS project, but runs independently. PowerPoint supplies the presentation framework; the existing OBS solution remains a separate choice for free-form sessions.

**The live image is still an external scrcpy window overlay, not native embedded slide video.** Only its marker is saved in the PPTX. PowerPoint PDF/video exports and slide-only sharing do not include the live feed. Whole-display capture must be tested separately.

## Build

Requires Windows x64, PowerShell 7, Visual Studio 2022/2026 with Office/SharePoint (Microsoft 365) development, Desktop development with C++, Windows SDK and .NET Framework 4.8 targeting pack. Desktop PowerPoint and the VSTO runtime are required to run the add-in. The native helper is x64; the managed add-in is AnyCPU (32-bit Office runtime testing remains pending).

```powershell
pwsh ./scripts/build.ps1 -Configuration Release
# Close PowerPoint, then complete the Microsoft installation dialog:
pwsh ./scripts/install-dev.ps1 -Configuration Release
```

This builds the QR helper from source, checks its vendored QR-generator hashes, runs the native/managed tests and generates signed VSTO development manifests including the helper. No external software is downloaded by the build. [Development setup](docs/DEVELOPMENT.md) covers signing, registration and removal.

## Connect your tablet wirelessly

1. Click **Forbind via Wi-Fi / QR**. PowerPoint automatically finds an existing complete scrcpy installation. If none is found, **Installér scrcpy…** opens the [official Windows installation instructions](https://github.com/Genymobile/scrcpy/blob/master/doc/windows.md); install or extract the complete package and click **Søg igen**. OBS is not required.
2. Connect PC and tablet to the same local Wi-Fi. On Android 11+, enable Developer options → **Wireless debugging**. The network must permit communication between the devices and mDNS discovery. [Android's wireless-debugging instructions](https://developer.android.com/tools/adb#connect-to-a-device-over-wi-fi).
3. The discovered location is remembered automatically. **Settings → Find automatisk** can search again; selecting a file manually remains available for custom locations.
4. Click **Forbind via Wi-Fi / QR**. On the tablet choose **Pair device with QR code** inside Wireless debugging, then scan the displayed code using that scanner (not the ordinary camera app).
5. Pairing locates the exact tablet and its connection port. PowerPoint verifies that it is online, saves that connection address and starts scrcpy automatically. No USB setup is needed.
6. Use **Add overlay marker** on the desired slide, move/resize the rectangle, then start the slideshow. Use **Show overlay** to hide it or **Stop Android** to stop the feed.
7. On later use, **Start / reconnect** checks the saved Wi-Fi address before starting. If the tablet changes its IP/port, pair again or enter its current connection address in the dialog.

The manual address is the IP:port on the **main Wireless debugging screen**, not the temporary pairing port. It is for a tablet already paired with this PC. This build accepts IPv4 addresses, not hostnames or IPv6. It deliberately does not guess devices or ports.

Audio and computer-to-tablet input are disabled. Use the tablet directly; advance PowerPoint using the keyboard/presenter controls, since clicks on the overlay are not forwarded to the slide.

## Lifecycle and limits

- QR sessions use random, transient credentials, a three-minute native expiry and a four-minute outer command limit. Cancel closes the owned helper; no credential is saved by the add-in. Only a verified connection address is persisted. Android/ADB retain their normal device-pairing authorization.
- ADB commands and scrcpy use the local PC's ADB server. The selected official scrcpy distribution supplies ADB. No shared ADB server restart/termination, port scanning, firewall changes or OBS integration is performed.
- Slideshow events and a 100 ms Office-thread timer align and hide the feed. Geometry uses the slideshow's own presentation/window, letterboxing, negative monitor coordinates and a restored thread DPI context.
- Hides on unmarked, blanked, paused, minimized/backgrounded or ended shows. Multiple simultaneous slideshows hide it rather than choose arbitrarily.
- Supports one visible, unrotated, ungrouped rectangle wholly inside the slide. Marker animation, shape cropping/masks, slide-artwork z-order and click-through are unsupported. Modal Office dialogs can delay timer reactions.
- Physical tablet pairing, actual Wi-Fi playback, projector/Presenter View, mixed-DPI, rotation and capture remain manual acceptance checks for this PowerPoint build; automated tests use simulated devices.

[Validation and acceptance matrix](docs/TESTING.md) · [Wi-Fi details](docs/WIFI.md) · [Native embedding research](docs/NATIVE-EMBEDDING.md) · [Change log](CHANGELOG.md)

The QR helper's reused GPL-2.0-or-later code and MIT QR generator retain their licenses and source under `native/WifiQr`. Keep corresponding helper source with any redistribution. CI provides both binary and helper-source artifacts.

User version **0.0.004** maps to assembly/ClickOnce **0.0.4.0**. Branch remains `feature/live-android-0.0.001`. **0.1.001 requires completed testing and explicit acceptance.**
