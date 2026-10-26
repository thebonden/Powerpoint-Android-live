using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AndroidLive
{
    internal sealed class RibbonImages : IDisposable
    {
        private readonly Dictionary<string,Bitmap> bitmaps=new Dictionary<string,Bitmap>();
        private readonly Dictionary<string,stdole.IPictureDisp> pictures=new Dictionary<string,stdole.IPictureDisp>();
        internal stdole.IPictureDisp Get(string id)
        {
            string kind;
            switch(id) {
                case "AddTablet":case "ConnectWifi":kind="qr";break;
                case "StartAndroid":kind="play";break;
                case "StartAll":kind="all";break;
                case "StopAndroid":case "StopAll":kind="stop";break;
                case "RenameDevice":kind="rename";break;
                case "RemoveDevice":kind="remove";break;
                case "AddMarker":kind="frame";break;
                case "Preview":case "ShowOverlay":kind="eye";break;
                case "Audio":kind="audio";break;
                case "Touches":kind="touch";break;
                case "Settings":kind="settings";break;
                case "Status":kind="status";break;
                case "Help":kind="help";break;
                default:kind="brand";break;
            }
            if(!pictures.TryGetValue(kind,out var picture)){
                var bitmap=BrandArtwork.Icon(kind,32);
                try {picture=PictureConverter.Convert(bitmap);pictures.Add(kind,picture);bitmaps.Add(kind,bitmap);}
                catch {bitmap.Dispose();throw;}
            }
            return picture;
        }
        public void Dispose()
        {
            foreach(var picture in pictures.Values)if(Marshal.IsComObject(picture))Marshal.ReleaseComObject(picture);
            pictures.Clear();foreach(var bitmap in bitmaps.Values)bitmap.Dispose();bitmaps.Clear();
        }
        private sealed class PictureConverter : AxHost
        {
            private PictureConverter():base(""){}
            internal static stdole.IPictureDisp Convert(Image image)=>(stdole.IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}