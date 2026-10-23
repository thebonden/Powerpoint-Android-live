# Changes

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
