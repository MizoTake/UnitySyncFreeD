using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Sync
{
    public static class StateInterpolator
    {
        public static PoseState Lerp(in PoseState from, in PoseState to, double t)
        {
            var clamped = t < 0d ? 0d : (t > 1d ? 1d : t);
            return new PoseState
            {
                PanDeg = LerpDouble(from.PanDeg, to.PanDeg, clamped),
                TiltDeg = LerpDouble(from.TiltDeg, to.TiltDeg, clamped),
                RollDeg = LerpDouble(from.RollDeg, to.RollDeg, clamped),
                Xmm = LerpDouble(from.Xmm, to.Xmm, clamped),
                Ymm = LerpDouble(from.Ymm, to.Ymm, clamped),
                Zmm = LerpDouble(from.Zmm, to.Zmm, clamped),
                TimestampTicks = (long)LerpDouble(from.TimestampTicks, to.TimestampTicks, clamped)
            };
        }

        private static double LerpDouble(double from, double to, double t)
        {
            return from + ((to - from) * t);
        }
    }
}
