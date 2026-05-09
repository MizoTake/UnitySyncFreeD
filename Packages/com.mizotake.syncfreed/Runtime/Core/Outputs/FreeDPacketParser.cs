using System;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class FreeDPacketParser
    {
        public bool TryParse(ReadOnlySpan<byte> packet, out CameraObservedFrame frame)
        {
            return TryParse(packet, true, out frame);
        }

        public bool TryParse(ReadOnlySpan<byte> packet, bool validateChecksum, out CameraObservedFrame frame)
        {
            return TryParse(packet, validateChecksum, out frame, out _);
        }

        public bool TryParse(ReadOnlySpan<byte> packet, bool validateChecksum, out CameraObservedFrame frame, out FreeDPacketFailureReason failureReason)
        {
            if (packet.Length != FreeDPacketBuilder.PacketLength)
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.InvalidLength;
                return false;
            }

            if (packet[0] != 0xD1)
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.InvalidMessageType;
                return false;
            }

            if (validateChecksum && packet[28] != FreeDChecksumCalculator.Calculate(packet.Slice(0, 28)))
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.ChecksumMismatch;
                return false;
            }

            var focalLengthMm = DecodeZoom24(ReadUInt24(packet, 20));
            var focusDistanceMeters = DecodeFocus24(ReadUInt24(packet, 23));
            var userArea = ReadUInt16(packet, 26);
            frame = new CameraObservedFrame
            {
                SourceId = "FreeDInput",
                CameraId = packet[1],
                Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris | CameraCapabilities.ExternalTracking,
                Pose = new PoseState
                {
                    PanDeg = DecodeAngle24(ReadInt24(packet, 2)),
                    TiltDeg = DecodeAngle24(ReadInt24(packet, 5)),
                    RollDeg = DecodeAngle24(ReadInt24(packet, 8)),
                    Xmm = DecodePosition24(ReadInt24(packet, 11)),
                    Ymm = DecodePosition24(ReadInt24(packet, 14)),
                    Zmm = DecodePosition24(ReadInt24(packet, 17)),
                    TimestampTicks = DateTime.UtcNow.Ticks
                },
                Lens = new LensState
                {
                    FocalLengthMm = focalLengthMm,
                    FocusDistanceMeters = focusDistanceMeters,
                    IrisFNumber = (userArea & 0x0FFF) / 100d
                },
                Timing = new TimingState
                {
                    FrameModulo16 = (ushort)((userArea >> 12) & 0x0F)
                },
                Validity = new ValidityState
                {
                    IsTrackingValid = true,
                    IsLensValid = true,
                    IsDegraded = false,
                    IsFallbackMode = false
                }
            };
            failureReason = FreeDPacketFailureReason.None;
            return true;
        }

        private static int ReadInt24(ReadOnlySpan<byte> packet, int offset)
        {
            var raw = ReadUInt24(packet, offset);
            return (raw & 0x800000) != 0 ? raw - 0x1000000 : raw;
        }

        private static int ReadUInt24(ReadOnlySpan<byte> packet, int offset)
        {
            return (packet[offset] << 16) | (packet[offset + 1] << 8) | packet[offset + 2];
        }

        private static ushort ReadUInt16(ReadOnlySpan<byte> packet, int offset)
        {
            return (ushort)((packet[offset] << 8) | packet[offset + 1]);
        }

        private static double DecodeAngle24(int encoded)
        {
            return encoded / (double)(1 << 15);
        }

        private static double DecodePosition24(int encoded)
        {
            return encoded / (double)(1 << 6);
        }

        private static double DecodeZoom24(int encoded)
        {
            return encoded <= 0 ? 0d : encoded / 1000d;
        }

        private static double DecodeFocus24(int encoded)
        {
            return encoded <= 0 ? 0d : (1 << 18) / (double)encoded;
        }
    }

    public enum FreeDPacketFailureReason
    {
        None = 0,
        InvalidLength = 1,
        InvalidMessageType = 2,
        ChecksumMismatch = 3,
        CameraIdFiltered = 4
    }
}
