using System;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    internal static class FreeDEncodingUtility
    {
        public static void WriteSigned24(Span<byte> destination, int offset, int value)
        {
            var packed = value & 0xFFFFFF;
            destination[offset] = (byte)((packed >> 16) & 0xFF);
            destination[offset + 1] = (byte)((packed >> 8) & 0xFF);
            destination[offset + 2] = (byte)(packed & 0xFF);
        }

        public static void WriteUnsigned24(Span<byte> destination, int offset, int value)
        {
            var clamped = Math.Clamp(value, 0, 0xFFFFFF);
            destination[offset] = (byte)((clamped >> 16) & 0xFF);
            destination[offset + 1] = (byte)((clamped >> 8) & 0xFF);
            destination[offset + 2] = (byte)(clamped & 0xFF);
        }

        public static int QuantizeSigned24FromDegrees(double degrees)
        {
            return (int)Math.Round(degrees * 32768d);
        }

        public static int QuantizeSigned24FromMillimeters(double millimeters)
        {
            return (int)Math.Round(millimeters);
        }

        public static int QuantizeUnsigned24Normalized(double normalizedValue)
        {
            var clamped = Math.Clamp(normalizedValue, 0d, 1d);
            return (int)Math.Round(clamped * 0xFFFFFF);
        }
    }
}
