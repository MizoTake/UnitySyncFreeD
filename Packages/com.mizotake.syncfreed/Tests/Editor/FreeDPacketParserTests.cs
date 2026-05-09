using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDPacketParserTests
    {
        [Test]
        public void TryParse_RoundTripsPacketBuilderValues()
        {
            var state = new CameraSyncState
            {
                CameraId = 12,
                Corrected = new PoseState
                {
                    PanDeg = 30.25d,
                    TiltDeg = -12.5d,
                    RollDeg = 4.75d,
                    Xmm = 1234d,
                    Ymm = -567d,
                    Zmm = 890d
                },
                CorrectedLens = new LensState
                {
                    FocalLengthMm = 42.5d,
                    FocusDistanceMeters = 3.5d,
                    IrisFNumber = 2.8d
                },
                Timing = new TimingState
                {
                    FrameModulo16 = 9
                }
            };
            var packet = new byte[FreeDPacketBuilder.PacketLength];
            var builder = new FreeDPacketBuilder();
            var parser = new FreeDPacketParser();

            builder.Build(state, packet);

            Assert.That(parser.TryParse(packet, out var frame), Is.True);
            Assert.That(frame.CameraId, Is.EqualTo(12));
            Assert.That(frame.Pose.PanDeg, Is.EqualTo(30.25d).Within(0.001d));
            Assert.That(frame.Pose.TiltDeg, Is.EqualTo(-12.5d).Within(0.001d));
            Assert.That(frame.Pose.RollDeg, Is.EqualTo(4.75d).Within(0.001d));
            Assert.That(frame.Pose.Xmm, Is.EqualTo(1234d).Within(0.001d));
            Assert.That(frame.Pose.Ymm, Is.EqualTo(-567d).Within(0.001d));
            Assert.That(frame.Pose.Zmm, Is.EqualTo(890d).Within(0.001d));
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(42.5d).Within(0.001d));
            Assert.That(frame.Lens.FocusDistanceMeters, Is.EqualTo(3.5d).Within(0.01d));
            Assert.That(frame.Lens.IrisFNumber, Is.EqualTo(2.8d).Within(0.001d));
            Assert.That(frame.Timing.FrameModulo16, Is.EqualTo(9));
        }

        [Test]
        public void TryParse_ReturnsFalseWhenChecksumDoesNotMatch()
        {
            var packet = new byte[FreeDPacketBuilder.PacketLength];
            var builder = new FreeDPacketBuilder();

            builder.Build(new CameraSyncState { CameraId = 1, Corrected = new PoseState(), CorrectedLens = new LensState { FocalLengthMm = 35d, FocusDistanceMeters = 1d, IrisFNumber = 2d } }, packet);
            packet[28] ^= 0x01;

            Assert.That(new FreeDPacketParser().TryParse(packet, true, out _, out var failureReason), Is.False);
            Assert.That(failureReason, Is.EqualTo(FreeDPacketFailureReason.ChecksumMismatch));
        }

        [Test]
        public void TryParse_ReturnsInvalidLengthWhenPacketIsNotExactlyD1Length()
        {
            var parser = new FreeDPacketParser();

            Assert.That(parser.TryParse(new byte[FreeDPacketBuilder.PacketLength - 1], true, out _, out var shortReason), Is.False);
            Assert.That(shortReason, Is.EqualTo(FreeDPacketFailureReason.InvalidLength));
            Assert.That(parser.TryParse(new byte[FreeDPacketBuilder.PacketLength + 1], true, out _, out var paddedReason), Is.False);
            Assert.That(paddedReason, Is.EqualTo(FreeDPacketFailureReason.InvalidLength));
        }

        [Test]
        public void TryParse_ReturnsInvalidMessageTypeWhenHeaderIsNotD1()
        {
            var packet = new byte[FreeDPacketBuilder.PacketLength];

            Assert.That(new FreeDPacketParser().TryParse(packet, true, out _, out var failureReason), Is.False);
            Assert.That(failureReason, Is.EqualTo(FreeDPacketFailureReason.InvalidMessageType));
        }
    }
}
