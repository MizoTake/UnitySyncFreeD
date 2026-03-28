using System;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public static class FreeDChecksumCalculator
    {
        public static byte Calculate(ReadOnlySpan<byte> messageWithoutChecksum)
        {
            var checksum = 0x40;
            for (var i = 0; i < messageWithoutChecksum.Length; i++)
            {
                checksum = (checksum - messageWithoutChecksum[i]) & 0xFF;
            }

            return (byte)checksum;
        }
    }
}
