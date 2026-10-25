using System;
using System.Collections.Generic;

namespace AndroidLive.Core
{
    internal enum FrameDisplay { Blank, Waiting, Live }

    // Input time comes from a monotonic clock, never the wall clock.
    internal sealed class FrameWaitingState
    {
        internal static readonly TimeSpan Delay = TimeSpan.FromSeconds(15);
        private readonly Dictionary<string, TimeSpan> missingSince = new Dictionary<string, TimeSpan>();
        private string slideKey;

        internal FrameDisplay Update(string slide, string device, bool ready, TimeSpan now)
        {
            if (slideKey != slide) { Reset(); slideKey = slide; }
            if (ready) { missingSince.Remove(device); return FrameDisplay.Live; }
            if (!missingSince.TryGetValue(device, out var since)) missingSince[device] = since = now;
            return now - since >= Delay ? FrameDisplay.Waiting : FrameDisplay.Blank;
        }
        internal void Reset() { missingSince.Clear(); slideKey = null; }
    }
}