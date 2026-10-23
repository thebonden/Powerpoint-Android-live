# Validation — development version 0.0.100

## Installer, names and reconnection — 2026-10-23

- User explicitly confirmed tablet connection and visible display in installed 0.0.006. This does not establish multiple-tablet/audio/orientation acceptance.
- Release build: 51 managed test groups passed plus native QR tests.
- New tests cover friendly-name/model fallback, invalid metadata, missing metadata, exact TLS advertisement filtering, changed endpoint/identity matching, wrong-device rejection, legacy profiles, cancellation and XML persistence of custom names/slide keys.
- Installation ZIP now contains Microsoft setup.exe, signed ClickOnce manifests, complete payload, Danish guide and corresponding helper source/licenses.
- The new installer and next-day physical reconnection still require installed/manual acceptance. Tests simulate changed endpoints; they do not prove a 24-hour hardware run.
- Check on the same PC: save PPTX with two profiles/frames, rename them, restart tablets/wireless debugging, then Klargør / forbind alle. Verify routing remains correct. For a failure, select that device and Opdater valgt / QR; confirm IDs/names/other streams remain unchanged.
- Check clean-machine install, existing-install upgrade, missing prerequisites, user cancellation and normal desktop PowerPoint restart. Do not bypass Microsoft trust prompts.

## Preview and multiple devices — 0.0.006 (2026-10-23)

- Full Release build and native QR tests passed. **40 managed test groups passed**, including all audio/touch/lock combinations, natural portrait/landscape and override dimensions, scoped/cancellable ADB size query, old settings migration, independent XML preferences and two real simulated child processes.
- The process test verifies stopping/restarting/failing one device leaves the other running with the same owned window.
- The user reported that QR pairing in 0.0.005 started a stream but no image appeared while editing. Actual diagnostics identified the deliberate no-slideshow hide rule; the new automatic preview addresses that case.
- Version 0.0.006 installed successfully through the desktop installer. A normal desktop launch reported the add-in connected. The new ribbon, migrated Tablet 1 profile, per-device controls and orientation selector were visually verified.
- GitHub Actions run 39949184256 passed for complete source commit 3685037: Windows Release build/tests and development binary/source artifact uploads.
- The saved tablet endpoint was offline; the dialog correctly rejected it. The installed native QR helper opened for a new pairing. Live preview, two physical tablets across/same slide, audible output, finger indicators and physical orientation remain pending until devices connect. Automated tests do not establish those hardware outcomes.

New manual checks:
1. Pair/reconnect and see the editing preview immediately. Resize/move/minimize/close it, change selected tablet, switch to another app, then show again.
2. Pair two distinct tablets. Use A on slide 1, B on slide 2, A+B on slide 3. Verify correct identity and bounds; duplicate one device's frame and verify that device hides.
3. Stop/restart/disconnect one tablet while the other continues. End and reopen a saved deck; profile IDs remain stable.
4. Toggle sound and finger marks for only the selected tablet. Verify a brief restart, no new QR requirement, PC output audio, visible finger marks and restoration on stop. Audio remains active while its image is hidden until muted/stopped.
5. Test automatic and locked portrait/landscape on both natural device orientations, with tablet rotation and an Android app that locks its own layout.

## Installed QR helper correction — 0.0.005 (2026-10-23)

- Full Release build, native QR tests and **34 managed test groups passed**. A real CLR shadow-copy test verifies helper resolution from the original deployment, including spaces and Unicode.
- The desktop installer updated successfully. A normal Explorer-brokered PowerPoint launch reported version 0.0.005 connected; the ribbon was visually verified.
- Automatic discovery selected the installed WinGet package in PowerPoint. The user scanned the QR code; actual scrcpy diagnostics confirmed a Samsung SM-X356B on Android 16 and a 1920x1200 video texture.
- The user reported no visible image after pairing. Diagnostics explicitly showed "No slideshow; overlay hidden." This is the current editing-view behavior, not a failed QR connection. A preview and optional audio/touch/orientation controls are the next development work.
- Visible slideshow rendering, physical touch/audio and the full hardware acceptance matrix remain pending. No device address or pairing credential is included in this evidence.

## Automatic discovery update — 0.0.004 (2026-10-22)

- Full Release build and **33 managed test groups passed**, including six new groups for saved-path precedence, incomplete-package fallback into a WinGet layout, each required support file, ignored unrelated/OBS/deep folders, installation followed by retry, and cancellation. Native QR tests also passed.
- The production locator, with no saved path, discovered the existing WinGet scrcpy 4.1 installation on the user's PC. This check did not execute scrcpy or contact a tablet.
- After the earlier desktop installation correction, the user confirmed that the ribbon worked. Independent repeated normal-launch acceptance and physical tablet QR/streaming tests remain outstanding. Automatic discovery and installation guidance still require in-PowerPoint acceptance after updating the installed build.

## Wi-Fi update — 0.0.003 (2026-10-22)

- Complete Debug and Release VSTO builds including the standalone x64 QR helper succeeded. Both configurations passed all 27 managed test groups.
- **27 managed test groups passed**, including the earlier 14 plus endpoint/result validation, successful helper + ADB handoff, cancellation, nonzero exit, malformed output, offline reconnect, bounded output and child-process timeout/cleanup. Devices are simulated; no live tablet was paired by these tests.
- Reused native QR tests pass: payload, mDNS parsing, exact session/device matching, stdin secret, port selection, cancellation, timeout and ambiguity rejection. Added cases cover offline state, unrelated GUID, using the pairing port as a connection port, and cancellation after pairing.
- The full PowerPoint-branded QR dialog was rendered with fixed dummy credentials and independently decoded with jsQR to the expected Android ADB payload. No live pairing secret or network command was used in that preview test.
- Actual tablet scanning/streaming in PowerPoint and the full multi-monitor slideshow acceptance matrix remain pending. Ribbon loading was subsequently verified as recorded below. The user's successful OBS pairing is the source of the reused code, not evidence that this new PowerPoint integration has passed hardware tests.


## Normal-launch regression and installation correction (2026-10-22)

- The user reproduced the missing ribbon after closing the successful test session and starting PowerPoint normally. The normal instance did not enumerate `HjvTak.LiveAndroid`; the Disabled Items list was empty. The earlier runtime check below therefore does **not** establish reliable installation.
- A probe in the development process saw the HKCU add-in key with LoadBehavior 3. A separate read-only probe launched by the actual Explorer desktop, under the same user, reported that the key did not exist. This explains why a development-host launch succeeded while a desktop launch failed. Package-identity detection alone did not identify this registry-view difference.
- Running the Microsoft installer through that desktop context required the normal user trust prompt for the locally signed development build; silent installation returned -300 and did not register the add-in. No trust/security setting was bypassed.
- Added `scripts/install-dev.ps1` to open the official installation dialog through Explorer's desktop Application. Its `-WhatIf` validation passed against the existing Release build. Updated the low-level registration message and setup docs to avoid treating a private registry write or installer launch as successful installation.
- The user subsequently approved the trust prompt and confirmed the ribbon. Desktop-brokered launches of 0.0.004 and 0.0.005 loaded the add-in; the later QR evidence is recorded above. Visible slideshow playback remains pending.

## PowerPoint runtime check (2026-10-22)

- After the local package was installed with Microsoft's VSTOInstaller and all PowerPoint processes exited, a fresh PowerPoint launch reported `HjvTak.LiveAndroid` with `COMAddIn.Connect = true`. VSTO loader and CLR modules were loaded.
- In a new blank presentation, the **Android Live** tab was visually verified. Opening it displayed Add overlay marker, Forbind via Wi-Fi / QR, Start / reconnect, Stop Android, Show overlay, Settings and Status / diagnostics. The Settings dialog also opened.
- Earlier sessions did not enumerate the add-in, and the Disabled Items list was empty. The successful later launch does not isolate the original cause; it does not prove that registry-only registration is sufficient on every machine.
- No source or binary change was required for this successful load. It validates ribbon presence and the settings dialog only, not QR pairing, the live feed, marker behavior or slideshow rendering. Those hardware/interactive checks remain pending.

## Recorded local evidence (2026-10-21)

- Full VSTO **Debug and Release builds succeeded** on Windows with Visual Studio 18/2026, .NET Framework 4.8. The DLL, signed application manifest and signed deployment `.vsto` were generated.
- **14 automated test groups passed**, including 128 visibility-input combinations, Windows command-line argument round trips, geometry and process/window integration using a simulated scrcpy executable.
- Actual installed PowerPoint COM smoke attempt: marker insertion remained idempotent, and the simulated feed stayed hidden in editing view. **Visible slideshow checks were blocked because Windows denied foreground activation from the automation environment.** This is not a full Office smoke pass.
- Ribbon loading through the VSTO runtime, real Android/scrcpy playback, mixed-DPI/projector/Presenter View, exports/capture and full acceptance are **not yet verified**. Compilation and fake-device tests do not establish these behaviors.

GitHub Actions runs the complete Release build and automated tests on `windows-2022`. Its result must be inspected separately; merely adding the workflow is not evidence of a CI pass. The optional Office harness is excluded from CI because it requires desktop PowerPoint, an interactive foreground desktop and no existing PowerPoint sessions.

## Automated test coverage

`pwsh ./scripts/build.ps1` builds and runs `tests/HjvTak.LiveAndroid.Tests`. It is a dependency-free test executable returning a nonzero exit code on any failure.

- Letterbox/pillarbox fitting, negative monitor origin, scaled marker, portrait slide and moved window.
- Empty/invalid/nonfinite geometry and partly off-slide markers fail closed.
- All combinations of the seven overlay visibility gates.
- Real Windows argument parser round trips: quotes, backslashes, spaces, Unicode and option-like serial text; reject embedded newlines/NULs.
- Video-only launch options and no implicit device choice when a serial is supplied.
- Startup timeout, stopped/starting/running/faulted transitions, disconnect, window loss and explicit restart.
- Real child process with an offscreen WinForms window: exact process ownership, hide, stop, repeated stop and restart.
- Child process failure/exit code and diagnostic capture; missing executable handling; invalid Windows executable launch failure and recovery.

The fake scrcpy window has a 15-second dead-man timer. Tests never require a phone and never kill the shared ADB server or unrelated processes.

## Opt-in PowerPoint smoke

Close all PowerPoint sessions. From a **Visual Studio Developer PowerShell** on an interactive desktop:

```powershell
pwsh ./scripts/build.ps1
msbuild tests/OfficeSmoke/OfficeSmoke.csproj /t:Build /v:minimal
./tests/OfficeSmoke/bin/Debug/OfficeSmoke.exe "$PWD/tests/HjvTak.LiveAndroid.Tests/bin/Debug/HjvTak.LiveAndroid.Tests.exe"
```

The harness refuses to use an existing PowerPoint process. It creates a temporary unsaved deck, exercises the real controller through COM with a fake Android process, and closes its own deck/application in `finally`. It checks marker idempotence, editing visibility, marked/unmarked slides, black/white screen, toggle and show end. Exit 0 = passed; 1 = failed; 2 = prerequisites/environment blocked. Foreground refusal is explicitly incomplete, never counted as a pass. This tests controller integration, not ribbon loading or real Android video.

## Manual acceptance matrix (all pending unless evidence above says otherwise)

Record Windows, Office build/channel/bitness, scrcpy version, Android model/version, monitor layout and DPI for every run.

| Area | Procedure and expected result |
| --- | --- |
| Fresh setup | Clean clone → build → register → open PowerPoint. Android Live ribbon appears; settings, dialogs and callbacks work. Unregister then reopen; ribbon is gone. |
| 32/64-bit Office | Repeat runtime tests on both. AnyCPU/PInvoke builds do not substitute for runtime checks. |
| Wi-Fi / QR | On a PC without OBS, use a tablet with no USB cable. Scan from Wireless debugging; exact tablet pairs and video starts. Test two nearby tablets, wrong scanner, expired code, unavailable mDNS, cancel and retry. A scrcpy path with spaces works. |
| Wi-Fi reconnect | Stop/start using saved endpoint; change tablet IP/port; old address must fail clearly, then QR or a verified current connection address succeeds. Offline/unauthorized devices must never be treated as connected. |
| Basic show | Move/resize marker, run full-screen and windowed shows; feed remains inside its rectangle with device aspect preserved. An unmarked slide hides it; return shows it. |
| Long transitions | Use a transition longer than 250 ms. Overlay stays hidden through the transition and then returns. Animated marker behavior is explicitly unsupported. |
| Blanking/pause | B, W, pause and resume; feed disappears while blank/paused and returns. Account for timer latency. |
| Focus | Switch to another application or minimize PowerPoint; no overlay covers that app. Return to show; feed returns without keyboard focus theft. Mouse on overlay does not advance slides. |
| Presenter View | Projector + presenter monitor; feed belongs to slideshow, not notes or editing window. Change active deck and confirm geometry still comes from the shown deck. |
| DPI/layout | 100/125/150/200%, mixed scaling, left/above negative-origin monitor, move/resize window, portrait and 4:3/16:9 decks. Measure rectangle alignment. |
| Multiple shows | Open two shows: feed hides with status instead of picking the wrong deck. Close one: valid remaining show can resume. |
| Unsupported marker | Rotated/grouped/hidden/off-slide marker does not produce a misplaced overlay. Keep one named marker per slide, without marker animations. |
| Rotation/disconnect | Rotate the phone both ways; disable tablet Wi-Fi or terminate the owned scrcpy instance. No frozen orphan window. Start reconnects; PowerPoint remains responsive. |
| End/shutdown | End show, close presentation and exit PowerPoint; video hides and owned process exits on add-in shutdown. Repeated Stop is safe. An unrelated scrcpy session survives. |
| Capture/export | Test whole-screen sharing/recording explicitly. Confirm slide-only sharing, PDF and native PowerPoint video export do not include this external feed; do not imply they do. |
| Duration | Run 30 minutes with navigation and reconnects; record latency, CPU/GPU, memory and any COM/process leaks. |

Promotion to **0.1.001** requires the agreed requirements to pass and explicit user acceptance. This matrix is a test plan, not a declaration that those tests have run.
