# Android Live 0.0.106 · Central IT deployment

This bundle supports central distribution of the same per-user Office add-in through Intune Win32 apps or equivalent software management. Install and uninstall run silently. It is not a machine-wide MSI: registration and device settings belong to each Windows user. Never run this package as SYSTEM, LocalService or NetworkService; it returns 1625 instead of installing into a service profile.

## Package contents

- Android-Live-0.0.106-Setup.exe: interactive and managed installation in the same binary.
- Install.ps1 and Uninstall.ps1: optional Windows PowerShell 5.1 wrappers forwarding the process exit code.
- Detect.ps1: custom user-context detection, requiring the neutral registration, fixed manifest path and minimum version 0.0.106.0.
- This guide. The EXE embeds the matching QR-helper source and licenses.

## Prerequisites controlled by IT

- Windows 10 (1607+)/11 x64 and desktop PowerPoint, with .NET Framework 4.6.2+ to run the launcher.
- .NET Framework 4.8 and Microsoft VSTO Runtime installed as managed dependencies before this app.
- A complete official scrcpy 4.1+ package, supplied separately or already installed. The add-in can discover it.
- A valid publisher certificate and chain approved by your organization's existing certificate policy, including the Trusted Publishers store. The package does not import certificates or alter security settings. Silent installation checks trust before removing an earlier version.
- The current downloadable package is development-signed. Production signing is not configured. Evaluate in an isolated pilot with an approved signing/trust process before broad assignment. Do not broadly trust a transient development certificate.
- Close PowerPoint before installation. Open PowerPoint causes exit 1618 and leaves it running.

The silent path never launches the interactive Microsoft prerequisite bootstrapper. Missing prerequisites return 1603 with a log message. A missing/untrusted publisher returns 1625 before the old Office component is removed. This also means the development package is not a zero-configuration production rollout.

## Intune configuration

1. Use Microsoft's Win32 Content Prep Tool to wrap this folder as an .intunewin package. The EXE is the setup file.
2. Set **Install behavior: User** and target the intended user groups. Do not choose System or attempt interactive service/session injection.
3. Install command:
   Android-Live-0.0.106-Setup.exe --install --silent
4. Uninstall command:
   Android-Live-0.0.106-Setup.exe --uninstall --silent
5. Detection: upload Detect.ps1 as a custom detection script; run in 64-bit/user context. It returns exit 0 plus stdout only when this or a newer neutral version is installed.
6. Configure prerequisite dependencies, return codes and a small pilot assignment. Validate normal PowerPoint startup in the target user's desktop session.

An optional log destination can be supplied as --log "C:\path\deployment.log". Without it, logs append to %LOCALAPPDATA%\AndroidLive\Logs\deployment.log. For local diagnostics, --detect returns 0 when the packaged minimum version is installed, otherwise 1. Invalid arguments return 87 rather than opening the UI.

## Return codes

| Code | Meaning | Suggested handling |
|---|---|---|
| 0 | Installation/uninstallation confirmed | Success |
| 87 | Invalid command line | Fix package command |
| 1603 | Missing dependency or installation failure | Inspect log; fix prerequisite/Office issue |
| 1618 | PowerPoint or another installer is running | Retry after the app closes |
| 1625 | Service account or publisher trust not approved | Fix user context/signing policy |

No user prompt, forced PowerPoint closure or automatic restart is performed in silent mode. Managed prerequisite packages handle their own restart codes before this package runs.

## Upgrade, rollback and removal

The neutral build can recognize earlier development identifiers solely for migration. New registrations, filenames, namespaces and settings paths use Android Live. Existing settings are read from the old location if the new file does not exist, then saved under the new neutral path when preferences are saved. Old files are retained. Existing slide shape tags remain readable; new frames use neutral tags.

Known old Office registrations are removed through VSTOInstaller, then the new version is registered. If installation fails, old program files and registration are restored when possible and recovery failures are logged. Unknown, missing-source or conflicting old registrations stop for review. Uninstall removes only the current neutral Office registration; settings and program backups remain.

This mode has automated CLI, migration, package and file-recovery tests. It still requires real Intune/policy testing: standard user, approved publisher, missing trust/dependencies, open PowerPoint retry, prior version, cancelled/failed VSTO operation and uninstall. No tenant configuration or rollout has been performed by this project.

References: [Intune Win32 app configuration](https://learn.microsoft.com/en-us/mem/intune-service/apps/apps-win32-add) and [Microsoft VSTO deployment](https://learn.microsoft.com/en-us/visualstudio/vsto/deploying-an-office-solution-by-using-clickonce).