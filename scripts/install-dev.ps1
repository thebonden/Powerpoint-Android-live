#requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$manifest=(Resolve-Path (Join-Path $repo "src\AndroidLive\bin\$Configuration\AndroidLive.vsto")).Path
$installer=Join-Path $env:CommonProgramFiles 'Microsoft Shared\VSTO\10.0\VSTOInstaller.exe'
if(!(Test-Path $installer)){throw 'Microsoft Visual Studio Tools for Office Runtime is required.'}
if(!$PSCmdlet.ShouldProcess($manifest,'Open the Microsoft VSTO installation dialog through the Windows desktop')){return}
if(Get-Process POWERPNT -ErrorAction SilentlyContinue){throw 'Save your work and close all PowerPoint windows before installing.'}
# Shell.Application.ShellExecute alone can inherit the caller context. Obtain
# the running Explorer desktop's Application to install into the registry view
# used by normal Office launches, not a packaged development host's private view.
$shell=$null; $windows=$null; $desktop=$null; $document=$null; $application=$null
try {
    $shell=New-Object -ComObject Shell.Application
    $windows=$shell.Windows()
    $desktopHwnd=0
    $desktop=$windows.FindWindowSW(0,0,8,[ref]$desktopHwnd,1)
    if(!$desktop){throw 'No interactive Windows desktop was found. Run installation from a normal signed-in desktop.'}
    $document=$desktop.Document
    $application=$document.Application
    $application.ShellExecute($installer,('/Install "'+$manifest+'"'),(Split-Path $manifest),'open',1)
} finally {
    foreach($comObject in @($application,$document,$desktop,$windows,$shell)){
        if($null -ne $comObject -and [Runtime.InteropServices.Marshal]::IsComObject($comObject)){
            [void][Runtime.InteropServices.Marshal]::ReleaseComObject($comObject)
        }
    }
}
Write-Host 'Microsoft installation dialog opened. This does not confirm installation succeeded.'
Write-Host 'Review/complete the prompt, then start PowerPoint normally from Start or the taskbar and check Android Live.'
Write-Host 'No certificate is added to Trusted Root or Trusted Publishers and no Office security setting is changed.'
