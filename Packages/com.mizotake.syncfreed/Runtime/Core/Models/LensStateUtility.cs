using System;

namespace MizoTake.SyncFreeD.Core.Models
{
    internal static class LensStateUtility
    {
        public static LensState MergePhysicalValues(in LensState primary, in LensState fallback)
        {
            var merged = primary;
            if (!IsPositiveFinite(merged.FocalLengthMm) && IsPositiveFinite(fallback.FocalLengthMm))
            {
                merged.FocalLengthMm = fallback.FocalLengthMm;
            }

            if (!IsPositiveFinite(merged.FocusDistanceMeters) && IsPositiveFinite(fallback.FocusDistanceMeters))
            {
                merged.FocusDistanceMeters = fallback.FocusDistanceMeters;
            }

            if (!IsPositiveFinite(merged.IrisFNumber) && IsPositiveFinite(fallback.IrisFNumber))
            {
                merged.IrisFNumber = fallback.IrisFNumber;
            }

            return merged;
        }

        private static bool IsPositiveFinite(double value)
        {
            return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
