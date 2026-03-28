using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDPacketBuilderTests
    {
        [Test]
        public void Build_WritesD1PacketWithExpectedLengthAndHeader()
        {
            var builder = new FreeDPacketBuilder();
            var state = CreateState();
            var buffer = new byte[FreeDPacketBuilder.PacketLength];

            var written = builder.Build(state, buffer);

            Assert.That(written, Is.EqualTo(29));
            Assert.That(buffer[0], Is.EqualTo(0xD1));
            Assert.That(buffer[1], Is.EqualTo(0x07));
        }

        [Test]
        public void Build_EncodesAnglesPositionsLensAndUserArea()
        {
            var builder = new FreeDPacketBuilder();
            var state = CreateState();
            var buffer = new byte[FreeDPacketBuilder.PacketLength];

            builder.Build(state, buffer);

            Assert.That(ReadInt24(buffer, 2), Is.EqualTo(90 << 15));
            Assert.That(ReadInt24(buffer, 5), Is.EqualTo(-45 << 15));
            Assert.That(ReadInt24(buffer, 8), Is.EqualTo(10 << 15));
            Assert.That(ReadInt24(buffer, 11), Is.EqualTo(100 << 6));
            Assert.That(ReadInt24(buffer, 14), Is.EqualTo(-250 << 6));
            Assert.That(ReadInt24(buffer, 17), Is.EqualTo(12 << 6));
            Assert.That(ReadUInt24(buffer, 20), Is.EqualTo(70000));
            Assert.That(ReadUInt24(buffer, 23), Is.EqualTo(1 << 17));
            Assert.That(ReadUInt16(buffer, 26), Is.EqualTo(0x3230));
        }

        [Test]
        public void Build_CalculatesChecksumFromFirst28Bytes()
        {
            var builder = new FreeDPacketBuilder();
            var state = CreateState();
            var buffer = new byte[FreeDPacketBuilder.PacketLength];

            builder.Build(state, buffer);

            var checksum = FreeDChecksumCalculator.Calculate(new System.ReadOnlySpan<byte>(buffer, 0, 28));
            Assert.That(buffer[28], Is.EqualTo(checksum));
        }

        private static CameraSyncState CreateState()
        {
            return new CameraSyncState
            {
                CameraId = 7,
                Corrected = new PoseState
                {
                    PanDeg = 90d,
                    TiltDeg = -45d,
                    RollDeg = 10d,
                    Xmm = 100d,
                    Ymm = -250d,
                    Zmm = 12d
                },
                Lens = new LensState
                {
                    IrisFNumber = 4.4d,
                    FocalLengthMm = 35d,
                    FocusDistanceMeters = 1d
                },
                CorrectedLens = new LensState
                {
                    IrisFNumber = 5.6d,
                    FocalLengthMm = 70d,
                    FocusDistanceMeters = 2d
                },
                Timing = new TimingState
                {
                    FrameModulo16 = 3
                }
            };
        }

        private static int ReadInt24(byte[] buffer, int offset)
        {
            var raw = ReadUInt24(buffer, offset);
            return (raw & 0x800000) != 0 ? raw - 0x1000000 : raw;
        }

        private static int ReadUInt24(byte[] buffer, int offset)
        {
            return (buffer[offset] << 16) | (buffer[offset + 1] << 8) | buffer[offset + 2];
        }

        private static int ReadUInt16(byte[] buffer, int offset)
        {
            return (buffer[offset] << 8) | buffer[offset + 1];
        }
    }
}
