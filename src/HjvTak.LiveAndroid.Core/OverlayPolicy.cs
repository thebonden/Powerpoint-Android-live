namespace HjvTak.LiveAndroid.Core
{
    public static class OverlayPolicy
    {
        public static bool ShouldShow(bool running, bool enabled, bool oneShow, bool normalState,
            bool hostVisible, bool powerPointForeground, bool validAnchor)
        {
            return running && enabled && oneShow && normalState && hostVisible &&
                powerPointForeground && validAnchor;
        }
    }
}
