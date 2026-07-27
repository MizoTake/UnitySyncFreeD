using System;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDPacketParserTests
    {
        [Test]
        public void TryParse_SyncFreeDPhysicalProfileRoundTripsPacketBuilderValues()
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

            Assert.That(parser.TryParse(packet, FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.SyncFreeDPhysicalV1), true, out var frame, out _), Is.True);
            Assert.That(frame.CameraId, Is.EqualTo(12));
            Assert.That(frame.RawFreeD.MessageType, Is.EqualTo(FreeDMessageType.CameraPositionAndOrientation));
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
            var shortPacket = new byte[FreeDPacketBuilder.PacketLength - 1];
            var paddedPacket = new byte[FreeDPacketBuilder.PacketLength + 1];
            shortPacket[0] = (byte)FreeDMessageType.CameraPositionAndOrientation;
            paddedPacket[0] = (byte)FreeDMessageType.CameraPositionAndOrientation;

            Assert.That(parser.TryParse(shortPacket, true, out _, out var shortReason), Is.False);
            Assert.That(shortReason, Is.EqualTo(FreeDPacketFailureReason.InvalidLength));
            Assert.That(parser.TryParse(paddedPacket, true, out _, out var paddedReason), Is.False);
            Assert.That(paddedReason, Is.EqualTo(FreeDPacketFailureReason.InvalidLength));
        }

        [Test]
        public void TryParse_ReturnsUnsupportedMessageTypeBeforeD1LengthValidation()
        {
            var packet = new[] { (byte)FreeDMessageType.SystemStatus };

            Assert.That(new FreeDPacketParser().TryParse(packet, true, out _, out var failureReason), Is.False);
            Assert.That(failureReason, Is.EqualTo(FreeDPacketFailureReason.UnsupportedMessageType));
        }

        [Test]
        public void TryParse_DefaultProfilePreservesD1RawValuesWithoutDeviceAssumptions()
        {
            var packet = CreateRawPacket(0x1800, 0x4000);

            Assert.That(new FreeDPacketParser().TryParse(packet, out var frame), Is.True);
            Assert.That(frame.RawFreeD.MessageType, Is.EqualTo(FreeDMessageType.CameraPositionAndOrientation));
            Assert.That(frame.RawFreeD.Zoom, Is.EqualTo(0x1800));
            Assert.That(frame.RawFreeD.Focus, Is.EqualTo(0x4000));
            Assert.That(frame.Capabilities, Is.EqualTo(CameraCapabilities.None));
        }

        [TestCase(0x0000, 9.3d)]
        [TestCase(0x1800, 18.6d)]
        [TestCase(0x2340, 27.9d)]
        [TestCase(0x2A40, 37.2d)]
        [TestCase(0x2F00, 46.5d)]
        [TestCase(0x3300, 55.8d)]
        [TestCase(0x3600, 65.1d)]
        [TestCase(0x3880, 74.4d)]
        [TestCase(0x3AC0, 83.7d)]
        [TestCase(0x3CC0, 93d)]
        [TestCase(0x3E80, 102.3d)]
        [TestCase(0x4000, 111.6d)]
        public void TryParse_SonyBrcX1000ProfileDecodesOpticalZoomPosition(int rawZoom, double expectedFocalLengthMm)
        {
            var packet = CreateRawPacket(rawZoom, 0x2000);
            var profile = CreateBrcX1000ReferenceProfile();

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out var frame, out var failureReason), Is.True, failureReason.ToString());
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(expectedFocalLengthMm).Within(0.001d));
            Assert.That(frame.Lens.EffectiveFocalLengthMm, Is.EqualTo(expectedFocalLengthMm).Within(0.001d));
            Assert.That(frame.RawFreeD.Zoom, Is.EqualTo(rawZoom));
            Assert.That(frame.Capabilities.HasFlag(CameraCapabilities.Position), Is.False);
            Assert.That(frame.Capabilities.HasFlag(CameraCapabilities.Roll), Is.False);
            Assert.That(frame.Projection.SensorWidthMm, Is.EqualTo(11.7584318941d).Within(0.000001d));
            Assert.That(frame.Projection.SensorHeightMm, Is.EqualTo(6.61411794045d).Within(0.000001d));
        }

        [TestCase(0x5580, 167.4d)]
        [TestCase(0x6000, 223.2d)]
        public void TryParse_SonyBrcX1000ProfileSeparatesOpticalAndClearImageZoom(int rawZoom, double expectedEffectiveFocalLengthMm)
        {
            var packet = CreateRawPacket(rawZoom, 0x2000);
            var profile = CreateBrcX1000ReferenceProfile();

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out var frame, out _), Is.True);
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(111.6d).Within(0.001d));
            Assert.That(frame.Lens.EffectiveFocalLengthMm, Is.EqualTo(expectedEffectiveFocalLengthMm).Within(0.001d));
        }

        [TestCase(0x1000, 1000d)]
        [TestCase(0x2000, 5d)]
        [TestCase(0x3000, 3d)]
        [TestCase(0x4000, 2d)]
        [TestCase(0x5000, 1.5d)]
        [TestCase(0x6000, 1.2d)]
        [TestCase(0x7000, 1d)]
        [TestCase(0x8000, 0.8d)]
        [TestCase(0x9000, 0.55d)]
        [TestCase(0xA000, 0.35d)]
        [TestCase(0xB000, 0.25d)]
        [TestCase(0xC000, 0.18d)]
        [TestCase(0xD000, 0.14d)]
        [TestCase(0xE000, 0.1d)]
        [TestCase(0xF000, 0.08d)]
        public void TryParse_SonyBrcX1000ProfileDecodesFocusReferenceTable(int rawFocus, double expectedDistanceMeters)
        {
            var packet = CreateRawPacket(0x0000, rawFocus);
            var profile = CreateBrcX1000ReferenceProfile();

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out var frame, out _), Is.True);
            Assert.That(frame.Lens.FocusDistanceMeters, Is.EqualTo(expectedDistanceMeters).Within(0.001d));
            Assert.That(frame.RawFreeD.Focus, Is.EqualTo(rawFocus));
        }

        [Test]
        public void TryParse_CustomProfileUsesDataDrivenCurvesAndCapabilities()
        {
            var packet = CreateRawPacket(500, 250);
            var profile = new FreeDPacketDecodingProfile
            {
                ProfileName = "Custom Lens",
                Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Zoom | CameraCapabilities.Focus,
                FocalLengthMm = new FreeDUnsignedFieldDecoder
                {
                    Mode = FreeDUnsignedFieldDecodingMode.PiecewiseLinear,
                    Curve = new CurveDefinition { Keys = new[] { new CurveKeyframe(0d, 10d), new CurveKeyframe(1000d, 110d) } }
                },
                FocusDistanceMeters = new FreeDUnsignedFieldDecoder
                {
                    Mode = FreeDUnsignedFieldDecodingMode.ScaleAndOffset,
                    Scale = 0.01d
                }
            };

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out var frame, out _), Is.True);
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(60d).Within(0.001d));
            Assert.That(frame.Lens.FocusDistanceMeters, Is.EqualTo(2.5d).Within(0.001d));
            Assert.That(frame.Capabilities, Is.EqualTo(profile.Capabilities));
        }

        [Test]
        public void TryParse_RawUnsigned24ProfilePreservesRawValuesWithoutInventingPhysicalLensValues()
        {
            var packet = CreateRawPacket(0x400000, 0x800000);
            var profile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out var frame, out _), Is.True);
            Assert.That(frame.Capabilities, Is.EqualTo(CameraCapabilities.None));
            Assert.That(frame.Lens.ZoomNormalized, Is.Zero);
            Assert.That(frame.Lens.FocusNormalized, Is.Zero);
            Assert.That(frame.Lens.FocalLengthMm, Is.Zero);
            Assert.That(frame.Lens.FocusDistanceMeters, Is.Zero);
            Assert.That(frame.RawFreeD.Zoom, Is.EqualTo(0x400000));
            Assert.That(frame.RawFreeD.Focus, Is.EqualTo(0x800000));
        }

        [Test]
        public void ProfileValidator_RejectsUnsortedOrDuplicateCurveKeys()
        {
            var profile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);
            profile.Capabilities = CameraCapabilities.Zoom;
            profile.FocalLengthMm = new FreeDUnsignedFieldDecoder
            {
                Mode = FreeDUnsignedFieldDecodingMode.PiecewiseLinear,
                Curve = new CurveDefinition { Keys = new[] { new CurveKeyframe(100d, 10d), new CurveKeyframe(100d, 20d) } }
            };

            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(profile, out var error), Is.False);
            Assert.That(error, Does.Contain("unique ascending"));
        }

        [Test]
        public void ProfileValidator_RejectsDecoderWithoutMatchingCapability()
        {
            var profile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);
            profile.FocalLengthMm = new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.ScaleAndOffset, Scale = 0.001d };

            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(profile, out var error), Is.False);
            Assert.That(error, Does.Contain("Zoom capability"));
        }

        [Test]
        public void TryParse_RejectsInvalidDirectProfile()
        {
            var packet = CreateRawPacket(100, 200);
            var profile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);
            profile.Capabilities = CameraCapabilities.Focus;

            Assert.That(new FreeDPacketParser().TryParse(packet, profile, true, out _, out var failureReason), Is.False);
            Assert.That(failureReason, Is.EqualTo(FreeDPacketFailureReason.InvalidDecodingProfile));
        }

        [Test]
        public void Create_UnknownPresetFailsClosed()
        {
            var profile = FreeDPacketDecodingProfiles.Create((FreeDPacketDecodingPreset)int.MaxValue);

            Assert.That(profile.Capabilities, Is.EqualTo(CameraCapabilities.None));
            Assert.That(profile.ProfileName, Is.EqualTo("Invalid Profile Raw Capture"));
            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(profile, out _), Is.True);
        }

        [Test]
        public void ProfileValidator_AcceptsBuiltInProfiles()
        {
            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.SyncFreeDPhysicalV1), out _), Is.True);
            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24), out _), Is.True);
            Assert.That(FreeDPacketDecodingProfileValidator.TryValidate(CreateBrcX1000ReferenceProfile(), out _), Is.True);
        }

        [Test]
        public void Create_BuiltInProfileRequiresExplicitProfileId()
        {
            Assert.That(FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.BuiltInDeviceProfile, string.Empty), Is.Null);
            Assert.That(FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.BuiltInDeviceProfile, "unknown-profile"), Is.Null);
        }

        private static FreeDPacketDecodingProfile CreateBrcX1000ReferenceProfile()
        {
            return FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.BuiltInDeviceProfile, FreeDBuiltInPacketDecodingProfileIds.SonyBrcX1000Firmware210);
        }

        private static byte[] CreateRawPacket(int rawZoom, int rawFocus)
        {
            var packet = new byte[FreeDPacketBuilder.PacketLength];
            packet[0] = 0xD1;
            packet[1] = 1;
            WriteUInt24(packet, 20, rawZoom);
            WriteUInt24(packet, 23, rawFocus);
            packet[28] = FreeDChecksumCalculator.Calculate(packet.AsSpan(0, 28));
            return packet;
        }

        private static void WriteUInt24(byte[] packet, int offset, int value)
        {
            packet[offset] = (byte)((value >> 16) & 0xFF);
            packet[offset + 1] = (byte)((value >> 8) & 0xFF);
            packet[offset + 2] = (byte)(value & 0xFF);
        }
    }
}
