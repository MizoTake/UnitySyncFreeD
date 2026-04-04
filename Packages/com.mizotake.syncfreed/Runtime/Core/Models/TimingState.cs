using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    [Serializable]
    public struct TimingState
    {
        public int CommandDelayMs;
        public int TrackingDelayMs;
        public int OutputDelayMs;
        public int VideoAlignmentDelayMs;
        public ushort FrameModulo16;
    }
}
