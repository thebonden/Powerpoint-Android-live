# Standalone Wi-Fi QR helper provenance

This directory reuses the user's working `android-usb-obs` QR helper (the integrated QR source in the delivered 0.3.x project), with permission to reuse its code and ideas for an independent PowerPoint application. Original files carry GPL-2.0-or-later; that license is retained here in `LICENSE` and the source headers. The QR generator is Nayuki QR-Code-generator 1.8.0, MIT, with its original license in `vendor/qrcodegen/LICENSE.txt`.

Adapted files: `qr-main.cpp`, `qr-pairing.hpp`, `qr-result.hpp`, `windows.cpp/.hpp`, and `tests/qr-tests.cpp`. `pairing-common.hpp` extracts only endpoint validation and Windows argument quoting from `stream.hpp` and `protocol.hpp`. Video decoding, libobs, OBS headers, sockets for media streaming and OBS installation logic are not included.

Changes for PowerPoint: UI wording and session prefix, `--powerpoint` result mode, `ANDROID_PPT_QR_V1` private stdout protocol, and an explicit `--adb <absolute-path>` selected through PowerPoint. ADB is supplied by the user's official scrcpy distribution; the old OBS bundle checksum is intentionally not applied to a different installation. The helper never downloads tools or changes the firewall. Pairing secrets remain transient and travel to `adb pair` via stdin, not arguments or saved settings.

The helper builds as a separate Windows x64 executable using the static C++ runtime. PowerPoint invokes it as a subprocess; it requires no OBS executable, plugin, service or library. Its complete corresponding source and licenses ship in this repository. Build artifacts include the license notices; source artifacts are produced by CI.
