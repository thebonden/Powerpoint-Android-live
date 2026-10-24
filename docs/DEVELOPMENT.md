# Windows development — 0.0.108

## Prerequisites and reproducible build

- Windows 10/11; desktop PowerPoint for runtime tests.
- Visual Studio 2022 or 2026 with **Office/SharePoint development** (called **Microsoft 365 development** in newer installers), .NET Framework 4.8 developer/targeting pack, Office PIAs and VSTO tools.
- Visual Studio **Desktop development with C++** and Windows SDK for the standalone x64 QR helper; Windows x64 is required to run it.
- PowerShell 7. The normal .NET SDK alone cannot build VSTO manifests; the script locates full Visual Studio MSBuild through vswhere.

```powershell
pwsh ./scripts/build.ps1 -Configuration Debug
pwsh ./scripts/build.ps1 -Configuration Release
```

Both commands build the native QR helper and its tests first, then the actual VSTO project and the self-contained managed Windows test executable. The helper and its licenses are included as VSTO manifest content. No NuGet test framework or Android device is needed. Build output is under `src/AndroidLive/bin/<Configuration>/` and includes the DLL, utility dependency, `.dll.manifest` and `.vsto`.

The first build creates `artifacts/androidlive-development.pfx`, valid for one year, with an empty PFX password, and imports it into **CurrentUser/My** so MSBuild can sign the manifests. This is a disposable development key, not a release credential. `artifacts/Signing.props` contains its thumbprint for subsequent Visual Studio builds. Both are ignored by Git. VSTO's build tasks also create a local inclusion-list trust entry for this build. No certificate is added to Trusted Root by our script. Build/Clean override VSTO's automatic add-in registration/unregistration; explicit registration is a separate command below.

The QR sources require no OBS SDK or libraries. Changes to native helper code must go through the scripted build before building in Visual Studio. After the first scripted build, open `Powerpoint-Android-live.sln`. All generated VSTO host files are checked in. There is no manual scaffold step. For an expired development certificate, remove only `artifacts/androidlive-development.pfx` and `artifacts/Signing.props` and rerun the script; old personal certificates can be removed using Windows certificate management. Do not redistribute the key. Production publishing requires an appropriate signing/deployment process and is outside this development build.

## Install for normal PowerPoint launches

Close PowerPoint and run `pwsh ./scripts/install-dev.ps1 -Configuration Release` after the build. This opens the existing Microsoft VSTO installer **through the running Windows Explorer desktop**. Review and complete its prompt. A local development certificate can appear as an unknown publisher; this script does not silently grant trust, import a certificate into Trusted Root/Publishers, or change Office security settings. Follow organizational policy if installation is blocked.

The script only reports that it opened the installer. Verify success by starting PowerPoint from the normal Start menu/taskbar, opening a blank presentation, checking Android Live, then closing and reopening PowerPoint the same way. A successful launch from a development host is insufficient evidence.

Why this matters: during local testing the development process could read this add-in's HKCU registration while a read-only probe launched by the actual Explorer desktop (same Windows user) could not. The add-in loaded from the development host but was absent from normal PowerPoint's COM Add-ins list. Merely using `Shell.Application.ShellExecute` from the caller did not reproduce the desktop view; the script obtains Explorer's desktop Application via `FindWindowSW`. This avoids mistaking a private/isolated registry view for the ordinary user's Office registration.

Microsoft's installer manages its ClickOnce registration/cache separately from the low-level local registration below. Do not mix the two during a test. Remove the installed customization through Windows Installed apps when switching back to registry-only development; do not remove unrelated Office add-ins. Keep the original build files at their current path while this development installation is in use.

## Register and remove the local development build (advanced)

This low-level option affects the current process registry view only. When running inside a packaged/isolated development host, use the desktop installer above instead. Close PowerPoint first. Register per user (no administrator rights):

```powershell
pwsh ./scripts/register-dev.ps1 -Configuration Debug
```

This adds `HKCU/Software/Microsoft/Office/PowerPoint/Addins/AndroidLive` with a local `.vsto|vstolocal` manifest and LoadBehavior 3. Run from the same machine/user that built the manifests; copied artifacts may require explicit publisher trust. The whole output directory must stay together and must remain at the registered path. This is development registration, not a production installer.

Open PowerPoint, find **Android Live**, and follow the README. After registration, Visual Studio can attach the debugger to POWERPNT.EXE. Stop PowerPoint before rebuilding to avoid locked assemblies.

To remove only this add-in registration:

```powershell
pwsh ./scripts/register-dev.ps1 -Unregister
```

The script retains source/build files, certificates and settings. To switch between Debug/Release or another checkout, unregister first. It refuses to overwrite a different registered manifest.

## Structure

- `ThisAddIn.Designer.*`: standard VSTO host bootstrap based on the installed Microsoft PowerPoint add-in template.
- `ThisAddIn.cs`: controller lifetime and ribbon creation.
- `ScrcpySetupDialog.cs` and core `ScrcpyLocator`: background discovery, completeness checks, installation guidance and retry.
- `WifiDialog.cs` and core `WifiConnector`, `LocalCommand`, `WifiEndpoint`: QR/reconnect dialog, bounded asynchronous commands and strict endpoint/result validation.
- `native/WifiQr`: independently built GPL QR helper and MIT QR generator adapted from the working OBS project; no OBS runtime dependency.
- `AndroidRibbon.*`, `SettingsDialog.cs`, `UserSettings.cs`: ribbon callbacks, configuration and per-user XML settings.
- `LiveAndroidController.cs`: STA-only Office events, 100 ms timer, marker management and show alignment. COM references obtained by the controller are released; the VSTO-owned application/event arguments are never released.
- `ScrcpySession.cs`: launches one owned scrcpy process, reads output on background callbacks without COM access, and discovers the video window by exact PID plus unique title.
- `NativeWindow.cs`: physical client coordinates, thread DPI scope, owned popup placement with NOACTIVATE, no global TOPMOST or SetParent.
- `AndroidLive.Core`: shared source for geometry, visibility guards, command-line quoting and lifecycle state, compiled into production and tests.

The overlay owner is detached on slideshow end/presentation close. Device connection remains until Stop, failure or add-in shutdown. Phone screen rotation is rendered by scrcpy; placement is reapplied to the marker. Wi-Fi pairing/reconnection is explicit through the PowerPoint dialog; no USB is required. No download, network setup or arbitrary extra command-line options are exposed.

## Troubleshooting

- **Build cannot find VSTO/PIAs:** add the Visual Studio Office workload; use the provided script, not `dotnet build`.
- **Ribbon absent:** verify the registered manifest exists, matching VSTO runtime is installed, and inspect PowerPoint Options → Add-ins → COM Add-ins/Disabled Items. Development certificate trust and organizational policy can prevent loading. Do not bypass organizational policy.
- **No video:** use Status / diagnostics; check the executable path, Wireless debugging, network and current connection address. Use Forbind via Wi-Fi / QR to pair. The complete scrcpy distribution must be present. Reconnect with Start.
- **Connected but hidden:** use Forhåndsvisning while editing. During the one active slideshow, use the assigned tablet frame, wholly inside the slide. Blanked/paused/background shows hide all images. Duplicate frames for one device are rejected; grouping, rotation and marker animation remain unsupported.
- **COM busy:** the overlay is hidden and retried on the next timer tick. No recurring modal error dialogs are shown.
- **Settings:** `%LOCALAPPDATA%/AndroidLive/AndroidLive/settings.xml`; corrupt/unreadable settings fall back to an empty configuration. Diagnostics are bounded in memory and shown only on request.

See [TESTING.md](TESTING.md) for the opt-in PowerPoint smoke harness and remaining acceptance gates.

See [WIFI.md](WIFI.md) for the QR protocol, cancellation and network troubleshooting. The helper is x64 even with 32-bit Office; preserve the full tools directory and license files when moving the build.

## Multiple device implementation

- DeviceProfiles persist stable IDs, names, explicit endpoints and audio/touch/orientation preferences. Old Serial settings migrate to the stable legacy profile.
- DeviceStreams owns one independent ScrcpySession per profile. Stop/restart never kills other streams or the shared ADB server.
- Shape tags store the profile ID, not its IP address. PowerPoint uppercases tag values; routing normalizes these IDs. Original untagged ANDROID_LIVE_FRAME frames map to the migrated legacy device.
- A selected-device preview uses an owned external window without reparenting into Office. Slideshows hide the preview and place every connected device in its own frame; absent/duplicate/unsupported frames hide that device.
- Asynchronous ADB preparation is cancellable per device. Ribbon callbacks establish the WinForms UI context before awaiting; all Office and window placement stays on the Office STA.
- Manual orientation uses bounded, exact-device ADB wm size output and scrcpy capture locks @0/@90. Automatic omits that flag. Audio uses output capture; touches enable the control channel required by scrcpy while keyboard/mouse/gamepad and clipboard sync are disabled.
- Options restart only the affected owned process. scrcpy has 1.5 seconds to exit gracefully before bounded cleanup; its Android cleanup restores the prior touch indicator setting.
## Installation packages
Run scripts/package.ps1 using PowerShell 7 to build, test and publish a local ClickOnce setup.exe package with its complete Application Files directory, development-signed manifests and corresponding helper source. Use -SkipBuild only after a successful Release build. CI uploads the installation ZIP and SHA-256 checksum. Never commit private signing keys. Local signing identity persists in artifacts; clean CI uses an ephemeral development identity, not a production publisher.
## Single-file installer

The installer project targets .NET Framework 4.6.2, allowing its UI to start before .NET 4.8 prerequisites are installed. The Office add-in remains .NET 4.8. The package script builds a Microsoft prerequisite-only bootstrapper (no ApplicationFile) and embeds the complete published payload/source in the launcher. No Inno Setup, third-party installer compiler or PowerShell installation is required on recipients' PCs.

The launcher runs as the interactive user. It never imports a trust certificate, alters Office security, kills PowerPoint or modifies settings.xml. It only removes the exact registered AndroidLive component through VSTOInstaller. Local source URI validation rejects other product names and network locations. It retains stage/backup/failed directories under the product appdata root for diagnostics; it does not recursively delete user directories. Do not run it from a packaged development host to validate real desktop registration; the user should launch the EXE normally.

The package script runs the installer tests and executes only --verify-package against a unique short temporary directory. This path never installs software or accesses Office registration. Real installation/migration and trust prompts require manual acceptance.

Production distribution needs a stable publisher identity: sign both Office manifests and the outer EXE with an approved certificate/service and a suitable timestamp. The current development key is local-only; clean CI uses an ephemeral key. Do not commit or distribute private keys. Public signing and IT deployment are not provisioned automatically.

References: [Microsoft VSTO deployment](https://learn.microsoft.com/en-us/visualstudio/vsto/deploying-an-office-solution-by-using-clickonce) and [GenerateBootstrapper task](https://learn.microsoft.com/en-us/visualstudio/msbuild/generatebootstrapper-task).
## Managed deployment (0.0.108)

The installer accepts explicit --install --silent / --uninstall --silent, --detect and optional --log. All managed paths return exit codes without dialogs. Silent prerequisite checks do not run Microsoft's interactive bootstrapper; IT supplies dependencies first. Managed publisher trust is checked against Trusted Publishers plus a valid certificate chain before uninstalling an older version. No certificates are provisioned by the installer.

The neutral project lives under src/AndroidLive and src/AndroidLive.Core. LegacyCompatibility.cs is the only production source of former identifiers. Preserve it for existing settings/slide migration; do not use it for new branding.

See docs/IT-DEPLOYMENT.md for the supported user-context rollout. Public production signing and real management-system acceptance remain required.