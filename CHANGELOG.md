# Changes

## 0.0.113 — 2026-10-26 (development)

- Add a common tablet/Wi-Fi logo and action icons to the Android Live ribbon.
- Put the shared logo into new/explicitly updated slide frames while retaining editable names, rectangle geometry and device routing. Existing slides are not silently restyled.
- Use the shared logo in the waiting surface only after the existing 15-second delay; include the .112 native/managed window lifecycle fix.
- Add searchable offline Danish help with symptom-based guides, teaching-day preparation, installation and export limitations.
- Replace the compact Status message box with read-only local checks and details in Help; allow explicit refresh and copying without automatic upload.
- Keep Help available during connection startup and add artwork, Office image conversion, search and status tests.
## 0.0.112 — 2026-10-25 (development)

- Fix a blank-cover lifecycle error in .111: native window visibility alone did not start WinForms visibility/Load/Shown, leaving the editor caption exposed.
- Show the cover through the nonactivating Form lifecycle with its slideshow owner, then apply native placement. Hide through the matching Form lifecycle before detaching.
- Extend the real-window regression to require managed visibility and Load/Shown across hide/re-show, as well as no foreground change. The stronger assertion fails on .111.
- Keep the 15-second text/icon delay and immediate ready-video behavior unchanged.
## 0.0.111 — 2026-10-25 (development)

- Show a plain dark cover while a slideshow frame has no live video; delay BOTH the waiting text and tablet/Wi-Fi icon by 15 seconds.
- Replace the cover immediately when video is available; the delay never holds back ready video beyond the existing slide-transition guard.
- Track waiting independently per device, reset on slide visits and new outages, and cover offline profiles without a running process.
- Use both Android and custom names in the waiting message. Keep editor captions and saved presentations untouched.
- Hide/detach covers on background, black/white/pause, overlay disable, show end and disposal; discard unused windows.
- Add boundary, recovery, multi-device, navigation, actual painting and nonactivation/lifetime tests.
- User confirmed the 0.0.110 automatic-start fix works; .111 installed slideshow/transition acceptance remains pending.
## 0.0.110 — 2026-10-25 (development)

- Fix automatic startup/recovery failing with “Call the controller on PowerPoint's UI thread” despite an authenticated tablet connection.
- Establish a dedicated STA dispatch context at each timer, manual-start and async ribbon entry; do not depend on Office retaining a startup synchronization context.
- Restore the caller's context after entry, preserve the UI context across nested awaits and keep the dispatch window alive until in-flight operations finish during disposal.
- Add actual message-pump tests for missing/replaced Office callback contexts, nested cancellation and shutdown.
- Confirm the installed 0.0.109 manual Forbind valgt workaround restored the user's live preview without QR; new automatic startup awaits installed acceptance.

## 0.0.109 — 2026-10-25 (development)

- Automatically resume saved, identified tablets on PowerPoint startup, without opening QR or preview windows.
- Persist explicit Stop preferences; closing PowerPoint alone does not disable the next automatic connection.
- Monitor tablet identity through bounded ADB probes every 15 seconds; recover after two failures or a lost video process.
- Retry independently per tablet with bounded discovery and 2/5/10/30-second backoff; cancel stale work on stop, removal and shutdown.
- Preserve selected tablet, preview/overlay choices, names, options and slide routing during background recovery; save refreshed endpoints.
- Make Forbind valgt reconnect without opening QR, and retain explicit QR repair for missing pairing.
- Configure child SDL windows not to activate themselves when shown, preserving the existing initialized-window readiness guard.
- Add tests for startup, cancellation, liveness, independent process recovery and nine simulated hours with eight outages.

## 0.0.108 — 2026-10-24 (development)

- Remove the development version from slide frame captions.
- Show **Livebillede fra Android name — custom name** followed by **under slideshow**, reusing the list's equal-name and missing-metadata handling.
- Include both names in the frame's alternative text.
- Rename the frame action to **Indsæt / opdater ramme**. Reusing it updates the selected tablet's existing frame on the current slide instead of only selecting it.
- Retain the existing shape, slide identity and placement; frame text is refreshed only by the explicit insert/update action.

## 0.0.107 — 2026-10-24 (development)

- Add an editable **Dit navn** field beside the tablet list; Enter saves a custom name without requiring a connection.
- Move the rename action next to the list and label it **Ændr navn…**.
- Keep the rename dialog text field editable in automatic Android-name mode; typing selects custom mode automatically.
- Preserve device IDs, Android names, endpoints, options and existing slide assignments when repeatedly renaming a saved device.
- Document renaming for reused slides and retain the explicit return-to-Android-name option.

## 0.0.106 — 2026-10-24 (development)

- Show Android device name and custom PowerPoint alias together in the tablet list, without repeating equal names.
- Clearly indicate an unavailable Android name while preserving the user's alias.
- Add confirmed removal of a saved device, stopping and disposing only its stream and removing its status entry.
- Persist removal, prevent legacy single-device settings from resurrecting a removed tablet, and select the next remaining device.
- Preserve slides, other device identities/options and Android pairing; document that removed-device frames need replacement if the device is added again.
- Include regression coverage for dual-name persistence, last-device removal, independent selection and live-process cleanup.

## 0.0.105 — 2026-10-24 (development)

- Refresh Android names for existing generated Tablet/Tablet N profiles, not only newly added devices.
- Persist automatic/custom naming mode and last known Android name independently.
- Apply names after QR/manual pairing and normal/bulk reconnection, while retaining stable slide IDs.
- Add an explicit Android-name checkbox to Rename: choose a custom PowerPoint name or return to the device name.
- Preserve non-placeholder legacy names conservatively and retain names when metadata cannot be read.
- Include 0.0.104 full-screen proportional fitting and 0.0.103 startup fix.

## 0.0.104 — 2026-10-24 (development)

- Always request full-image letterbox rendering with preserved proportions and black padding in preview and slide frames.
- Disable scrcpy's window aspect lock so it accepts arbitrary frame shapes while preserving the video aspect ratio. No crop, unscaled or stretched rendering is requested.
- Document scrcpy 4.1+ for these options and explain the fit behavior in the ribbon and installation guide.
- Extend the real-device probe to normal, tall and wide frames and verify client bounds after SDL processes each resize.
- User confirmed installed 0.0.103 restores live video. This change retains its startup-readiness fix.

## 0.0.103 — 2026-10-24 (development)

- Fix a startup race that could crash scrcpy 4.1 with 0xC0000094 after successful Wi-Fi pairing.
- Wait for scrcpy to show its own off-screen video window before exposing it to preview/slideshow placement. A hidden initialization HWND is no longer treated as ready.
- Keep the acquired window across deliberate hide/show operations, preserving slide navigation and preview behavior.
- Add a gated child-process regression test and an opt-in real-tablet startup/resize/hide/resume probe.
- Ship updated ordinary and IT installers; settings and slide identities are unchanged.

## 0.0.102 — 2026-10-23 (development)

- Use neutral Android Live branding, assembly/project names, namespaces, settings paths and development certificate identity.
- Retain old identifiers only in a compatibility class for registration migration, saved settings and existing slide routing.
- Add explicit silent install/uninstall/detect commands, per-user deployment logs and return codes for central IT rollout.
- Refuse service-account installation, avoid interactive prerequisite prompts in silent mode and check managed publisher trust before removing an older build.
- Provide an IT ZIP with the same EXE, user-context install/uninstall wrappers, detection and Intune guidance.
- Add regression coverage for old/new settings precedence, unchanged tablet names/IDs, legacy frame routing, CLI validation, manifest detection, DTD rejection and unsigned publisher refusal.

## 0.0.101 — 2026-10-23 (development)

- Add one self-contained Windows installer EXE: no manual ZIP extraction or folder selection.
- Validate embedded payload hash, paths, completeness and size before changing Office registration.
- Use a stable short per-user appdata folder; preserve tablet settings and presentations.
- Add Microsoft prerequisite-only bootstrapper, PowerPoint checks, recognized-old-version migration and recovery attempts after failed/cancelled installation.
- Verify success using VSTO exit code and Office registry state. Retain backups and report incomplete recovery.
- Add eight installer regression groups and read-only verification of the actual packaged EXE.
- Public publisher signing and clean-machine/upgrade acceptance remain prerequisites for broad release; no central IT deployment or silent install yet.

## 0.0.100 — 2026-10-23 (development)

- Start the requested 0.0.1xx sequence (assembly/ClickOnce 0.0.100.0).
- Produce a complete setup.exe installation ZIP locally and in CI, with prerequisite checks, Danish instructions, matching QR source and licenses.
- Read Android device names for new tablets, with model/numbered fallback; add Omdøb enhed independent of scrcpy setup.
- Reconnect known tablets after an IP/port change through bounded mDNS discovery and hardware-identity matching. Preserve custom names and stable slide tags.
- Clarify Klargør / forbind alle and Opdater valgt / QR for reusing a PPTX on the same PC on another day.
- Add eleven regression groups (51 total), including address reuse and explicit QR update without losing slide assignments.
- Keep pairing QR short-lived; Android controls pairing authorization. No 24-hour connection guarantee is implied.

## 0.0.006 — 2026-10-23 (development)

- Show an automatic external preview after connection, making live video visible before a slideshow.
- Save multiple named tablet profiles and run independent feeds. Assign a device to each slide frame; support different devices between slides and several devices on one slide.
- Add per-device sound, physical touch indicators and Automatic/Landscape/Portrait capture orientation; preserve choices and restart only the affected stream.
- Add selected/all connection and stop controls, settings migration, duplicate-frame rejection and cancellation for device preparation.
- Add regression coverage for options, natural/override dimensions, exact-device orientation queries, settings migration/XML and independent live process stop/restart/failure.

Numeric assembly/ClickOnce version: 0.0.6.0. Hardware acceptance of the new features remains pending.

## 0.0.005 — 2026-10-23 (development)

- Resolve the native QR helper from the original signed deployment location instead of VSTO's managed-DLL shadow-copy folder.
- Add a real CLR shadow-copy regression test (34 managed groups total).
- Verify the installed add-in loads normally and the user's physical tablet QR connection reaches scrcpy video startup. Editing-view preview and visible slideshow acceptance are separate work.

Numeric assembly/ClickOnce version: 0.0.5.0.

## 0.0.004 — 2026-10-22 (development)

- Discover complete scrcpy installations automatically before QR pairing/reconnect; remember the discovered path.
- Search PATH and bounded local installation/extraction locations in the background; skip incomplete packages and unrelated directories.
- Ask the user to install scrcpy when missing, with official installation guidance, Search again and manual selection. Add Find automatically to Settings.
- Validate required adjacent ADB/server/DLL files and add six discovery regression groups (33 managed groups total).
- Retain the desktop-context VSTO installation flow introduced while diagnosing normal-launch registration visibility.

Numeric assembly/ClickOnce version: `0.0.4.0`. Real tablet/slideshow acceptance remains pending.

## 0.0.003 — 2026-10-22 (development)

- Add PowerPoint **Forbind via Wi-Fi / QR** and verified **Start / reconnect** workflow; no USB or OBS Studio is required.
- Reuse the working native QR pairing code as an independent helper with PowerPoint wording, an explicit selected ADB path, a private endpoint protocol and preserved GPL/MIT source/license notices.
- Validate exact mDNS session/device identity and separate pairing/connection ports; recheck device state before storing the endpoint or launching the feed.
- Add bounded asynchronous command execution, cancellation, timeout, strict IPv4/result parsing and local ADB environment configuration shared with scrcpy.
- Build/package the helper with VSTO manifests and add 13 managed Wi-Fi regression groups plus native pairing edge cases; independently decode the rendered dummy QR fixture.
- Make standalone operation and the continued external-overlay limitation explicit in setup, research and acceptance documentation.

Numeric assembly/ClickOnce version: `0.0.3.0`. Physical tablet/PowerPoint validation remains pending.

## 0.0.002 — 2026-10-21 (development)

- Add complete .NET Framework 4.8 PowerPoint VSTO project, solution, generated host bootstrap, signed development manifests and repeatable build/registration scripts.
- Add Android Live ribbon: marker, Start, Stop, visibility toggle, Settings and Status/diagnostics.
- Wire slideshow lifecycle and presentation-close events; maintain alignment on the Office STA with a timer and hide the overlay when inappropriate.
- Add PID/title-scoped scrcpy management, startup timeout, output capture, safe Windows argument quoting and explicit restart.
- Add letterbox-aware geometry, monitor-origin and DPI handling, ownership without global always-on-top, and conservative unsupported/multiple-show behavior.
- Add automated Windows tests, a separate opt-in Office smoke harness, CI and manual acceptance documentation.
- Investigate Cameo/virtual-camera feasibility while keeping the external overlay distinction explicit.

Numeric assembly and ClickOnce version: `0.0.2.0`. User-facing version: `0.0.002`. This is not `0.1.001` or a tested/accepted production release.

## 0.0.001

Initial source-only controller and project direction; no complete VSTO project or runnable build pipeline.
