# Wi-Fi / QR connection — 0.0.006

PowerPoint is standalone. It launches its own small QR helper; no OBS installation, running OBS process or plugin is needed. The helper reuses the working QR-pairing logic from the user's OBS project, with its source and license preserved in `native/WifiQr/NOTICE.md`.

## Automatic scrcpy setup

Before opening QR pairing, PowerPoint checks the saved scrcpy path and, when it is absent/incomplete, searches in the background. It reads fresh user/machine/process PATH values and checks local WinGet packages, Program Files, local Programs, Downloads, Desktop and supported package locations. Search is limited to two levels of scrcpy-named folders (up to 512 child directories per level); it does not scan the whole disk or network shares and does not execute candidate programs. Custom or redirected extraction locations can be selected manually.

A candidate must include scrcpy.exe, adjacent adb.exe and scrcpy-server, plus SDL, avcodec, avformat and avutil DLLs. This is a completeness check, not signature verification. A valid saved selection takes precedence. If no package qualifies, the Danish setup dialog asks the user to install scrcpy and opens only the official installation guide when the user clicks **Installér scrcpy…**. It does not download or run an installer automatically. **Søg igen** reads the locations again without requiring a PowerPoint restart. Cancel does not start pairing or change the saved path.

## Workflow

`Tilføj tablet / QR` opens the connection dialog and starts the bundled `tools/Android-WiFi-QR.exe`. The helper receives only the path to the selected distribution's `adb.exe`; it generates a fresh Android ADB QR payload in memory. The user scans in Android's Wireless debugging pairing screen.

The helper discovers `_adb-tls-pairing._tcp` for that random session name, sends the secret to `adb pair` through stdin, then discovers `_adb-tls-connect._tcp` for the exact device GUID returned by authenticated pairing. It connects to that service's port and verifies `get-state`. The two ports are not interchangeable. No service belonging to a different session/device is selected.

On success the helper writes exactly `ANDROID_PPT_QR_V1 <IPv4:connection-port>` to its private inherited stdout pipe and closes. PowerPoint validates the format, reconnects/verifies `get-state` again, then saves the endpoint and starts the feed. Closing/cancelling, malformed output, timeout, offline state or failed exit does not change saved settings or start a new feed.

`Forbind valgt` repeats the check against the saved endpoint. Changed addresses/ports require another QR pairing or the current connection address from the tablet's main Wireless debugging screen. The manual option assumes this PC is already paired. Old USB serial values from 0.0.002 are not automatically used for a wireless feed; the dialog opens QR pairing instead.

## Requirements and troubleshooting

- Android tablet/phone with QR pairing under Wireless debugging (Android 11+), unlocked during pairing; PC and tablet on the same local network.
- Full official scrcpy Windows distribution with adjacent `adb.exe`; compatible ADB with `pair` and `mdns services`. The build does not bundle or download ADB.
- Network must permit client-to-client traffic and mDNS. Isolated guest Wi-Fi or enterprise network restrictions can prevent discovery; changing the network is a user/network-administrator decision.
- Scan from **Wireless debugging → Pair using QR code**, not a camera QR scanner. The payload is an ADB pairing code, not Wi-Fi-network credentials.
- If pairing succeeds but discovery fails, use the connection IP:port from the main Wireless debugging screen, not the pairing-code popup.
- QR expires after three minutes. Retry generates new credentials. The outer command is bounded to four minutes; after that, restart from PowerPoint.
- No automatic reconnect to another device: IP/port changes are verified explicitly. IPv6 and hostname endpoints are not supported by this extracted implementation.
- To revoke pairing, use Android's paired-device list and forget this PC. Stop Android in PowerPoint stops the feed, not the shared ADB server or Android's persisted trust.

## Implementation boundaries

- The helper's commands explicitly target `-H localhost -P 5037`. The managed connector and scrcpy receive the same local ADB environment and selected executable path.
- Native pairing runs off its UI thread. Managed commands are asynchronous, bounded, cancellable and capture at most 32 KiB per output stream. PowerPoint COM calls remain on its UI thread after a successful dialog result.
- Cancellation gracefully closes the owned helper before a bounded kill fallback. Its worker cancels its own ADB command process. Neither implementation kills the shared ADB daemon or another mirroring application.
- Secrets are random, not written to the helper's command line, stdout, settings or diagnostics. The helper redacts the session secret from displayed ADB errors. Tests and preview fixtures use fixed dummy credentials only.
- Reusing the pairing component does not change rendering: this release still aligns an external scrcpy window with a slide marker. Native slide video remains a separate investigation.

Primary Android reference: [ADB — connect to a device over Wi-Fi](https://developer.android.com/tools/adb#connect-to-a-device-over-wi-fi).

## Multiple paired devices (0.0.006)

Use Tilføj tablet / QR for each additional tablet. The Tablet list selects which device the frame and option buttons affect. Name the devices under Navn / indstillinger. Forbind alle starts saved devices independently and reports individual failures while preserving successful connections. For a changed port, select the existing profile and use Par valgt igen / QR rather than adding it as a new device.

Connect all desired devices before the presentation. A slide can contain frames for several devices, or different slides can choose different devices. Only device IDs are saved with slide frames; endpoint/ADB pairing remains on this PC. On a different PC, the frames need profiles with matching assignments; automatic profile transfer is not implemented.

The preview opens after successful start. It is independent of slide editing and temporarily hides during a slideshow. See README for sound, physical touch indicators and automatic/locked capture orientation.