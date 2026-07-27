using System;
using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public sealed class FreeDPacketParser
    {
        private static readonly FreeDPacketDecodingProfile DefaultDecodingProfile = FreeDPacketDecodingProfiles.Create(FreeDPacketDecodingPreset.RawUnsigned24);

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
            return TryParse(packet, DefaultDecodingProfile, validateChecksum, out frame, out failureReason);
        }

        public bool TryParse(ReadOnlySpan<byte> packet, FreeDPacketDecodingProfile decodingProfile, bool validateChecksum, out CameraObservedFrame frame, out FreeDPacketFailureReason failureReason)
        {
            if (packet.Length == 0)
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.InvalidLength;
                return false;
            }

            if (packet[0] != (byte)FreeDMessageType.CameraPositionAndOrientation)
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.UnsupportedMessageType;
                return false;
            }

            if (packet.Length != FreeDPacketBuilder.PacketLength)
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.InvalidLength;
                return false;
            }

            if (validateChecksum && packet[28] != FreeDChecksumCalculator.Calculate(packet.Slice(0, 28)))
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.ChecksumMismatch;
                return false;
            }

            decodingProfile ??= DefaultDecodingProfile;
            if (!FreeDPacketDecodingProfileValidator.TryValidate(decodingProfile, out _))
            {
                frame = default;
                failureReason = FreeDPacketFailureReason.InvalidDecodingProfile;
                return false;
            }

            var rawPan = ReadInt24(packet, 2);
            var rawTilt = ReadInt24(packet, 5);
            var rawRoll = ReadInt24(packet, 8);
            var rawX = ReadInt24(packet, 11);
            var rawY = ReadInt24(packet, 14);
            var rawZ = ReadInt24(packet, 17);
            var rawZoom = ReadUInt24(packet, 20);
            var rawFocus = ReadUInt24(packet, 23);
            var userArea = ReadUInt16(packet, 26);
            var capabilities = decodingProfile.Capabilities;
            var decodesIrisAndFrameCounter = decodingProfile.UserData == FreeDUserDataDecodingMode.IrisX100AndFrameModulo16;
            frame = new CameraObservedFrame
            {
                SourceId = "FreeDInput",
                CameraId = packet[1],
                Capabilities = capabilities,
                Pose = new PoseState
                {
                    PanDeg = HasCapability(capabilities, CameraCapabilities.PanTilt) ? DecodeAngle24(rawPan) : 0d,
                    TiltDeg = HasCapability(capabilities, CameraCapabilities.PanTilt) ? DecodeAngle24(rawTilt) : 0d,
                    RollDeg = HasCapability(capabilities, CameraCapabilities.Roll) ? DecodeAngle24(rawRoll) : 0d,
                    Xmm = HasCapability(capabilities, CameraCapabilities.Position) ? DecodePosition24(rawX) : 0d,
                    Ymm = HasCapability(capabilities, CameraCapabilities.Position) ? DecodePosition24(rawY) : 0d,
                    Zmm = HasCapability(capabilities, CameraCapabilities.Position) ? DecodePosition24(rawZ) : 0d,
                    TimestampTicks = DateTime.UtcNow.Ticks
                },
                Lens = new LensState
                {
                    ZoomNormalized = HasCapability(capabilities, CameraCapabilities.Zoom) ? DecodeLensField(decodingProfile.ZoomNormalized, rawZoom) : 0d,
                    FocalLengthMm = HasCapability(capabilities, CameraCapabilities.Zoom) ? DecodeLensField(decodingProfile.FocalLengthMm, rawZoom) : 0d,
                    EffectiveFocalLengthMm = HasCapability(capabilities, CameraCapabilities.Zoom) ? DecodeLensField(decodingProfile.EffectiveFocalLengthMm, rawZoom) : 0d,
                    FocusNormalized = HasCapability(capabilities, CameraCapabilities.Focus) ? DecodeLensField(decodingProfile.FocusNormalized, rawFocus) : 0d,
                    FocusDistanceMeters = HasCapability(capabilities, CameraCapabilities.Focus) ? DecodeLensField(decodingProfile.FocusDistanceMeters, rawFocus) : 0d,
                    IrisFNumber = decodesIrisAndFrameCounter && HasCapability(capabilities, CameraCapabilities.Iris) ? (userArea & 0x0FFF) / 100d : 0d
                },
                Projection = new CameraProjectionState
                {
                    SensorWidthMm = HasCapability(capabilities, CameraCapabilities.Zoom) ? decodingProfile.SensorWidthMm : 0d,
                    SensorHeightMm = HasCapability(capabilities, CameraCapabilities.Zoom) ? decodingProfile.SensorHeightMm : 0d
                },
                Timing = new TimingState
                {
                    FrameModulo16 = decodesIrisAndFrameCounter ? (ushort)((userArea >> 12) & 0x0F) : (ushort)0
                },
                Validity = new ValidityState
                {
                    IsTrackingValid = HasCapability(capabilities, CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position),
                    IsLensValid = HasCapability(capabilities, CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris),
                    IsDegraded = false,
                    IsFallbackMode = false
                },
                RawFreeD = new FreeDRawPacketValues
                {
                    MessageType = FreeDMessageType.CameraPositionAndOrientation,
                    Pan = rawPan,
                    Tilt = rawTilt,
                    Roll = rawRoll,
                    X = rawX,
                    Y = rawY,
                    Z = rawZ,
                    Zoom = rawZoom,
                    Focus = rawFocus,
                    UserData = userArea,
                    Checksum = packet[28]
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

        private static double DecodeLensField(FreeDUnsignedFieldDecoder decoder, int encoded)
        {
            return decoder != null ? decoder.Decode(encoded) : 0d;
        }

        private static bool HasCapability(CameraCapabilities capabilities, CameraCapabilities requested)
        {
            return (capabilities & requested) != 0;
        }
    }

    public enum FreeDPacketFailureReason
    {
        None = 0,
        InvalidLength = 1,
        UnsupportedMessageType = 2,
        InvalidMessageType = UnsupportedMessageType,
        ChecksumMismatch = 3,
        CameraIdFiltered = 4,
        InvalidDecodingProfile = 5
    }

    public enum FreeDPacketDecodingPreset
    {
        SyncFreeDPhysicalV1 = 0,
        RawUnsigned24 = 1,
        BuiltInDeviceProfile = 2,
        Custom = 3
    }

    public enum FreeDUnsignedFieldDecodingMode
    {
        Disabled = 0,
        ScaleAndOffset = 1,
        NormalizeUnsigned24 = 2,
        Reciprocal = 3,
        PiecewiseLinear = 4
    }

    public enum FreeDUserDataDecodingMode
    {
        RawOnly = 0,
        IrisX100AndFrameModulo16 = 1
    }

    [Serializable]
    public sealed class FreeDUnsignedFieldDecoder
    {
        public FreeDUnsignedFieldDecodingMode Mode;
        public double Scale = 1d;
        public double Offset;
        public double ReciprocalNumerator = 1d;
        public double ReciprocalOffset;
        public CurveDefinition Curve = new CurveDefinition();

        public double Decode(int rawValue)
        {
            switch (Mode)
            {
                case FreeDUnsignedFieldDecodingMode.ScaleAndOffset:
                    return (rawValue * Scale) + Offset;
                case FreeDUnsignedFieldDecodingMode.NormalizeUnsigned24:
                    return rawValue / 16777215d;
                case FreeDUnsignedFieldDecodingMode.Reciprocal:
                    var denominator = rawValue - ReciprocalOffset;
                    return denominator > 0d ? ReciprocalNumerator / denominator : 0d;
                case FreeDUnsignedFieldDecodingMode.PiecewiseLinear:
                    return Curve != null ? Curve.Evaluate(rawValue) : 0d;
                case FreeDUnsignedFieldDecodingMode.Disabled:
                default:
                    return 0d;
            }
        }
    }

    [Serializable]
    public sealed class FreeDPacketDecodingProfile
    {
        public string ProfileName = string.Empty;
        public CameraCapabilities Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris | CameraCapabilities.ExternalTracking;
        public FreeDUnsignedFieldDecoder ZoomNormalized = new FreeDUnsignedFieldDecoder();
        public FreeDUnsignedFieldDecoder FocalLengthMm = new FreeDUnsignedFieldDecoder();
        public FreeDUnsignedFieldDecoder EffectiveFocalLengthMm = new FreeDUnsignedFieldDecoder();
        public FreeDUnsignedFieldDecoder FocusNormalized = new FreeDUnsignedFieldDecoder();
        public FreeDUnsignedFieldDecoder FocusDistanceMeters = new FreeDUnsignedFieldDecoder();
        public FreeDUserDataDecodingMode UserData = FreeDUserDataDecodingMode.RawOnly;
        public double SensorWidthMm;
        public double SensorHeightMm;
    }

    public static class FreeDPacketDecodingProfiles
    {
        public static FreeDPacketDecodingProfile Create(FreeDPacketDecodingPreset preset, string builtInProfileId = null)
        {
            switch (preset)
            {
                case FreeDPacketDecodingPreset.RawUnsigned24:
                    return CreateRawUnsigned24();
                case FreeDPacketDecodingPreset.BuiltInDeviceProfile:
                    return FreeDBuiltInPacketDecodingProfiles.Create(builtInProfileId);
                case FreeDPacketDecodingPreset.Custom:
                    return new FreeDPacketDecodingProfile { ProfileName = "Custom" };
                case FreeDPacketDecodingPreset.SyncFreeDPhysicalV1:
                    return CreateSyncFreeDPhysicalV1();
                default:
                    return CreateFailClosedRaw();
            }
        }

        public static FreeDPacketDecodingProfile CreateFailClosedRaw()
        {
            return new FreeDPacketDecodingProfile
            {
                ProfileName = "Invalid Profile Raw Capture",
                Capabilities = CameraCapabilities.None,
                UserData = FreeDUserDataDecodingMode.RawOnly
            };
        }

        private static FreeDPacketDecodingProfile CreateSyncFreeDPhysicalV1()
        {
            return new FreeDPacketDecodingProfile
            {
                ProfileName = "SyncFreeD Physical V1",
                FocalLengthMm = CreateScaleDecoder(0.001d),
                FocusDistanceMeters = CreateReciprocalDecoder(1 << 18, 0d),
                UserData = FreeDUserDataDecodingMode.IrisX100AndFrameModulo16
            };
        }

        private static FreeDPacketDecodingProfile CreateRawUnsigned24()
        {
            return new FreeDPacketDecodingProfile
            {
                ProfileName = "Raw Free-D D1 Inspection",
                Capabilities = CameraCapabilities.None,
                UserData = FreeDUserDataDecodingMode.RawOnly
            };
        }

        private static FreeDUnsignedFieldDecoder CreateScaleDecoder(double scale)
        {
            return new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.ScaleAndOffset, Scale = scale };
        }

        private static FreeDUnsignedFieldDecoder CreateNormalizedDecoder()
        {
            return new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.NormalizeUnsigned24 };
        }

        private static FreeDUnsignedFieldDecoder CreateReciprocalDecoder(double numerator, double offset)
        {
            return new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.Reciprocal, ReciprocalNumerator = numerator, ReciprocalOffset = offset };
        }

        internal static FreeDUnsignedFieldDecoder CreateCurveDecoder(params CurveKeyframe[] keys)
        {
            return new FreeDUnsignedFieldDecoder { Mode = FreeDUnsignedFieldDecodingMode.PiecewiseLinear, Curve = new CurveDefinition { Keys = keys } };
        }
    }

    public static class FreeDPacketDecodingProfileValidator
    {
        public static bool TryValidate(FreeDPacketDecodingProfile profile, out string error)
        {
            if (profile == null)
            {
                error = "Packet decoding profile is null.";
                return false;
            }

            if (!IsFiniteNonNegative(profile.SensorWidthMm) || !IsFiniteNonNegative(profile.SensorHeightMm) || (profile.SensorWidthMm == 0d) != (profile.SensorHeightMm == 0d))
            {
                error = "Sensor width and height must both be zero or finite positive values.";
                return false;
            }

            var hasZoomCapability = HasCapability(profile.Capabilities, CameraCapabilities.Zoom);
            var hasFocusCapability = HasCapability(profile.Capabilities, CameraCapabilities.Focus);
            var hasIrisCapability = HasCapability(profile.Capabilities, CameraCapabilities.Iris);
            var hasZoomDecoder = IsEnabled(profile.ZoomNormalized) || IsEnabled(profile.FocalLengthMm) || IsEnabled(profile.EffectiveFocalLengthMm);
            var hasFocusDecoder = IsEnabled(profile.FocusNormalized) || IsEnabled(profile.FocusDistanceMeters);
            if (hasZoomCapability != hasZoomDecoder)
            {
                error = "Zoom capability and Zoom decoders must be enabled or disabled together.";
                return false;
            }

            if (hasFocusCapability != hasFocusDecoder)
            {
                error = "Focus capability and Focus decoders must be enabled or disabled together.";
                return false;
            }

            if (hasIrisCapability && profile.UserData != FreeDUserDataDecodingMode.IrisX100AndFrameModulo16)
            {
                error = "Iris capability requires IrisX100AndFrameModulo16 user-data decoding.";
                return false;
            }

            if ((profile.SensorWidthMm > 0d || profile.SensorHeightMm > 0d) && !hasZoomCapability)
            {
                error = "Sensor projection values require Zoom capability.";
                return false;
            }

            if (!TryValidateDecoder(profile.ZoomNormalized, nameof(profile.ZoomNormalized), out error)
                || !TryValidateDecoder(profile.FocalLengthMm, nameof(profile.FocalLengthMm), out error)
                || !TryValidateDecoder(profile.EffectiveFocalLengthMm, nameof(profile.EffectiveFocalLengthMm), out error)
                || !TryValidateDecoder(profile.FocusNormalized, nameof(profile.FocusNormalized), out error)
                || !TryValidateDecoder(profile.FocusDistanceMeters, nameof(profile.FocusDistanceMeters), out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool TryValidateDecoder(FreeDUnsignedFieldDecoder decoder, string fieldName, out string error)
        {
            if (decoder == null)
            {
                error = $"{fieldName} decoder is null.";
                return false;
            }

            if (decoder.Mode == FreeDUnsignedFieldDecodingMode.ScaleAndOffset && (!IsFinite(decoder.Scale) || !IsFinite(decoder.Offset)))
            {
                error = $"{fieldName} scale and offset must be finite.";
                return false;
            }

            if (decoder.Mode == FreeDUnsignedFieldDecodingMode.Reciprocal && (!IsFinite(decoder.ReciprocalNumerator) || decoder.ReciprocalNumerator <= 0d || !IsFinite(decoder.ReciprocalOffset)))
            {
                error = $"{fieldName} reciprocal parameters are invalid.";
                return false;
            }

            if (decoder.Mode != FreeDUnsignedFieldDecodingMode.PiecewiseLinear)
            {
                error = string.Empty;
                return true;
            }

            var keys = decoder.Curve?.Keys;
            if (keys == null || keys.Length == 0)
            {
                error = $"{fieldName} curve requires at least one key.";
                return false;
            }

            var previousTime = double.NegativeInfinity;
            for (var index = 0; index < keys.Length; index++)
            {
                var key = keys[index];
                if (!IsFinite(key.Time) || !IsFinite(key.Value) || key.Time < 0d || key.Time > 0xFFFFFF || key.Time <= previousTime)
                {
                    error = $"{fieldName} curve keys must use unique ascending raw values from 0 to 0xFFFFFF and finite outputs.";
                    return false;
                }

                previousTime = key.Time;
            }

            error = string.Empty;
            return true;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0d;
        }

        private static bool IsEnabled(FreeDUnsignedFieldDecoder decoder)
        {
            return decoder != null && decoder.Mode != FreeDUnsignedFieldDecodingMode.Disabled;
        }

        private static bool HasCapability(CameraCapabilities capabilities, CameraCapabilities requested)
        {
            return (capabilities & requested) != 0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
