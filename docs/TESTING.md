# Validation — development version 0.0.109

## Restart, idle recovery and teaching-day simulation — 2026-10-25

- User clarified failures after closing/reopening PowerPoint or leaving it idle, with one hour of preparation followed by eight hours of teaching.
- Code inspection found no startup resume or automatic retry after session faults. Added per-device recovery, preserving the existing identity-matched discovery and readiness guard.
- Release/package validation: 73 managed groups and 14 installer groups, native QR checks and actual EXE payload/CLI verification.
- New coordinator tests cover initially offline startup, retry spacing, reset only after 60 seconds of stable video, two failed probes, one transient miss, cancellation during retry/probe, and stale completion after replacement.
- Preference XML tests cover legacy defaults and explicit Stop persistence. Probe tests verify the exact target/identity and absence of pairing/disconnect commands.
- Two actual simulated child processes verify automatic restart after one process exits while the other retains the same running window.
- A virtual clock advances nine hours, with eight one-minute outages and successful recovery each time; monitoring continues through long idle intervals. This is not physical Wi-Fi/Android endurance evidence.
- Child SDL activation behavior follows [SDL3](https://wiki.libsdl.org/SDL3/SDL_HINT_WINDOW_ACTIVATE_WHEN_SHOWN) and [SDL2](https://wiki.libsdl.org/SDL2/SDL_HINT_WINDOW_NO_ACTIVATION_WHEN_SHOWN) hints. Installed PowerPoint focus and foreground behavior require hardware acceptance.
- Pairing versus connection follows the [Android ADB guide](https://developer.android.com/tools/adb#wireless). The add-in does not reset shared ADB, repair authorization, override power settings or launch QR automatically.
- Required installed acceptance: reopen PowerPoint, verify startup resumes; interrupt Wi-Fi then restore it; leave a slideshow idle and on slides without frames; verify focus, selection and audio options; Stop then reopen and confirm it remains stopped; run one hour setup plus eight hours teaching with breaks on the target network.
- No real PowerPoint presentation, device authorization, saved user settings or installed Office registration was changed by the agent. ADB liveness cannot detect every frozen app/decoder while the transport remains responsive.

## Frame captions with both names — 2026-10-24

- New and explicitly refreshed frames use the same dual-name label as the device list, followed by under slideshow, without a version number.
- The existing-frame branch writes the caption on the matching device ID and selects that same shape; it does not create a replacement shape or rewrite routing tags.
- Release/package validation: 63 managed groups and 14 installer groups, native QR checks and actual EXE payload/CLI verification. Existing label tests cover custom names, identical names, missing metadata and name refresh.
- Installed Office acceptance remains pending: insert a new frame, refresh an old version-bearing frame, rename then refresh, and verify slide routing and appearance. Existing user text in a frame is replaced only when the user explicitly chooses the insert/update action.
- No real presentation, device settings or Office registration was changed by the agent.

## Direct renaming for reused slides — 2026-10-24

- User reported being unable to enter a name. Inspection found the previous dialog disabled its text field while Android-name mode was selected; the action was in a separate setup group.
- The ribbon now exposes an editable name field and the rename action next to device selection. The dialog allows typing in either mode and automatically chooses custom naming on edits.
- Release/package validation: 63 managed groups plus 14 installer groups, native QR tests and actual EXE payload/CLI checks.
- Added offline/repeated-rename round-trip and reconnect coverage: stable slide ID, Android name, hardware identity and audio/touch/orientation options remain unchanged. Empty names are rejected without overwriting the previous name.
- Ribbon XML/callback bindings are checked locally. Actual installed Office field editing and repeated names on reused slides still require user acceptance. Existing frame captions are not automatically rewritten.
- The user installs this update. No real presentation, saved settings or Office registration was changed by the agent.

## Dual device names and removal — 2026-10-24

- The list displays the stored Android name plus the custom alias; equal names are not repeated. Unknown Android metadata is identified explicitly.
- Release/package validation: 62 managed groups and 14 installer groups, native QR tests and actual EXE payload/CLI checks.
- Regression coverage checks labels after reconnection and XML reload, removing the last legacy profile without resurrection, missing IDs, deterministic selection and preservation of other profiles.
- The two-child-process test removes one running stream and verifies its window is destroyed, its session entry is absent and the other process remains running with the same window.
- Removal requires user confirmation and saves settings before stopping the stream. Save failure restores the previous in-memory list/selection. Slides and Android pairing are not changed.
- Installed ribbon/confirmation acceptance remains pending: cancel removal, remove selected and last devices, restart PowerPoint and verify other feeds/frames. No real saved device was removed and no Office installation was performed by the agent.

## Default Android names and explicit custom names — 2026-10-24

- Inspection confirmed the saved profile still used Tablet 1. Previous code only applied the reported Android name when creating a new profile; reconnect paths preserved every existing name, including generated placeholders.
- Naming mode (Android/custom) and last Android name are now persisted separately. Legacy generated placeholders migrate to automatic mode; other legacy names are preserved. Explicitly custom names, including Tablet N, remain custom on reconnect.
- Local Release/package build passed native QR checks, **59 managed add-in groups and 14 installer groups**, plus actual EXE payload/CLI verification.
- Added cases for first naming, old-placeholder migration, subsequent Android-name changes, unavailable metadata, explicit custom names, return to Android naming and XML persistence with unchanged slide IDs. Existing QR/reconnection tests still pass.
- Reading the tablet's actual Android name could not complete because its saved endpoint was no longer connected. No device address, pairing credential or hardware serial is committed. Installed naming/rename acceptance remains pending.
- The user installs the update themselves. No 0.0.105 Office installation, presentation mutation or saved-user-settings edit was performed by the agent. This package includes the tested 0.0.104 fitting and 0.0.103 startup fixes.
## Full Android screen inside the frame — 2026-10-24

- User confirmed that installed 0.0.103 restores live video, then requested that the complete Android screen always fit inside its frame.
- Explicitly request --render-fit=letterbox, --no-window-aspect-ratio-lock and a black background. Retain the full default display capture with no crop. Existing option-combination tests check this contract across audio/touch/orientation choices.
- Local Release/package build passed native QR checks, 56 managed test groups and 14 installer groups, plus actual EXE verification.
- The paired tablet passed three eight-second production-session probe runs in 640x400, 320x600 and 900x220 client frames. Tests checked decoded texture availability, client position/size after SDL event processing (including a further width change), hide/resume and owned-process cleanup.
- The baseline also passed frame-bound checks: these tests establish geometry compatibility, not reproduction of specific missing pixels reported by the user.
- A separate 45-second live scrcpy run with the explicit fit options received video and exited normally. Automated visual inspection could not complete because Computer Use app approval timed out; no pixel-completeness claim is based on a screenshot.
- 0.0.104 has not been installed into the user's PowerPoint. Installed preview/slideshow acceptance, physical rotation and checking all four display edges still require user confirmation. No presentation or device settings were changed.
- Behavior follows the official [scrcpy 4.1 window documentation](https://github.com/Genymobile/scrcpy/blob/v4.1/doc/window.md#render-fit). The window can change aspect ratio while letterbox preserves the image proportions. scrcpy 4.1+ is required for the explicit fit options.
## Live video startup correction — 2026-10-24

- Installed 0.0.102 connected successfully to the user's tablet but scrcpy exited with -1073741676 (0xC0000094); Windows Application Error events confirmed the crash in scrcpy.exe. Saved tablet identity/options and neutral settings migration were intact.
- A standalone six-second scrcpy 4.1 run using the same paired tablet produced a 1920x1200 texture and exited normally. The old production session/placement probe reproduced the same native crash during its third start. This isolates a window-startup race rather than a missing Wi-Fi pairing.
- The new child-process regression creates a real hidden HWND, blocks first ShowWindow on an explicit event, and checks that no usable handle/Running state is published before the event. It failed before the fix (Expected Starting, got Running), and passes after it. It also verifies that deliberate hiding after acquisition retains readiness.
- Local Release/package validation passed native QR checks, **56 managed add-in groups and 14 installer groups**, plus actual EXE extraction/CLI probes.
- With the corrected production ScrcpySession and NativeWindow code, three consecutive eight-second runs against the paired Samsung SM-X356B/Android 16 passed video-texture detection, placement, resizing, hide/resume and stop. The opt-in probe does not pair, record, save device settings or access Office.
- User installs updates themselves. The 0.0.103 add-in has not been installed or accepted inside their running PowerPoint. No presentation was changed or saved by these checks. Full installed slideshow/Presenter View and longer hardware acceptance remain pending.
- Startup behavior was checked against [scrcpy 4.1 screen initialization](https://github.com/Genymobile/scrcpy/blob/v4.1/app/src/screen.c): the initial HWND precedes content initialization; scrcpy shows the video window after initializing its dimensions. The add-in waits for that first visibility handshake, not a fixed sleep or log-text timing.

Optional developer-only hardware probe, after building tests:
    AndroidLive.Tests.exe --probe-live-startup "<full scrcpy.exe path>" "<paired IPv4:port>"

Use an already paired test tablet. The probe opens its own temporary preview, performs three runs and closes only its own streams. It is never run by CI or package installation.
## Neutral identity and central deployment — 2026-10-23

- Local Release/package build passed native QR checks, 55 managed add-in test groups and 14 installer test groups. The actual EXE passed payload verification and CLI probes. Deployment scripts passed PowerShell syntax validation; both distribution archives were inspected. No 0.0.102 installation or Office registration change was performed by the agent.
- Added cases for legacy settings migration without losing custom names/IDs, neutral-file precedence, corrupt neutral-file handling and old/new slide tags.
- Added installer cases for old manifest recognition, strict managed CLI parsing, minimum-version detection, unsigned manifest refusal and prohibited DTDs.
- Package verification exercises the actual EXE with --verify-package, invalid --silent and read-only --detect. It never installs/uninstalls Office.
- Required manual acceptance: neutral branding in real PowerPoint, upgrade from an older installation, old PPTX routing, approved-certificate silent rollout in an IT pilot, missing-trust/dependency failure, running PowerPoint retry and user-context uninstall.
- Central deployment is per-user. SYSTEM installation, automatic trust provisioning and machine-wide MSI are not implemented. No actual tenant rollout has occurred.

## Single-file installation — 2026-10-23

- Add-in Release build: 51 managed groups plus native QR tests passed.
- Installer: eight groups passed (valid extraction, corrupted payload, path traversal, duplicate Windows paths, incomplete payload, settings-preserving replacement/rollback, failed swap recovery, accepted/rejected old manifest locations).
- The actual EXE's --verify-package path extracted and validated the embedded archive without launching prerequisites or changing Office registration.
- User chose to perform installation themselves. No 0.0.101 installation was performed by the agent.
- Required acceptance before broader distribution: fresh Windows/Office user, missing prerequisites, normal desktop launch, update from 0.0.006/0.0.100, update from fixed installer folder, cancelled Microsoft dialog, restoration failures, restart-required prerequisites and uninstall.
- Development EXE is not publicly signed. No production release or silent/central deployment acceptance claimed.

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

- The user reproduced the missing ribbon after closing the successful test session and starting PowerPoint normally. The normal instance did not enumerate `AndroidLive`; the Disabled Items list was empty. The earlier runtime check below therefore does **not** establish reliable installation.
- A probe in the development process saw the HKCU add-in key with LoadBehavior 3. A separate read-only probe launched by the actual Explorer desktop, under the same user, reported that the key did not exist. This explains why a development-host launch succeeded while a desktop launch failed. Package-identity detection alone did not identify this registry-view difference.
- Running the Microsoft installer through that desktop context required the normal user trust prompt for the locally signed development build; silent installation returned -300 and did not register the add-in. No trust/security setting was bypassed.
- Added `scripts/install-dev.ps1` to open the official installation dialog through Explorer's desktop Application. Its `-WhatIf` validation passed against the existing Release build. Updated the low-level registration message and setup docs to avoid treating a private registry write or installer launch as successful installation.
- The user subsequently approved the trust prompt and confirmed the ribbon. Desktop-brokered launches of 0.0.004 and 0.0.005 loaded the add-in; the later QR evidence is recorded above. Visible slideshow playback remains pending.

## PowerPoint runtime check (2026-10-22)

- After the local package was installed with Microsoft's VSTOInstaller and all PowerPoint processes exited, a fresh PowerPoint launch reported `AndroidLive` with `COMAddIn.Connect = true`. VSTO loader and CLR modules were loaded.
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

`pwsh ./scripts/build.ps1` builds and runs `tests/AndroidLive.Tests`. It is a dependency-free test executable returning a nonzero exit code on any failure.

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
./tests/OfficeSmoke/bin/Debug/OfficeSmoke.exe "$PWD/tests/AndroidLive.Tests/bin/Debug/AndroidLive.Tests.exe"
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
