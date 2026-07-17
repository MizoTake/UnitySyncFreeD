using System;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public static class FreeDEncoding
    {
        private const int Signed24Min = -0x800000;
        private const int Signed24Max = 0x7FFFFF;
        private const int Unsigned24Max = 0xFFFFFF;

        public static int EncodeAngle24(double degrees)
        {
            return EncodeSignedFixedPoint(degrees, 15);
        }

        public static int EncodePosition24(double millimeters)
        {
            return EncodeSignedFixedPoint(millimeters, 6);
        }

        public static int EncodeZoom24(double focalLengthMm)
        {
            if (double.IsNaN(focalLengthMm) || double.IsInfinity(focalLengthMm) || focalLengthMm <= 0d)
            {
                return 0;
            }

            return RoundAndClamp(focalLengthMm * 1000d, 0, Unsigned24Max);
        }

        public static int EncodeFocus24(double focusDistanceMeters)
        {
            if (double.IsNaN(focusDistanceMeters) || double.IsInfinity(focusDistanceMeters) || focusDistanceMeters <= 0d)
            {
                return 0;
            }

            return RoundAndClamp((1d / focusDistanceMeters) * (1 << 18), 0, Signed24Max);
        }

        public static ushort EncodeUserArea(double irisFNumber, ushort frameModulo16)
        {
            var iris = 0;
            if (!double.IsNaN(irisFNumber) && !double.IsInfinity(irisFNumber) && irisFNumber > 0d)
            {
                iris = RoundAndClamp(irisFNumber * 100d, 0, 0x0FFF);
            }

            var frame = (frameModulo16 & 0x000F) << 12;
            return (ushort)(frame | iris);
        }

        public static void WriteInt24BigEndian(Span<byte> destination, int value)
        {
            destination[0] = (byte)((value >> 16) & 0xFF);
            destination[1] = (byte)((value >> 8) & 0xFF);
            destination[2] = (byte)(value & 0xFF);
        }

        public static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
        {
            destination[0] = (byte)((value >> 8) & 0xFF);
            destination[1] = (byte)(value & 0xFF);
        }

        public static int EncodeNormalized24(double normalizedValue)
        {
            if (double.IsNaN(normalizedValue) || double.IsInfinity(normalizedValue))
            {
                return 0;
            }

            var clamped = Math.Clamp(normalizedValue, 0d, 1d);
            return (int)Math.Round(clamped * Unsigned24Max, MidpointRounding.AwayFromZero);
        }

        private static int EncodeSignedFixedPoint(double value, int fractionalBits)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return 0;
            }

            return RoundAndClamp(value * (1 << fractionalBits), Signed24Min, Signed24Max);
        }

        private static int RoundAndClamp(double value, int minimum, int maximum)
        {
            if (double.IsNaN(value))
            {
                return minimum;
            }

            if (value <= minimum)
            {
                return minimum;
            }

            if (value >= maximum)
            {
                return maximum;
            }

            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
