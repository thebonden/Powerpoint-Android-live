# Changes

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
