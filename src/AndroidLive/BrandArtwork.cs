using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AndroidLive
{
    // One scalable, locally drawn identity. No downloaded assets, fonts or third-party logo.
    internal static class BrandArtwork
    {
        internal static readonly Color Background = Color.FromArgb(24,35,45);
        internal static readonly Color Accent = Color.FromArgb(38,190,176);
        internal static Bitmap Icon(string action, int size)
        {
            var bitmap=new Bitmap(size,size);
            using(var g=Graphics.FromImage(bitmap)) {
                g.Clear(Color.Transparent);
                Draw(g,new RectangleF(1,1,size-2,size-2),Color.FromArgb(30,116,134),action);
            }
            return bitmap;
        }
        internal static void Draw(Graphics g,RectangleF bounds,Color ink,string action="brand")
        {
            var state=g.Save();
            try {
                g.SmoothingMode=SmoothingMode.AntiAlias;
                float scale=Math.Min(bounds.Width,bounds.Height)/64f;
                if(scale<=0)return;
                g.TranslateTransform(bounds.X+(bounds.Width-64*scale)/2,bounds.Y+(bounds.Height-64*scale)/2);
                g.ScaleTransform(scale,scale);
                using(var pen=new Pen(ink,3){StartCap=LineCap.Round,EndCap=LineCap.Round})
                using(var brush=new SolidBrush(ink))
                using(var accent=new Pen(Accent,3){StartCap=LineCap.Round,EndCap=LineCap.Round})
                {
                    switch(action) {
                        case "play":
                            g.FillPolygon(brush,new[]{new PointF(20,12),new PointF(51,32),new PointF(20,52)});break;
                        case "all":
                            g.DrawRectangle(pen,8,8,32,42);
                            g.DrawRectangle(accent,18,17,32,42);break;
                        case "stop":
                            g.FillRectangle(brush,14,14,36,36);break;
                        case "remove":
                            g.DrawRectangle(pen,17,18,30,36);g.DrawLine(pen,12,13,52,13);
                            g.DrawLine(pen,24,7,40,7);g.DrawLine(pen,26,26,26,46);g.DrawLine(pen,38,26,38,46);break;
                        case "rename":
                            g.DrawLines(pen,new[]{new PointF(13,47),new PointF(43,11),new PointF(53,20),new PointF(22,55),new PointF(11,57),new PointF(13,47)});
                            g.DrawLine(pen,36,19,46,28);break;
                        case "frame":
                            g.DrawRectangle(pen,5,10,54,43);g.DrawLine(accent,12,45,24,45);
                            g.DrawLine(accent,12,37,12,45);g.DrawLine(accent,40,18,52,18);break;
                        case "eye":
                            g.DrawEllipse(pen,5,17,54,30);g.DrawEllipse(accent,23,23,18,18);break;
                        case "audio":
                            g.DrawPolygon(pen,new[]{new PointF(9,25),new PointF(20,25),new PointF(33,14),new PointF(33,50),new PointF(20,39),new PointF(9,39)});
                            g.DrawArc(accent,32,15,23,34,-65,130);break;
                        case "touch":
                            g.DrawEllipse(accent,9,8,36,36);g.DrawEllipse(pen,19,18,16,16);
                            g.DrawLines(pen,new[]{new PointF(29,27),new PointF(36,55),new PointF(43,43),new PointF(56,40),new PointF(29,27)});break;
                        case "settings":
                            for(int row=0;row<3;row++) {
                                float y=15+17*row;g.DrawLine(pen,7,y,57,y);
                                g.FillRectangle(brush,row==1?37:18,y-5,8,10);
                            }break;
                        case "help":
                            g.DrawEllipse(pen,6,6,52,52);
                            g.DrawArc(pen,23,17,18,17,190,285);g.DrawLine(pen,32,34,32,39);
                            g.FillEllipse(brush,30,46,4,4);break;
                        case "status":
                            g.DrawEllipse(pen,6,6,52,52);g.DrawLine(pen,32,29,32,47);g.FillEllipse(brush,30,17,4,4);break;
                        case "qr":
                            g.DrawRectangle(pen,5,5,20,20);g.DrawRectangle(pen,39,5,20,20);g.DrawRectangle(pen,5,39,20,20);
                            g.FillRectangle(brush,11,11,8,8);g.FillRectangle(brush,45,11,8,8);g.FillRectangle(brush,11,45,8,8);
                            g.DrawLines(accent,new[]{new PointF(38,34),new PointF(38,45),new PointF(49,45),new PointF(49,58),new PointF(59,58)});
                            g.DrawLine(accent,56,34,56,42);g.DrawLine(accent,34,56,40,56);break;
                        default:
                            using(var path=new GraphicsPath()) {
                                path.AddArc(10,10,8,8,180,90);path.AddArc(36,10,8,8,270,90);
                                path.AddArc(36,50,8,8,0,90);path.AddArc(10,50,8,8,90,90);
                                path.CloseFigure();g.DrawPath(pen,path);
                            }
                            g.DrawLine(pen,23,51,31,51);
                            g.DrawArc(accent,32,1,28,25,220,100);g.DrawArc(accent,38,7,16,14,220,100);
                            g.FillEllipse(brush,45,17,3,3);break;
                    }
                }
            } finally {g.Restore(state);}
        }
        internal static Bitmap Frame(float width,float height)
        {
            if(width<=0||height<=0||float.IsNaN(width)||float.IsNaN(height)||float.IsInfinity(width)||float.IsInfinity(height))
                throw new ArgumentException("Invalid frame size.");
            float scale=800f/Math.Max(width,height);
            int w=Math.Max(32,(int)(width*scale)),h=Math.Max(32,(int)(height*scale));
            var bitmap=new Bitmap(w,h);
            using(var g=Graphics.FromImage(bitmap)) {
                g.Clear(Background);
                float icon=Math.Min(w*.25f,h*.30f);
                Draw(g,new RectangleF((w-icon)/2,h*.09f,icon,icon),Color.FromArgb(206,227,237));
            }
            return bitmap;
        }
    }
}