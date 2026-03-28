namespace MizoTake.SyncFreeD.Core.Models
{
    public struct CurveKeyframe
    {
        public double Time;
        public double Value;

        public CurveKeyframe(double time, double value)
        {
            Time = time;
            Value = value;
        }
    }
}
