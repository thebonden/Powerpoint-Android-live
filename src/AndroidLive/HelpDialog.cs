using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AndroidLive
{
    internal class HelpDialog : Form
    {
        private readonly Func<string> statusProvider;
        private readonly TextBox search=new TextBox{Dock=DockStyle.Top,AccessibleName="Søg i hjælpen"};
        private readonly ListBox topics=new ListBox{Dock=DockStyle.Fill,IntegralHeight=false,HorizontalScrollbar=true,AccessibleName="Hjælpeemner"};
        private readonly TextBox article=ReadOnlyText("Vejledning");
        private readonly TextBox status=ReadOnlyText("Status og detaljer");
        private readonly Label result=new Label{AutoSize=true,Text="Detaljer kan indeholde enhedsnavne og netværksadresser. Del kun med din support."};
        internal HelpDialog(string version,Func<string> statusProvider,bool showStatus=false)
        {
            this.statusProvider=statusProvider;
            Text="Android Live · Hjælp og fejlfinding";ClientSize=new Size(940,640);MinimumSize=new Size(740,520);
            AutoScaleMode=AutoScaleMode.Dpi;StartPosition=FormStartPosition.CenterScreen;MinimizeBox=false;
            Font=new Font("Segoe UI",10);
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(12)};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,78));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
            var header=new Panel{Dock=DockStyle.Fill};
            var logo=BrandArtwork.Icon("brand",64);
            var picture=new PictureBox{Image=logo,SizeMode=PictureBoxSizeMode.CenterImage,Bounds=new Rectangle(0,0,70,70)};
            picture.Disposed+=(s,e)=>logo.Dispose();
            header.Controls.Add(picture);
            header.Controls.Add(new Label{AutoSize=true,Location=new Point(82,4),Text="Android Live",Font=new Font("Segoe UI",19,FontStyle.Bold)});
            header.Controls.Add(new Label{AutoSize=true,Location=new Point(84,43),Text="Hjælp og fejlfinding · version "+version+" · Tilgængelig uden internet"});
            var tabs=new TabControl{Dock=DockStyle.Fill};
            var guide=new TabPage("Vejledninger"){Padding=new Padding(10)};
            var guideLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};
            guideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));guideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));
            var navigation=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(0,0,10,0)};
            navigation.RowStyles.Add(new RowStyle(SizeType.Absolute,24));navigation.RowStyles.Add(new RowStyle(SizeType.Absolute,32));navigation.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            navigation.Controls.Add(new Label{Text="Søg efter et problem",AutoSize=true},0,0);
            navigation.Controls.Add(search,0,1);navigation.Controls.Add(topics,0,2);
            guideLayout.Controls.Add(navigation,0,0);guideLayout.Controls.Add(article,1,0);guide.Controls.Add(guideLayout);
            var details=new TabPage("Status og detaljer"){Padding=new Padding(10)};
            var statusLayout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
            var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=true};
            var refresh=new Button{Text="Opdater status",AutoSize=true};refresh.Click+=(s,e)=>RefreshStatus();
            var copy=new Button{Text="Kopiér til support",AutoSize=true};
            copy.Click+=(s,e)=>{
                try {Clipboard.SetText(status.Text);result.Text="Kopieret. Gennemse oplysningerne, før du deler dem med support.";}
                catch(Exception ex){MessageBox.Show(this,"Kunne ikke kopiere: "+ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Information);}
            };
            actions.Controls.Add(refresh);actions.Controls.Add(copy);actions.SetFlowBreak(copy,true);actions.Controls.Add(result);
            statusLayout.Controls.Add(status,0,0);statusLayout.Controls.Add(actions,0,1);details.Controls.Add(statusLayout);
            tabs.TabPages.Add(guide);tabs.TabPages.Add(details);
            tabs.SelectedIndexChanged+=(s,e)=>{if(tabs.SelectedTab==details)RefreshStatus();};
            var close=new Button{Text="Luk og gå tilbage",AutoSize=true,DialogResult=DialogResult.Cancel,Anchor=AnchorStyles.Right};
            var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};footer.Controls.Add(close);
            layout.Controls.Add(header,0,0);layout.Controls.Add(tabs,0,1);layout.Controls.Add(footer,0,2);Controls.Add(layout);
            CancelButton=close;
            topics.SelectedIndexChanged+=(s,e)=>{var topic=topics.SelectedItem as HelpTopic;article.Text=topic==null?"Ingen emner matcher. Prøv fx billede, QR, Wi-Fi eller lyd.":topic.Title+"\r\n\r\n"+topic.Body.Replace("\n","\r\n");article.SelectionStart=0;article.ScrollToCaret();};
            search.TextChanged+=(s,e)=>Filter();
            Filter();RefreshStatus();if(showStatus)tabs.SelectedTab=details;
        }
        private static TextBox ReadOnlyText(string name)=>new TextBox{
            Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,WordWrap=true,
            BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle,AccessibleName=name
        };
        private void Filter()
        {
            string previous=(topics.SelectedItem as HelpTopic)?.Id;
            topics.BeginUpdate();
            try {
                topics.Items.Clear();
                foreach(var topic in HelpContent.Search(search.Text))topics.Items.Add(topic);
                int selected=topics.Items.Cast<HelpTopic>().ToList().FindIndex(t=>t.Id==previous);
                if(topics.Items.Count>0)topics.SelectedIndex=Math.Max(0,selected);
                else article.Text="Ingen emner matcher. Prøv fx billede, QR, Wi-Fi eller lyd.";
            } finally {topics.EndUpdate();}
        }
        private void RefreshStatus()
        {
            try {status.Text=statusProvider().Replace("\r\n","\n").Replace("\n","\r\n");}
            catch(Exception ex){status.Text="Status kunne ikke læses: "+ex.Message+"\nVejledningerne kan stadig bruges.";}
        }
    }
}