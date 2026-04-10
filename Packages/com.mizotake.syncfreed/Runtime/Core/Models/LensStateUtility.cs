namespace MizoTake.SyncFreeD.Core.Models
{
    internal static class LensStateUtility
    {
        public static LensState MergePhysicalValues(in LensState primary, in LensState fallback)
        {
            var merged = primary;
            if (merged.FocalLengthMm <= 0d && fallback.FocalLengthMm > 0d)
            {
                merged.FocalLengthMm = fallback.FocalLengthMm;
            }

            if (merged.FocusDistanceMeters <= 0d && fallback.FocusDistanceMeters > 0d)
            {
                merged.FocusDistanceMeters = fallback.FocusDistanceMeters;
            }

            if (merged.IrisFNumber <= 0d && fallback.IrisFNumber > 0d)
            {
                merged.IrisFNumber = fallback.IrisFNumber;
            }

            return merged;
        }
    }
}
