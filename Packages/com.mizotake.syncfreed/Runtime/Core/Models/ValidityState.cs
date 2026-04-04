using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    [Serializable]
    public struct ValidityState
    {
        public bool IsTrackingValid;
        public bool IsLensValid;
        public bool IsDegraded;
        public bool IsFallbackMode;
    }
}
