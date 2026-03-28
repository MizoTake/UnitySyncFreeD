namespace MizoTake.SyncFreeD.Core.Models
{
    public sealed class LensProfile
    {
        public string LensName = string.Empty;
        public double MinFocalLengthMm;
        public double MaxFocalLengthMm;
        public CurveDefinition ZoomCurve = new CurveDefinition();
        public CurveDefinition FocusCurve = new CurveDefinition();
    }
}
