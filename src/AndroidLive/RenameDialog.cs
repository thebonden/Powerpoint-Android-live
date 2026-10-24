using System;
using System.Drawing;
using System.Windows.Forms;

namespace AndroidLive
{
    internal sealed class RenameDialog : Form
    {
        internal RenameDialog(UserSettings settings)
        {
            Text="Omdøb enhed";ClientSize=new Size(460,180);
            AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterScreen;MinimizeBox=false;MaximizeBox=false;
            var name=new TextBox{Left=16,Top=45,Width=425,MaxLength=60,Text=settings.Selected.Name};
            var save=new Button{Left=245,Top=130,Width=90,Text="Gem"};
            var cancel=new Button{Left=345,Top=130,Width=95,Text="Annuller",DialogResult=DialogResult.Cancel};
            save.Click+=(s,e)=>{
                if(string.IsNullOrWhiteSpace(name.Text)){MessageBox.Show(this,"Skriv et navn til enheden.");return;}
                string previous=settings.Selected.Name;
                try{settings.Selected.Name=name.Text.Trim();settings.Save();DialogResult=DialogResult.OK;Close();}
                catch(Exception ex){settings.Selected.Name=previous;MessageBox.Show(this,ex.Message);}
            };
            Controls.AddRange(new Control[]{
                new Label{Left=16,Top=16,Width=425,Text="Navn i PowerPoint"},name,
                new Label{Left=16,Top=80,Width=425,Height=40,Text="Navnet huskes ved genforbindelse. Enhedens eget navn og dine slide-tilknytninger ændres ikke."},save,cancel});
            AcceptButton=save;CancelButton=cancel;
        }
    }
}