using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    [Serializable]
    public struct LensState
    {
        public double ZoomNormalized;
        public double FocusNormalized;
        public double IrisFNumber;
        public double FocalLengthMm;
        public double FocusDistanceMeters;
    }
}
