using System;
using System.Drawing;
using System.Windows.Forms;
using AndroidLive.Core;

namespace AndroidLive
{
    internal sealed class RenameDialog : Form
    {
        internal RenameDialog(UserSettings settings)
        {
            var device=settings.Selected;
            device.NormalizeNameMode();
            Text="Omdøb enhed";ClientSize=new Size(460,245);
            AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterScreen;MinimizeBox=false;MaximizeBox=false;
            var automatic=new CheckBox{Left=16,Top=16,Width=425,Height=24,
                Text="Brug enhedens Android-navn",Checked=device.NameMode==DeviceNameMode.Android};
            var name=new TextBox{Left=16,Top=115,Width=425,MaxLength=60,Text=device.Name,Enabled=!automatic.Checked};
            automatic.CheckedChanged+=(s,e)=>name.Enabled=!automatic.Checked;
            var save=new Button{Left=245,Top=200,Width=90,Text="Gem"};
            var cancel=new Button{Left=345,Top=200,Width=95,Text="Annuller",DialogResult=DialogResult.Cancel};
            save.Click+=(s,e)=>{
                string previous=device.Name;
                var previousMode=device.NameMode;
                try{
                    if(automatic.Checked)device.UseAndroidName();else device.SetCustomName(name.Text);
                    settings.Save();DialogResult=DialogResult.OK;Close();
                }
                catch(Exception ex){device.Name=previous;device.NameMode=previousMode;MessageBox.Show(this,ex.Message);}
            };
            Controls.AddRange(new Control[]{
                automatic,new Label{Left=16,Top=45,Width=425,Height=40,
                    Text="Android-navn: "+(string.IsNullOrEmpty(device.AndroidName)?"Hentes ved næste forbindelse":device.AndroidName)},
                new Label{Left=16,Top=90,Width=425,Text="Dit eget navn i PowerPoint"},name,
                new Label{Left=16,Top=150,Width=425,Height=40,Text="Et eget navn huskes ved genforbindelse. Enhedens navn på Android og dine slide-tilknytninger ændres ikke."},save,cancel});
            AcceptButton=save;CancelButton=cancel;
        }
    }
}