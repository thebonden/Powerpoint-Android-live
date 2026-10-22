using System;

namespace HjvTak.LiveAndroid.Core
{
    public enum DeviceState { Stopped, Starting, Running, Faulted }

    // No Office or process dependencies: transitions can be tested without PowerPoint or a phone.
    public sealed class SessionState
    {
        public DeviceState State { get; private set; }
        public string Detail { get; private set; } = "Stopped";
        private DateTime deadline;
        public void Start(DateTime utcNow)
        {
            State = DeviceState.Starting;
            Detail = "Waiting for tablet video over Wi-Fi";
            deadline = utcNow.AddSeconds(20);
        }
        public bool Poll(DateTime utcNow, bool exited, int exitCode, bool hasWindow)
        {
            if (State != DeviceState.Starting && State != DeviceState.Running) return false;
            if (exited) { Fail("scrcpy exited (code " + exitCode + "). Check the device and start again."); return true; }
            if (State == DeviceState.Starting && hasWindow) { State = DeviceState.Running; Detail = "Connected"; }
            else if (State == DeviceState.Starting && utcNow >= deadline)
            { Fail("No video window after 20 seconds. Check Wireless debugging and the connection address."); return true; }
            else if (State == DeviceState.Running && !hasWindow)
            { Fail("The video window closed. Start again to reconnect."); return true; }
            return false;
        }
        public void Fail(string detail) { State = DeviceState.Faulted; Detail = detail; }
        public void Stop() { State = DeviceState.Stopped; Detail = "Stopped"; }
    }
}
