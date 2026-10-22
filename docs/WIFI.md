# Wi-Fi / QR connection — 0.0.003

PowerPoint is standalone. It launches its own small QR helper; no OBS installation, running OBS process or plugin is needed. The helper reuses the working QR-pairing logic from the user's OBS project, with its source and license preserved in `native/WifiQr/NOTICE.md`.

## Workflow

`Forbind via Wi-Fi / QR` opens the connection dialog and starts the bundled `tools/Android-WiFi-QR.exe`. The helper receives only the path to the selected distribution's `adb.exe`; it generates a fresh Android ADB QR payload in memory. The user scans in Android's Wireless debugging pairing screen.

The helper discovers `_adb-tls-pairing._tcp` for that random session name, sends the secret to `adb pair` through stdin, then discovers `_adb-tls-connect._tcp` for the exact device GUID returned by authenticated pairing. It connects to that service's port and verifies `get-state`. The two ports are not interchangeable. No service belonging to a different session/device is selected.

On success the helper writes exactly `ANDROID_PPT_QR_V1 <IPv4:connection-port>` to its private inherited stdout pipe and closes. PowerPoint validates the format, reconnects/verifies `get-state` again, then saves the endpoint and starts the feed. Closing/cancelling, malformed output, timeout, offline state or failed exit does not change saved settings or start a new feed.

`Start / reconnect` repeats the check against the saved endpoint. Changed addresses/ports require another QR pairing or the current connection address from the tablet's main Wireless debugging screen. The manual option assumes this PC is already paired. Old USB serial values from 0.0.002 are not automatically used for a wireless feed; the dialog opens QR pairing instead.

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
