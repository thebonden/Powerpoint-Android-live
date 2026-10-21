# Validation — development version 0.0.002

## Recorded local evidence (2026-10-21)

- Full VSTO **Debug and Release builds succeeded** on Windows with Visual Studio 18/2026, .NET Framework 4.8. The DLL, signed application manifest and signed deployment `.vsto` were generated.
- **13 automated test groups passed**, including 128 visibility-input combinations, Windows command-line argument round trips, geometry and process/window integration using a simulated scrcpy executable.
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
- Child process failure/exit code and diagnostic capture; missing executable handling.

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
| USB/device selection | Single authorized phone connects; unauthorized/missing device gives actionable status and timeout. Two devices require a serial. A path with spaces works. |
| Basic show | Move/resize marker, run full-screen and windowed shows; feed remains inside its rectangle with device aspect preserved. An unmarked slide hides it; return shows it. |
| Long transitions | Use a transition longer than 250 ms. Overlay stays hidden through the transition and then returns. Animated marker behavior is explicitly unsupported. |
| Blanking/pause | B, W, pause and resume; feed disappears while blank/paused and returns. Account for timer latency. |
| Focus | Switch to another application or minimize PowerPoint; no overlay covers that app. Return to show; feed returns without keyboard focus theft. Mouse on overlay does not advance slides. |
| Presenter View | Projector + presenter monitor; feed belongs to slideshow, not notes or editing window. Change active deck and confirm geometry still comes from the shown deck. |
| DPI/layout | 100/125/150/200%, mixed scaling, left/above negative-origin monitor, move/resize window, portrait and 4:3/16:9 decks. Measure rectangle alignment. |
| Multiple shows | Open two shows: feed hides with status instead of picking the wrong deck. Close one: valid remaining show can resume. |
| Unsupported marker | Rotated/grouped/hidden/off-slide marker does not produce a misplaced overlay. Keep one named marker per slide, without marker animations. |
| Rotation/disconnect | Rotate the phone both ways; disconnect USB or terminate the owned scrcpy instance. No frozen orphan window. Start reconnects; PowerPoint remains responsive. |
| End/shutdown | End show, close presentation and exit PowerPoint; video hides and owned process exits on add-in shutdown. Repeated Stop is safe. An unrelated scrcpy session survives. |
| Capture/export | Test whole-screen sharing/recording explicitly. Confirm slide-only sharing, PDF and native PowerPoint video export do not include this external feed; do not imply they do. |
| Duration | Run 30 minutes with navigation and reconnects; record latency, CPU/GPU, memory and any COM/process leaks. |

Promotion to **0.1.001** requires the agreed requirements to pass and explicit user acceptance. This matrix is a test plan, not a declaration that those tests have run.
