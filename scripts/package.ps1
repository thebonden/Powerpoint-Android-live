#requires -Version 7.0
[CmdletBinding()]
param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(!$SkipBuild){ & (Join-Path $PSScriptRoot 'build.ps1') -Configuration Release }
[xml]$project=Get-Content (Join-Path $repo 'src/HjvTak.LiveAndroid/HjvTak.LiveAndroid.csproj')
$version=($project.Project.PropertyGroup.FriendlyName | Where-Object { $_ }) -replace '^HJVTAK Android Live ',''
if($version -notmatch '^0\.0\.\d{3}$'){throw 'Expected a 0.0.1xx development version.'}
$vswhere=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio/Installer/vswhere.exe'
$instances=& $vswhere -all -products * -format json | ConvertFrom-Json
$instance=$instances | Where-Object { Test-Path (Join-Path $_.installationPath ('MSBuild/Microsoft/VisualStudio/v'+$_.installationVersion.Split('.')[0]+'.0/OfficeTools/Microsoft.VisualStudio.Tools.Office.targets')) } | Select-Object -First 1
if(!$instance){throw 'Visual Studio Office tools are required.'}
$stage=Join-Path $repo ('artifacts/package-'+[Guid]::NewGuid().ToString('N'))
$publish=Join-Path $stage "Android-Live-$version"
New-Item -ItemType Directory -Path $publish -Force | Out-Null
$msbuild=Join-Path $instance.installationPath 'MSBuild/Current/Bin/MSBuild.exe'
& $msbuild (Join-Path $repo 'src/HjvTak.LiveAndroid/HjvTak.LiveAndroid.csproj') /t:Publish /p:Configuration=Release "/p:VisualStudioVersion=$($instance.installationVersion.Split('.')[0]).0" "/p:PublishDir=$publish\" "/p:PublishUrl=$publish\" /p:BootstrapperEnabled=true /p:IsWebBootstrapper=false /p:TargetCulture=en /p:FallbackCulture=en "/warnaserror:MSB3155,MSB3696" /v:minimal /nologo
if($LASTEXITCODE -ne 0){throw 'Publishing the installer failed.'}
foreach($required in @('setup.exe','HjvTak.LiveAndroid.vsto','Application Files')){
    if(!(Test-Path (Join-Path $publish $required))){throw "Installer is incomplete: $required"}
}
Copy-Item (Join-Path $repo 'docs/INSTALLATION.md') (Join-Path $publish 'LAES-MIG.md')
$source=Join-Path $publish 'source'
New-Item -ItemType Directory -Path $source | Out-Null
Copy-Item (Join-Path $repo 'native') $source -Recurse
New-Item -ItemType Directory -Path (Join-Path $source 'scripts') | Out-Null
Copy-Item (Join-Path $repo 'scripts/build-wifi-helper.ps1') (Join-Path $source 'scripts')
$destination=Join-Path $repo "artifacts/Android-Live-$version-installation.zip"
Compress-Archive -Path $publish -DestinationPath $destination -Force
(Get-FileHash -Algorithm SHA256 $destination).Hash | Set-Content ($destination+'.sha256')
Write-Host "Installation package: $destination"
Write-Host 'Extract the whole package to a permanent folder and run setup.exe from Explorer.'
Write-Host 'Development signing: users must review any Microsoft trust prompt themselves.'
# Make a prerequisite-only Microsoft bootstrapper: it must not register the add-in from staging.
$prerequisites=Join-Path $stage 'prerequisites'
& $msbuild (Join-Path $repo 'installer/Prerequisites.proj') "/p:OutputPath=$prerequisites" "/p:VisualStudioVersion=$($instance.installationVersion.Split('.')[0]).0" "/warnaserror:MSB3155,MSB3696" /v:minimal /nologo
if($LASTEXITCODE -ne 0){throw 'Prerequisite bootstrapper failed.'}
Copy-Item (Join-Path $prerequisites 'setup.exe') (Join-Path $publish 'prerequisites.exe')
$payload=Join-Path $stage 'payload.zip'
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $payload
$payloadHash=(Get-FileHash -Algorithm SHA256 $payload).Hash
$generated=Join-Path $stage 'BuildInfo.cs'
@"
using System.Reflection;
[assembly: AssemblyVersion("$version.0")]
[assembly: AssemblyFileVersion("$version.0")]
[assembly: AssemblyProduct("HJVTAK Android Live Installer")]
[assembly: AssemblyTitle("Android Live - Installer")]
namespace HjvTak.Setup { internal static class BuildInfo {
internal const string Version="$version";
internal const string PayloadHash="$payloadHash";
} }
"@ | Set-Content -Encoding utf8 $generated
& $msbuild (Join-Path $repo 'installer/InstallerTests.csproj') /t:Build "/p:GeneratedSource=$generated" /v:minimal /nologo
if($LASTEXITCODE -ne 0){throw 'Installer test compilation failed.'}
& (Join-Path $repo 'installer/bin/Tests/Android-Live-InstallerTests.exe')
if($LASTEXITCODE -ne 0){throw 'Installer tests failed.'}
& $msbuild (Join-Path $repo 'installer/Installer.csproj') /t:Build "/p:GeneratedSource=$generated" "/p:PayloadPath=$payload" /v:minimal /nologo
if($LASTEXITCODE -ne 0){throw 'Single-file installer compilation failed.'}
$singleFile=Join-Path $repo "artifacts/Android-Live-$version-Setup.exe"
Copy-Item (Join-Path $repo 'installer/bin/Release/Android-Live-Setup.exe') $singleFile -Force
# Verify the actual distributable without changing Office or installing prerequisites.
$verify=Join-Path ([IO.Path]::GetTempPath()) ('AndroidLiveVerify-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
$process=Start-Process -FilePath $singleFile -ArgumentList @('--verify-package',('"'+$verify+'"')) -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0 -or !(Test-Path (Join-Path $verify 'verified.txt'))){throw 'Single-file package verification failed.'}
(Get-FileHash -Algorithm SHA256 $singleFile).Hash | Set-Content ($singleFile+'.sha256')
Write-Host "Single-file installer: $singleFile"