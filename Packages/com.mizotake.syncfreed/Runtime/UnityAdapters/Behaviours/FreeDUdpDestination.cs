using System;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [Serializable]
    public struct FreeDUdpDestination
    {
        public string IpAddress;
        public int Port;
        public bool Enabled;
    }
}
