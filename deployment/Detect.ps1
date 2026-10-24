#requires -Version 5.1
# Intune custom detection: run in USER context, 64-bit PowerShell.
$ErrorActionPreference='Stop'
try {
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent()
 if($identity.IsSystem){exit 1}
 $expected=Join-Path $env:LOCALAPPDATA 'AndroidLive\App\AndroidLive.vsto'
 $found=$false
 foreach($view in @([Microsoft.Win32.RegistryView]::Registry64,[Microsoft.Win32.RegistryView]::Registry32)){
  $root=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,$view)
  $key=$null
  try{
   $key=$root.OpenSubKey('Software\Microsoft\Office\PowerPoint\Addins\AndroidLive')
   if($key){
    $value=[string]$key.GetValue('Manifest')
    if($value){
     $uri=[uri]($value.Replace('|vstolocal',''))
     if($uri.IsFile -and !$uri.IsUnc -and $uri.LocalPath -eq $expected){$found=$true}
    }
   }
  }finally{if($key){$key.Dispose()};$root.Dispose()}
 }
 if(!$found -or !(Test-Path -LiteralPath $expected)){exit 1}
 $settings=[Xml.XmlReaderSettings]::new()
 $settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit
 $settings.XmlResolver=$null
 $settings.MaxCharactersInDocument=2097152
 $reader=[Xml.XmlReader]::Create($expected,$settings)
 try{$document=[Xml.XmlDocument]::new();$document.XmlResolver=$null;$document.Load($reader)}finally{$reader.Dispose()}
 $node=$document.SelectSingleNode('/*[local-name()="assembly"]/*[local-name()="assemblyIdentity"]')
 if(!$node -or [version]$node.version -lt [version]'0.0.105.0'){exit 1}
 Write-Output ('Android Live '+$node.version)
 exit 0
}catch{exit 1}