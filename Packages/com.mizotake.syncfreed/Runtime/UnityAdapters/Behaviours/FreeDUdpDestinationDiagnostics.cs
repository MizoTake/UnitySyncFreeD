using System;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [Serializable]
    public struct FreeDUdpDestinationDiagnostics
    {
        public int Order;
        public string Label;
        public string IpAddress;
        public int Port;
        public long OffsetMicroseconds;
        public bool Succeeded;
    }
}
