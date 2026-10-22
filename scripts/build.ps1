#requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug', [switch]$SkipTests)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build-wifi-helper.ps1') -RunTests:(!$SkipTests)
$repo = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path $vswhere)) { throw 'Install Visual Studio with the Office/SharePoint development workload.' }
$instances = & $vswhere -all -products * -format json | ConvertFrom-Json
$instance = $instances | Where-Object { Test-Path (Join-Path $_.installationPath ('MSBuild\Microsoft\VisualStudio\v' + $_.installationVersion.Split('.')[0] + '.0\OfficeTools\Microsoft.VisualStudio.Tools.Office.targets')) } | Select-Object -First 1
$vs = $instance.installationPath
if (!$vs) { throw 'Visual Studio Office/SharePoint development tools are required.' }
$msbuild = Join-Path $vs 'MSBuild\Current\Bin\MSBuild.exe'
$version = $instance.installationVersion.Split('.')[0] + '.0'
$artifacts = Join-Path $repo 'artifacts'
New-Item -ItemType Directory -Force $artifacts | Out-Null
$key = Join-Path $artifacts 'development.pfx'
if (!(Test-Path $key)) {
    $rsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=HJVTAK Android Live Development Only', $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $oids = [Security.Cryptography.OidCollection]::new()
        [void]$oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.3'))
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids,$false))
        $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5),[DateTimeOffset]::UtcNow.AddYears(1))
        try { [IO.File]::WriteAllBytes($key,$certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx,'')) }
        finally { $certificate.Dispose() }
    } finally { $rsa.Dispose() }
}
$signingCertificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($key, '', [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet -bor [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet)
$store = [Security.Cryptography.X509Certificates.X509Store]::new('My','CurrentUser')
try { $store.Open('ReadWrite'); $store.Add($signingCertificate) } finally { $store.Dispose() }
$thumbprint = $signingCertificate.Thumbprint
$signingCertificate.Dispose()
"<Project><PropertyGroup><ManifestCertificateThumbprint>$thumbprint</ManifestCertificateThumbprint></PropertyGroup></Project>" | Set-Content (Join-Path $artifacts 'Signing.props')
# VSTO requires a manifest signing certificate. This disposable local key is never committed.
# VSTO tooling may import it into CurrentUser\My and add a local inclusion-list entry.
# The project overrides automatic Office registration; build never enables the add-in in PowerPoint.
& $msbuild (Join-Path $repo 'src\HjvTak.LiveAndroid\HjvTak.LiveAndroid.csproj') /t:Build "/p:Configuration=$Configuration" "/p:VisualStudioVersion=$version" "/p:ManifestKeyFile=$key" "/p:ManifestCertificateThumbprint=$thumbprint" /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'VSTO build failed.' }
if (!$SkipTests) {
    & $msbuild (Join-Path $repo 'tests\HjvTak.LiveAndroid.Tests\HjvTak.LiveAndroid.Tests.csproj') /t:Build "/p:Configuration=$Configuration" /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $repo "tests\HjvTak.LiveAndroid.Tests\bin\$Configuration\HjvTak.LiveAndroid.Tests.exe")
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
Write-Host "Build completed: src\HjvTak.LiveAndroid\bin\$Configuration\HjvTak.LiveAndroid.vsto"
