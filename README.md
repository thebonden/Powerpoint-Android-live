# PowerPoint Android Live

**0.0.108 — development build, not accepted for production use.**

A standalone Windows PowerPoint VSTO add-in for teaching from slides while showing a live Android tablet screen. Pair over **Wi-Fi with a QR code directly from PowerPoint**. No USB connection, OBS Studio, OBS plugin, camera bridge or Android Studio installation is required.

The Wi-Fi helper reuses the pairing code and ideas from the user's working OBS project, but runs independently. PowerPoint supplies the presentation framework; the existing OBS solution remains a separate choice for free-form sessions.

**The live image is still an external scrcpy window overlay, not native embedded slide video.** Only its marker is saved in the PPTX. PowerPoint PDF/video exports and slide-only sharing do not include the live feed. Whole-display capture must be tested separately.

## Install without development tools

Distribute **Android-Live-0.0.108-Setup.exe**, one file per recipient. Close PowerPoint, double-click the file, and click **Installer / opdater**. The launcher validates/extracts its payload to a fixed per-user appdata directory, guides missing Microsoft prerequisites, migrates recognized old Office registrations, and confirms success through VSTO's exit status and the resulting registration. It attempts to restore the previous version on failure. Settings and presentations are outside the program directory.

[Installation guide (Danish)](docs/INSTALLATION.md) explains prerequisites, updates, recovery and first tablet connection. The same EXE supports silent central deployment in user context; the IT bundle includes install/uninstall commands, detection and guidance. This is not a machine-wide MSI. The outer EXE is not publicly signed; Microsoft trust prompts still apply. scrcpy is discovered/guided separately.

Download the EXE from the **Android-Live-0.0.108-installation** artifact of a successful GitHub Actions run. GitHub wraps artifacts in a download ZIP; extract just the EXE and share that file. Run **scripts/package.ps1** using PowerShell 7 to build/test both the single-file installer and diagnostic ZIP. Corresponding QR source/licenses are embedded.
## Central IT deployment and neutral identity

**Android-Live-0.0.108-IT.zip** contains the same EXE, Windows PowerShell wrappers, version-aware detection and [IT deployment instructions](docs/IT-DEPLOYMENT.md). Use user-context Intune/management assignments with --install --silent, --uninstall --silent and --detect. Service accounts are refused. Missing dependencies or publisher trust fail without a dialog and before removing the old add-in; PowerPoint running returns a retry code.

Product identity, program files, namespaces, new settings paths and development-certificate name are now **Android Live**. A small read-only compatibility class retains released identifiers for settings, registrations and slide frames. User-supplied tablet names are preserved.

## Remove a saved tablet

Select the tablet and click **Fjern fra listen**, then confirm. Its live stream stops and it disappears from the saved list and status; other devices stay connected. Slides and frames remain, but frames for the removed device no longer show video. Android pairing is not revoked. Adding the device again creates a new profile: replace its old frames using **Indsæt / opdater ramme**. Use **Opdater valgt / QR** instead when keeping existing slide assignments.

## Build

Requires Windows x64, PowerShell 7, Visual Studio 2022/2026 with Office/SharePoint (Microsoft 365) development, Desktop development with C++, Windows SDK and .NET Framework 4.8 targeting pack. Desktop PowerPoint and the VSTO runtime are required to run the add-in. The native helper is x64; the managed add-in is AnyCPU (32-bit Office runtime testing remains pending).

```powershell
pwsh ./scripts/build.ps1 -Configuration Release
# Close PowerPoint, then complete the Microsoft installation dialog:
pwsh ./scripts/install-dev.ps1 -Configuration Release
```

This builds the QR helper from source, checks its vendored QR-generator hashes, runs the native/managed tests and generates signed VSTO development manifests including the helper. No external software is downloaded by the build. [Development setup](docs/DEVELOPMENT.md) covers signing, registration and removal.

## Connect and choose your tablets

1. Click **Tilføj tablet / QR**. PowerPoint automatically finds a complete scrcpy installation. If none is found, **Installér scrcpy…** opens the [official Windows guide](https://github.com/Genymobile/scrcpy/blob/master/doc/windows.md). Install/extract the complete package and click **Søg igen**. Requires scrcpy 4.1 or newer for the explicit full-screen fit options.
2. Put PC and tablet on the same local Wi-Fi. On Android 11+, open Developer options → Wireless debugging → **Pair device with QR code**. Use that scanner, not the ordinary camera app.
3. After pairing, a **Forhåndsvisning** window opens automatically. A connection is visible immediately, even before a slideshow. Close/toggle this preview without stopping the connection.
4. Repeat **Tilføj tablet / QR** for additional tablets. Choose a tablet in the **Tablet** list; type a useful name in **Dit navn** beside the list and press Enter. You can rename saved devices while offline, including when reusing existing slides: the stable device/slide identity stays unchanged. **Ændr navn…** also opens the name dialog or restores automatic Android naming. The list shows Android name and custom name together, e.g. **Tablet001 — Teaching**; identical names appear once.
5. On a slide, select the desired tablet and click **Indsæt / opdater ramme**. Resize and position the frame. Use different tablets on different slides, or insert several devices' frames on the same slide for simultaneous images. One frame per tablet per slide is supported. Start each required connection before the show; **Klargør / forbind alle** reconnects the saved devices.
6. Start the slideshow. Each frame shows its assigned device; slide navigation changes the visible feeds. Connections remain warm between slides. **Stop valgt** stops only the selected device; **Stop alle** stops them all.
7. After a tablet changes its connection port, choose that saved tablet and use **Opdater valgt / QR** to update it without breaking existing slide assignments. **Forbind valgt** also accepts its current IPv4 connection address.

## Reuse a presentation on another day

Keep the PPTX and the same Windows user profile. Turn on Wireless debugging, join the same Wi-Fi, then click **Klargør / forbind alle**. The saved endpoint is tried first; if it fails, known tablets are found through ADB mDNS advertisements and matched by hardware serial, never just the display name. An old profile without a serial must first reconnect at its saved address or be updated explicitly. Search is bounded and does not scan the network.

If a tablet cannot be found, select it and use **Opdater valgt / QR** or enter the current connection address. This updates the existing profile, retaining its stable slide key and custom name. Never recreate the slide frames just because the port changed. The device list belongs to the PC user profile and is not transferred with the PPTX to another PC.

New tablets use Android's own device name (model fallback). Legacy generated names such as Tablet 1 are refreshed on the next connection. **Dit navn** saves a custom PowerPoint name directly. In **Ændr navn…**, typing also selects custom naming automatically; select **Brug enhedens Android-navn** to follow Android's name again. This choice and the last reported Android name are saved separately. Reconnection never replaces an explicitly custom name. Old non-placeholder names are conservatively preserved because earlier versions did not record their source. New frames read **Livebillede fra Tablet001 — Teaching / under slideshow** without a version number. To refresh an existing frame after renaming, select its tablet and use **Indsæt / opdater ramme** on that slide. The existing shape and stable routing ID are retained; text is updated only by this explicit action.

## Show the whole Android screen

Preview and slideshow frames always use proportional letterbox scaling: the whole captured Android screen fits inside the chosen rectangle, with black padding when aspect ratios differ. Window aspect locking is disabled so scrcpy accepts tall, wide and freely resized frames; the image itself retains its proportions. No crop or stretch option is used. Rotation is handled by scrcpy. Android app/system restrictions still determine what Android supplies.

## Sound, finger marks and orientation

These choices belong to the selected tablet and are remembered:

- **Lyd** enables the tablet's output audio on the PC. Off by default. Sound continues while that connection runs, including when its picture is hidden; turn it off or stop that device to silence it. This does not capture the microphone. Protected apps/device restrictions may prevent audio.
- **Vis fingertryk** shows physical finger touches. Off by default. scrcpy restores the tablet's previous touch-indicator setting on exit. Keyboard, mouse, gamepad input and automatic clipboard synchronization are disabled.
- **Orientering: Automatisk / Liggende / Stående** follows rotation or locks the transmitted image to a landscape/portrait orientation. Natural device dimensions are read first, so landscape-native tablets are handled correctly. A capture lock does not force Android apps to relayout.
- Changing these choices briefly restarts only that tablet's stream, without a new QR pairing.

The editing preview shows the selected tablet. During a slideshow, it hides and the device windows follow their slide frames. All are external windows; PowerPoint exports still do not contain the feed. Use the tablet directly; advance slides with the keyboard/presenter, since overlay clicks do not advance the show.

## Lifecycle and limits

- QR sessions use random, transient credentials, a three-minute native expiry and a four-minute outer command limit. Cancel closes the owned helper; no credential is saved by the add-in. Verified connection addresses, device names and hardware serials are stored locally in your user settings. Android/ADB retain their normal device-pairing authorization.
- ADB commands and scrcpy use the local PC's ADB server. The selected official scrcpy distribution supplies ADB. No shared ADB server restart/termination, port scanning, firewall changes or OBS integration is performed.
- Slideshow events and a 100 ms Office-thread timer align and hide the feed. Geometry uses the slideshow's own presentation/window, letterboxing, negative monitor coordinates and a restored thread DPI context.
- Hides on unmarked, blanked, paused, minimized/backgrounded or ended shows. Multiple simultaneous slideshows hide it rather than choose arbitrarily.
- Supports multiple devices with one visible, unrotated, ungrouped rectangle per device, wholly inside the slide. Duplicate frames for the same device are hidden. Keep frames separate; overlapping-window order is not slide-artwork order. Marker animation, shape cropping/masks, slide-artwork z-order and click-through are unsupported. Modal Office dialogs can delay timer reactions.
- Physical QR pairing/video startup was confirmed in 0.0.005. The user confirmed tablet connection and visible display in 0.0.006. Multiple real tablets, audio/touches, orientation locks, projector/Presenter View, mixed-DPI and capture require installed-build/manual acceptance; automated process tests use simulated devices.

[Validation and acceptance matrix](docs/TESTING.md) · [Wi-Fi details](docs/WIFI.md) · [Native embedding research](docs/NATIVE-EMBEDDING.md) · [Change log](CHANGELOG.md)

The QR helper's reused GPL-2.0-or-later code and MIT QR generator retain their licenses and source under `native/WifiQr`. Keep corresponding helper source with any redistribution. CI provides both binary and helper-source artifacts.

User version **0.0.108** maps to assembly/ClickOnce **0.0.108.0**. Branch remains `feature/live-android-0.0.001`. **0.1.001 requires completed testing and explicit acceptance.**
