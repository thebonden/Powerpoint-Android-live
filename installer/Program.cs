using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HjvTak.Setup
{
    internal static class Program
    {
        [STAThread] private static int Main(string[] args)
        {
            // Read-only package validation: never touches Office registration or launches installers.
            if(args.Length==2 && args[0]=="--verify-package") {
                try {
                    if(Directory.Exists(args[1]))throw new IOException("Verification directory must be new.");
                    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                        PackageFiles.Extract(stream,args[1],BuildInfo.PayloadHash);
                    File.WriteAllText(Path.Combine(args[1],"verified.txt"),BuildInfo.Version);
                    return 0;
                }catch{return 1;}
            }
            using(var mutex=new Mutex(false,@"Local\HjvTak.AndroidLive.Setup")) {
                if(!mutex.WaitOne(0)){MessageBox.Show("Installationsprogrammet er allerede åbent.");return 1;}
                try {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new SetupWindow());
                }finally{mutex.ReleaseMutex();}
            }
            return 0;
        }
    }
    internal sealed class SetupWindow : Form
    {
        private readonly Button install,close;
        private readonly TextBox status;
        private bool busy;
        internal SetupWindow()
        {
            Text="Android Live · Installation "+BuildInfo.Version;ClientSize=new Size(610,390);
            AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;
            var title=new Label{Left=24,Top=22,Width=560,Height=32,Text="Android Live til PowerPoint",Font=new Font(SystemFonts.MessageBoxFont.FontFamily,17,FontStyle.Bold)};
            var description=new Label{Left=24,Top=66,Width=560,Height=100,Text=
                "Gem dit arbejde, og luk PowerPoint. Klik derefter Installer / opdater.\n\n"+
                "Programmet installeres for din Windows-bruger. Tidligere udgaver opdateres, og dine tabletindstillinger og præsentationer bevares."};
            status=new TextBox{Left=24,Top=175,Width=560,Height=125,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text="Klar. Microsoft kan bede dig godkende installationen."};
            install=new Button{Left=320,Top=328,Width=160,Height=36,Text="Installer / opdater"};
            close=new Button{Left=490,Top=328,Width=95,Height=36,Text="Luk"};
            close.Click+=(s,e)=>Close();
            install.Click+=async(s,e)=>{
                busy=true;install.Enabled=false;close.Enabled=false;status.Clear();
                try{await Task.Run(()=>new Installation(Log).Run());Log("Færdig! Start PowerPoint normalt, og åbn fanen Android Live.");}
                catch(Exception ex){Log("Installationen blev ikke fuldført.\r\n"+ex.Message);install.Enabled=true;}
                finally{busy=false;close.Enabled=true;}
            };
            FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show(this,"Afslut eller annuller først det åbne Microsoft-installationsvindue.");}};
            Controls.AddRange(new Control[]{title,description,status,install,close});
        }
        private void Log(string value)
        {
            if(InvokeRequired){BeginInvoke(new Action<string>(Log),value);return;}
            status.AppendText(value+Environment.NewLine);
        }
    }
    internal sealed class Installation
    {
        private const string AddinKey=@"Software\Microsoft\Office\PowerPoint\Addins\HjvTak.LiveAndroid";
        private readonly Action<string> log;
        internal Installation(Action<string> progress){log=progress;}
        private static bool PowerPointOpen()
        {
            int session=Process.GetCurrentProcess().SessionId;
            foreach(var process in Process.GetProcessesByName("POWERPNT"))
                using(process){try{if(process.SessionId==session)return true;}catch(InvalidOperationException){}}
            return false;
        }
        private static string RegistryValue(RegistryHive hive,RegistryView view,string key,string value)
        {
            using(var root=RegistryKey.OpenBaseKey(hive,view))
            using(var item=root.OpenSubKey(key))return Convert.ToString(item?.GetValue(value))??"";
        }
        private static string VstoInstaller()
        {
            foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}) {
                string candidate=RegistryValue(RegistryHive.LocalMachine,view,@"SOFTWARE\Microsoft\VSTO Runtime Setup\v4","InstallerPath");
                if(File.Exists(candidate))return candidate;
            }
            foreach(string folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86)}) {
                string candidate=Path.Combine(folder,@"Microsoft Shared\VSTO\10.0\VSTOInstaller.exe");
                if(File.Exists(candidate))return candidate;
            }
            return null;
        }
        private static bool FrameworkReady()
        {
            foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}) {
                int release;
                if(int.TryParse(RegistryValue(RegistryHive.LocalMachine,view,@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full","Release"),out release)&&release>=528040)return true;
            }
            return false;
        }
        private static bool PowerPointInstalled()
        {
            foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
            foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32}) {
                string value=RegistryValue(hive,view,@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\POWERPNT.EXE","");
                if(File.Exists(value.Trim('"')))return true;
            }
            return false;
        }
        internal static string LocalManifest(string value)
        {
            value=(value??"").Replace("|vstolocal","");
            Uri uri;
            if(string.IsNullOrEmpty(value))return null;
            if(!Uri.TryCreate(value,UriKind.Absolute,out uri)||!uri.IsFile||uri.IsUnc ||
                !Path.GetFileName(uri.LocalPath).Equals(PackageFiles.Manifest,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Den tidligere installation bruger en ukendt placering. Fjern HJVTAK Android Live via Installerede apps, og prøv igen.");
            return uri.LocalPath;
        }
        private static string PreviousManifest()
        {
            var values=new[]{RegistryView.Registry64,RegistryView.Registry32}.Select(view=>
                LocalManifest(RegistryValue(RegistryHive.CurrentUser,view,AddinKey,"Manifest")))
                .Where(value=>value!=null).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(values.Length>1)throw new InvalidOperationException("Der er registreret flere forskellige Android Live-installationer. Fjern dem via Installerede apps først.");
            return values.FirstOrDefault();
        }
        private static int Execute(string executable,string arguments)
        {
            using(var process=Process.Start(new ProcessStartInfo(executable,arguments){
                UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(executable)})) {
                if(process==null)throw new InvalidOperationException("Microsofts installationsprogram kunne ikke startes.");
                process.WaitForExit();return process.ExitCode;
            }
        }
        private static string Quote(string path)
        {
            if(path.Contains('"')||path.Contains('\r')||path.Contains('\n'))throw new ArgumentException("Ugyldig sti.");
            return "\""+path+"\"";
        }
        internal void Run()
        {
            if(!Environment.Is64BitOperatingSystem)throw new InvalidOperationException("Android Live kræver Windows 10/11, 64-bit.");
            if(PowerPointOpen())throw new InvalidOperationException("Gem dit arbejde, luk alle PowerPoint-vinduer, og prøv igen.");
            if(!PowerPointInstalled())throw new InvalidOperationException("Microsoft PowerPoint til Windows blev ikke fundet. Installer desktop-udgaven først.");
            string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HjvTak","AndroidLive");
            string current=Path.Combine(root,"App");
            string suffix=Guid.NewGuid().ToString("N").Substring(0,8);
            string stage=Path.Combine(root,"stage-"+suffix),backup=Path.Combine(root,"backup-"+suffix),failed=Path.Combine(root,"failed-"+suffix);
            // Never use an existing stage/backup, and refuse junctions in the application directories.
            foreach(string path in new[]{root,current})
                if(Directory.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                    throw new IOException("Installationsmappen må ikke være en mappehenvisning.");
            if(Directory.Exists(stage)||Directory.Exists(backup)||Directory.Exists(failed))throw new IOException("Prøv at starte installationsprogrammet igen.");
            log("Kontrollerer og pakker installationsfilerne ud…");
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
                PackageFiles.Extract(stream,stage,BuildInfo.PayloadHash);
            if(!FrameworkReady()||VstoInstaller()==null) {
                log("Microsoft kontrollerer de nødvendige komponenter. Følg det nye vindue.");
                int prerequisites=Execute(Path.Combine(stage,"prerequisites.exe"),"");
                if(prerequisites==3010||prerequisites==1641)throw new InvalidOperationException("Genstart Windows, og kør denne installationsfil igen.");
                if(prerequisites!=0||!FrameworkReady()||VstoInstaller()==null)
                    throw new InvalidOperationException("Microsoft-komponenterne blev ikke installeret. Prøv igen, eller kontakt din IT-afdeling.");
            }
            if(PowerPointOpen())throw new InvalidOperationException("PowerPoint blev åbnet. Luk det, og prøv igen.");
            string installer=VstoInstaller();
            string old=PreviousManifest();
            bool removed=false,moved=false,newInstalled=false;
            try {
                if(old!=null) {
                    if(!File.Exists(old))throw new InvalidOperationException("Den tidligere installationsmappe mangler. Fjern HJVTAK Android Live via Installerede apps, og prøv igen.");
                    log("Opdaterer den tidligere Office-tilføjelse. Tabletindstillingerne bevares…");
                    int code=Execute(installer,"/Uninstall "+Quote(old)+" /Silent");
                    if(code!=0)throw new InvalidOperationException("Den tidligere Office-tilføjelse kunne ikke fjernes (kode "+code+"). Intet nyt er installeret.");
                    removed=true;
                }
                PackageFiles.MoveIntoPlace(stage,current,backup);moved=true;
                log("Installerer Android Live "+BuildInfo.Version+". Følg Microsofts installationsvindue.");
                int installed=Execute(installer,"/Install "+Quote(Path.Combine(current,PackageFiles.Manifest)));
                if(installed!=0)throw new InvalidOperationException("Microsoft afsluttede ikke installationen (kode "+installed+").");
                newInstalled=true;
                string registered=PreviousManifest();
                if(!string.Equals(registered,Path.Combine(current,PackageFiles.Manifest),StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Office-registreringen kunne ikke bekræftes.");
                log("Installationen er bekræftet. Ingen præsentationer eller tabletindstillinger er ændret.");
                // Backups are deliberately retained for recovery; never recursively delete user directories.
            }
            catch(Exception error) {
                if(newInstalled) {
                    int undo=Execute(installer,"/Uninstall "+Quote(Path.Combine(current,PackageFiles.Manifest))+" /Silent");
                    if(undo!=0)throw new InvalidOperationException(error.Message+" Den nye Office-registrering kunne ikke fjernes. Behold installationsfilerne og kontakt support.");
                }
                if(moved) {
                    try{PackageFiles.Restore(current,backup,failed);}
                    catch(Exception restore){throw new InvalidOperationException(error.Message+" Filerne kunne ikke gendannes: "+restore.Message);}
                }
                if(removed&&old!=null&&File.Exists(old)) {
                    try {
                        int rollback=Execute(installer,"/Install "+Quote(old)+" /Silent");
                        log(rollback==0?"Den tidligere udgave er gendannet.":"Den tidligere udgave kunne ikke geninstalleres automatisk. Kør dens installationsfil igen.");
                    }catch{log("Den tidligere udgave skal geninstalleres fra dens installationsfil.");}
                }
                throw;
            }
        }
    }
}