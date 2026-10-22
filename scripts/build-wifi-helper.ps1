#requires -Version 7.0
[CmdletBinding()]
param([switch]$RunTests)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'native\WifiQr'
$build=Join-Path $repo 'artifacts\wifi-qr'
$lock=Get-Content (Join-Path $source 'dependencies.lock.json') -Raw | ConvertFrom-Json
foreach($entry in $lock.qrcodegen.files){
 if((Get-FileHash (Join-Path $source $entry.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.sha256){throw "QR dependency checksum mismatch: $($entry.path)"}
}
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vs=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$vs){throw 'Install Visual Studio Desktop development with C++ and a Windows SDK to build the QR helper.'}
$vc=(Get-ChildItem (Join-Path $vs 'VC\Tools\MSVC') -Directory | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1).FullName
$sdk=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots').KitsRoot10
$sdkVersion=(Get-ChildItem (Join-Path $sdk 'Include') -Directory | Where-Object {Test-Path (Join-Path $_.FullName 'um\Windows.h')} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1).Name
$compiler=Join-Path $vc 'bin\Hostx64\x64\cl.exe'
$includes=@("$vc\include","$sdk\Include\$sdkVersion\ucrt","$sdk\Include\$sdkVersion\um","$sdk\Include\$sdkVersion\shared","$source\src")
$common=@('/nologo','/std:c++17','/EHsc','/MT','/O2','/W4','/wd4201','/utf-8','/DWIN32_LEAN_AND_MEAN','/DNOMINMAX')+@($includes|ForEach-Object {"/I$_"})
$link=@('/link',"/LIBPATH:$vc\lib\x64","/LIBPATH:$sdk\Lib\$sdkVersion\ucrt\x64","/LIBPATH:$sdk\Lib\$sdkVersion\um\x64",'user32.lib','gdi32.lib','shell32.lib','bcrypt.lib','ws2_32.lib','/Brepro')
New-Item -ItemType Directory -Force $build | Out-Null
Push-Location $build
try {
 & $compiler @common "$source\src\qr-main.cpp" "$source\src\windows.cpp" "$source\vendor\qrcodegen\qrcodegen.cpp" "/Fe:$build\Android-WiFi-QR.exe" @link /SUBSYSTEM:WINDOWS
 if($LASTEXITCODE -ne 0){throw 'QR helper build failed.'}
 if($RunTests){
  & $compiler @common "$source\tests\qr-tests.cpp" "$source\vendor\qrcodegen\qrcodegen.cpp" "/Fe:$build\qr-tests.exe" @link
  if($LASTEXITCODE -ne 0){throw 'QR test build failed.'}
  & "$build\qr-tests.exe" "$build\qr-fixture.pgm"
  if($LASTEXITCODE -ne 0){throw 'QR tests failed.'}
 }
} finally { Pop-Location }
