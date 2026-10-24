#requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug', [switch]$Unregister)
$ErrorActionPreference = 'Stop'
if (Get-Process POWERPNT -ErrorAction SilentlyContinue) { throw 'Close PowerPoint before changing add-in registration.' }
$registryPath = 'Software\Microsoft\Office\PowerPoint\Addins\AndroidLive'
if ($Unregister) {
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKey($registryPath, $false)
    Write-Host 'Removed this add-in registration. Files and settings are retained.'
    return
}
$repo = Split-Path $PSScriptRoot -Parent
$manifest = (Resolve-Path (Join-Path $repo "src\AndroidLive\bin\$Configuration\AndroidLive.vsto")).Path
$uri = ([uri]$manifest).AbsoluteUri + '|vstolocal'
$key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($registryPath)
try {
    $old = $key.GetValue('Manifest')
    if ($old -and $old -ne $uri) { throw 'Another copy is registered. Unregister that copy before registering this one.' }
    $key.SetValue('FriendlyName','Android Live 0.0.108')
    $key.SetValue('Description','Android live window overlay; not embedded video')
    $key.SetValue('Manifest',$uri)
    $key.SetValue('LoadBehavior',3,[Microsoft.Win32.RegistryValueKind]::DWord)
} finally { $key.Dispose() }
Write-Host 'Registered the local development build in this process registry view.'
Write-Host 'For ordinary desktop use, prefer install-dev.ps1 and verify by launching PowerPoint normally. A packaged host may have a private registry view.'
