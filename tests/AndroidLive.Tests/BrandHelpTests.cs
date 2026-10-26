using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AndroidLive;
using AndroidLive.Core;

internal static class BrandHelpTests
{
    private static readonly string[] Kinds={"brand","play","all","stop","remove","rename","frame","eye","audio","touch","settings","help","status","qr"};
    internal static void Run(Action<string,Action> test)
    {
        test("brand assets render at ribbon sizes and preserve transparent background",()=>{
            foreach(var kind in Kinds)foreach(int size in new[]{16,32,64})using(var bitmap=BrandArtwork.Icon(kind,size)){
                Check(bitmap.GetPixel(0,0).A==0);
                int ink=0;for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(bitmap.GetPixel(x,y).A>0)ink++;
                Check(ink>8 && ink<size*size);
            }
        });
        test("ribbon images convert to Office pictures and remain reusable",()=>{
            using(var images=new RibbonImages())foreach(string id in new[]{"Brand","AddTablet","StartAndroid","StartAll","StopAndroid","RenameDevice","RemoveDevice","AddMarker","Preview","Audio","Touches","Settings","Status","Help"}){
                var picture=images.Get(id);Check(picture.Width>0&&picture.Height>0);
                Check(ReferenceEquals(picture,images.Get(id)));
            }
        });
        test("frame artwork preserves aspect and leaves caption area clear",()=>{
            foreach(var size in new[]{new Size(600,360),new Size(300,600),new Size(900,200)})
            using(var bitmap=BrandArtwork.Frame(size.Width,size.Height)){
                Check(Math.Abs((double)bitmap.Width/bitmap.Height-(double)size.Width/size.Height)<.03);
                for(int y=(int)(bitmap.Height*.44);y<bitmap.Height;y++)
                    for(int x=0;x<bitmap.Width;x++)Check(bitmap.GetPixel(x,y).ToArgb()==BrandArtwork.Background.ToArgb());
            }
        });
        test("help local checks distinguish missing tools, no tablet, stopped and live states",()=>{
            string empty=HelpContent.LocalChecks(false,null,false,DeviceState.Stopped);
            Check(empty.Contains("Ingen tablet")&&empty.Contains("mangler")&&empty.Contains("slået fra")&&empty.Contains("netværket er ikke testet"));
            var profile=new DeviceProfile{AndroidName="TABLET001",Name="Instruktør",NameMode=DeviceNameMode.Custom,ReconnectOnStartup=false};
            string stopped=HelpContent.LocalChecks(true,profile,true,DeviceState.Stopped);
            Check(stopped.Contains(profile.ListLabel)&&stopped.Contains("Forbind valgt")&&stopped.Contains("Videostatus: Stoppet"));
            profile.ReconnectOnStartup=true;
            Check(HelpContent.LocalChecks(true,profile,true,DeviceState.Running).Contains("Videostatus: Kører"));
        });
        test("help UI search recovers from no results and support status is read only",()=>{
            using(var dialog=new HelpDialog("test",()=>"LOCAL STATUS")){
                var controls=Descendants(dialog).ToArray();
                var search=controls.OfType<TextBox>().Single(c=>c.AccessibleName=="Søg i hjælpen");
                var topics=controls.OfType<ListBox>().Single();
                var article=controls.OfType<TextBox>().Single(c=>c.AccessibleName=="Vejledning");
                var status=controls.OfType<TextBox>().Single(c=>c.AccessibleName=="Status og detaljer");
                Check(status.ReadOnly&&status.Text=="LOCAL STATUS"&&article.ReadOnly);
                search.Text="no_such_topic_928";
                Check(topics.Items.Count==0&&article.Text.Contains("Ingen emner"));
                search.Text="FORBUNDET";
                Check(topics.Items.Count>0&&article.Text.Length>100);
                search.Text="";
                Check(topics.Items.Count==HelpContent.Topics.Length);
            }
        });
        test("offline help survives unavailable status provider",()=>{
            using(var dialog=new HelpDialog("test",()=>{throw new InvalidOperationException("unavailable");},true)){
                var controls=Descendants(dialog).ToArray();
                Check(controls.OfType<TextBox>().Single(c=>c.AccessibleName=="Status og detaljer").Text.Contains("kunne ikke læses"));
                Check(controls.OfType<ListBox>().Single().Items.Count==HelpContent.Topics.Length);
            }
        });
    }
    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {foreach(Control child in root.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}}
    internal static int RenderSamples(string folder)
    {
        Directory.CreateDirectory(folder);
        using(var sheet=new Bitmap(840,170))
        using(var g=Graphics.FromImage(sheet))
        using(var font=new Font("Segoe UI",9)){
            g.Clear(Color.White);
            for(int i=0;i<Kinds.Length;i++){
                using(var icon=BrandArtwork.Icon(Kinds[i],48))g.DrawImageUnscaled(icon,12+i*59,18);
                g.DrawString(Kinds[i],font,Brushes.Black,8+i*59,78);
                using(var icon=BrandArtwork.Icon(Kinds[i],16))g.DrawImageUnscaled(icon,28+i*59,112);
            }
            sheet.Save(Path.Combine(folder,"ribbon-icons.png"));
        }
        using(var frame=BrandArtwork.Frame(600,360))frame.Save(Path.Combine(folder,"frame.png"));
        using(var help=new PreviewHelpDialog()){
            help.StartPosition=FormStartPosition.Manual;help.Location=new Point(-32000,-32000);help.ShowInTaskbar=false;help.Show();Application.DoEvents();help.PerformLayout();
            using(var bitmap=new Bitmap(help.Width,help.Height)){
                help.DrawToBitmap(bitmap,new Rectangle(0,0,help.Width,help.Height));bitmap.Save(Path.Combine(folder,"help.png"));
                Descendants(help).OfType<TabControl>().Single().SelectedIndex=1;Application.DoEvents();
                help.DrawToBitmap(bitmap,new Rectangle(0,0,help.Width,help.Height));bitmap.Save(Path.Combine(folder,"help-status.png"));
            }
        }
        File.WriteAllText(Path.Combine(folder,"HELP.md"),"# Android Live · Hjælp og fejlfinding\n\n"+
            string.Join("\n\n",HelpContent.Topics.Select(t=>"## "+t.Title+"\n\n"+t.Body))+
            "\n\n## Status og detaljer\n\nÅbn Hjælp og fejlfinding → Status og detaljer. Opdater status for at læse lokale kontroller og seneste fejl. Kopiér kun ved behov; gennemse enhedsnavne og netværksadresser, før du deler oplysninger med support.\n\n"+
            "Vejledningen til trådløs parring bygger på [Androids officielle ADB-guide](https://developer.android.com/tools/adb#wireless). Enhedens menuer kan variere.\n");
        return 0;
    }
    private sealed class PreviewHelpDialog : HelpDialog
    {
        internal PreviewHelpDialog():base("0.0.113",()=> "Eksempelstatus – ingen netværkskontrol udført.",false){}
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get {var p=base.CreateParams;p.ExStyle|=0x08000000;return p;} }
    }
    private static void Check(bool value){if(!value)throw new Exception("Assertion failed");}
}