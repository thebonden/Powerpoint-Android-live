#requires -Version 5.1
$ErrorActionPreference='Stop'
$installer=Join-Path $PSScriptRoot 'Android-Live-0.0.110-Setup.exe'
if(!(Test-Path -LiteralPath $installer)){throw 'Installer EXE is missing from the IT package.'}
$process=Start-Process -FilePath $installer -ArgumentList '--install','--silent' -WindowStyle Hidden -PassThru -Wait
exit $process.ExitCode