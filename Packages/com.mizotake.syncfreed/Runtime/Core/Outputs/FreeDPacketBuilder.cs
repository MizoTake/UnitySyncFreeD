using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
#pragma warning disable CS0618
    public sealed class FreeDPacketBuilder : IFreeDPacketBuilder
#pragma warning restore CS0618
    {
        public const int PacketLength = 29;
        public const int PacketSize = PacketLength;
        private const byte MessageType = 0xD1;

        public int Build(in CameraSyncState state, Span<byte> destination)
        {
            var outputLens = LensStateUtility.MergePhysicalValues(state.CorrectedLens, state.Lens);
            if (destination.Length < PacketLength)
            {
                throw new ArgumentException("Destination must be at least 29 bytes.", nameof(destination));
            }

            destination[0] = MessageType;
            destination[1] = (byte)Math.Clamp(state.CameraId, 0, 255);
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(2, 3), FreeDEncoding.EncodeAngle24(state.Corrected.PanDeg));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(5, 3), FreeDEncoding.EncodeAngle24(state.Corrected.TiltDeg));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(8, 3), FreeDEncoding.EncodeAngle24(state.Corrected.RollDeg));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(11, 3), FreeDEncoding.EncodePosition24(state.Corrected.Xmm));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(14, 3), FreeDEncoding.EncodePosition24(state.Corrected.Ymm));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(17, 3), FreeDEncoding.EncodePosition24(state.Corrected.Zmm));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(20, 3), FreeDEncoding.EncodeZoom24(outputLens.FocalLengthMm));
            FreeDEncoding.WriteInt24BigEndian(destination.Slice(23, 3), FreeDEncoding.EncodeFocus24(outputLens.FocusDistanceMeters));
            FreeDEncoding.WriteUInt16BigEndian(destination.Slice(26, 2), FreeDEncoding.EncodeUserArea(outputLens.IrisFNumber, state.Timing.FrameModulo16));
            destination[28] = FreeDChecksumCalculator.Calculate(destination.Slice(0, 28));
            return PacketLength;
        }
    }
}
