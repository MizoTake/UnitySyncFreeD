using MizoTake.SyncFreeD.Core.Models;

namespace MizoTake.SyncFreeD.Core.Outputs
{
    public static class FreeDBuiltInPacketDecodingProfileIds
    {
        public const string SonyBrcX1000Firmware210 = "sony-brc-x1000-fw-2.10";
    }

    public static class FreeDBuiltInPacketDecodingProfiles
    {
        public static FreeDPacketDecodingProfile Create(string profileId)
        {
            switch (profileId)
            {
                case FreeDBuiltInPacketDecodingProfileIds.SonyBrcX1000Firmware210:
                    return CreateSonyBrcX1000Firmware210();
                default:
                    return null;
            }
        }

        private static FreeDPacketDecodingProfile CreateSonyBrcX1000Firmware210()
        {
            return new FreeDPacketDecodingProfile
            {
                ProfileName = "Sony BRC-X1000 Free-D (Firmware 2.10)",
                Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris | CameraCapabilities.ExternalTracking,
                ZoomNormalized = FreeDPacketDecodingProfiles.CreateCurveDecoder(new CurveKeyframe(0x0000, 0d), new CurveKeyframe(0x4000, 1d)),
                FocalLengthMm = FreeDPacketDecodingProfiles.CreateCurveDecoder(
                    new CurveKeyframe(0x0000, 9.3d),
                    new CurveKeyframe(0x1800, 18.6d),
                    new CurveKeyframe(0x2340, 27.9d),
                    new CurveKeyframe(0x2A40, 37.2d),
                    new CurveKeyframe(0x2F00, 46.5d),
                    new CurveKeyframe(0x3300, 55.8d),
                    new CurveKeyframe(0x3600, 65.1d),
                    new CurveKeyframe(0x3880, 74.4d),
                    new CurveKeyframe(0x3AC0, 83.7d),
                    new CurveKeyframe(0x3CC0, 93d),
                    new CurveKeyframe(0x3E80, 102.3d),
                    new CurveKeyframe(0x4000, 111.6d)),
                EffectiveFocalLengthMm = FreeDPacketDecodingProfiles.CreateCurveDecoder(
                    new CurveKeyframe(0x0000, 9.3d),
                    new CurveKeyframe(0x1800, 18.6d),
                    new CurveKeyframe(0x2340, 27.9d),
                    new CurveKeyframe(0x2A40, 37.2d),
                    new CurveKeyframe(0x2F00, 46.5d),
                    new CurveKeyframe(0x3300, 55.8d),
                    new CurveKeyframe(0x3600, 65.1d),
                    new CurveKeyframe(0x3880, 74.4d),
                    new CurveKeyframe(0x3AC0, 83.7d),
                    new CurveKeyframe(0x3CC0, 93d),
                    new CurveKeyframe(0x3E80, 102.3d),
                    new CurveKeyframe(0x4000, 111.6d),
                    new CurveKeyframe(0x5580, 167.4d),
                    new CurveKeyframe(0x6000, 223.2d)),
                FocusDistanceMeters = FreeDPacketDecodingProfiles.CreateCurveDecoder(
                    new CurveKeyframe(0x1000, 1000d),
                    new CurveKeyframe(0x2000, 5d),
                    new CurveKeyframe(0x3000, 3d),
                    new CurveKeyframe(0x4000, 2d),
                    new CurveKeyframe(0x5000, 1.5d),
                    new CurveKeyframe(0x6000, 1.2d),
                    new CurveKeyframe(0x7000, 1d),
                    new CurveKeyframe(0x8000, 0.8d),
                    new CurveKeyframe(0x9000, 0.55d),
                    new CurveKeyframe(0xA000, 0.35d),
                    new CurveKeyframe(0xB000, 0.25d),
                    new CurveKeyframe(0xC000, 0.18d),
                    new CurveKeyframe(0xD000, 0.14d),
                    new CurveKeyframe(0xE000, 0.1d),
                    new CurveKeyframe(0xF000, 0.08d)),
                UserData = FreeDUserDataDecodingMode.IrisX100AndFrameModulo16,
                SensorWidthMm = 11.758431894134853d,
                SensorHeightMm = 6.614117940450855d
            };
        }
    }
}
