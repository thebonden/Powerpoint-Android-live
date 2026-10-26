using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidLive
{
    // One independently owned process/window per device. Never infer the device from slide order.
    internal sealed class DeviceStreams : IDisposable
    {
        private readonly Dictionary<string, ScrcpySession> sessions = new Dictionary<string, ScrcpySession>();
        internal IEnumerable<KeyValuePair<string, ScrcpySession>> All => sessions.ToArray();
        internal ScrcpySession Get(string id) => id != null && sessions.TryGetValue(id, out var value) ? value : null;
        internal ScrcpySession Start(string id, string executable, string endpoint, bool audio, bool touches, string orientation)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Device id is required.");
            var session = Get(id);
            if (session == null) { session = new ScrcpySession(); sessions.Add(id, session); }
            session.Start(executable, endpoint, audio, touches, orientation);
            return session;
        }
        internal void Stop(string id) { Get(id)?.Stop(); }
        internal void Remove(string id)
        {
            var session=Get(id);
            if(session==null)return;
            session.Dispose(); sessions.Remove(id);
        }
        internal void HideAll() { foreach (var item in All) item.Value.Hide(); }
        public void Dispose() { foreach (var item in All) item.Value.Dispose(); sessions.Clear(); }
    }
}