using System;
using System.Drawing;
using System.Windows.Forms;
using AndroidLive.Core;

namespace AndroidLive
{
    internal sealed class SettingsDialog : Form
    {
        internal SettingsDialog(UserSettings settings)
        {
            Text="Android Live · Indstillinger";ClientSize=new Size(600,290);
            AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterScreen;MinimizeBox=false;MaximizeBox=false;
            var path=new TextBox{Left=16,Top=40,Width=470,Text=settings.ScrcpyPath};
            var name=new TextBox{Left=16,Top=115,Width=470,Text=settings.Selected?.Name??"",Enabled=settings.Selected!=null,MaxLength=60};
            var browse=new Button{Left=495,Top=38,Width=90,Text="Vælg…"};
            browse.Click+=(s,e)=>{using(var picker=new OpenFileDialog{Filter="scrcpy executable|scrcpy.exe",CheckFileExists=true})
                if(picker.ShowDialog(this)==DialogResult.OK)path.Text=picker.FileName;};
            var find=new Button{Left=16,Top=245,Width=180,Text="Find automatisk"};
            find.Click+=(s,e)=>{var candidate=new UserSettings{ScrcpyPath=path.Text};
                using(var dialog=new ScrcpySetupDialog(candidate))if(dialog.ShowDialog(this)==DialogResult.OK)path.Text=candidate.ScrcpyPath;};
            var save=new Button{Left=395,Top=245,Width=90,Text="Gem"};
            save.Click+=(s,e)=>{
                try {
                    if(!ScrcpyLocator.IsComplete(path.Text.Trim()))throw new InvalidOperationException("Vælg scrcpy.exe fra den komplette Windows-pakke.");
                    if(settings.Selected!=null && string.IsNullOrWhiteSpace(name.Text))throw new InvalidOperationException("Giv tabletten et navn.");
                    settings.ScrcpyPath=path.Text.Trim();
                    if(settings.Selected!=null)settings.Selected.Name=name.Text.Trim();
                    settings.Save();DialogResult=DialogResult.OK;Close();
                } catch(Exception ex){MessageBox.Show(this,ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            };
            var cancel=new Button{Left=495,Top=245,Width=90,Text="Annuller",DialogResult=DialogResult.Cancel};
            Controls.AddRange(new Control[]{
                new Label{Left=16,Top=16,Width=560,Text="Placering af scrcpy.exe (hele pakken skal være udpakket)"},path,browse,
                new Label{Left=16,Top=90,Width=560,Text="Navn på den valgte tablet"},name,
                new Label{Left=16,Top=155,Width=565,Height=75,Text="Lyd, fingertryk og orientering vælges i Android Live-fanen pr. tablet.\nForbind hver tablet, og indsæt dens ramme på de ønskede slides.\nFlere tablets kan vises på samme slide. Ingen OBS Studio er nødvendig."},
                find,save,cancel
            });
            AcceptButton=save;CancelButton=cancel;
        }
    }
}