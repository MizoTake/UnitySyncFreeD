using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Support;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDOutputStateApplicatorTests
    {
        [Test]
        public void Apply_ReturnsOriginalState_WhenMountProfileIsNull()
        {
            var state = CreateState();

            var adjusted = FreeDOutputStateApplicator.Apply(state, null, null);

            Assert.That(adjusted.Corrected.PanDeg, Is.EqualTo(state.Corrected.PanDeg));
            Assert.That(adjusted.Corrected.Xmm, Is.EqualTo(state.Corrected.Xmm));
            Assert.That(adjusted.Corrected.Ymm, Is.EqualTo(state.Corrected.Ymm));
            Assert.That(adjusted.Corrected.Zmm, Is.EqualTo(state.Corrected.Zmm));
        }

        [Test]
        public void Apply_AddsTrackingOriginAndSensorOffset_ToCorrectedPose()
        {
            var state = CreateState();
            state.Corrected.PanDeg = 90d;
            var profile = new MountProfile
            {
                TrackingOriginOffsetMm = new Vector3Data(10d, 20d, 30d),
                SensorOffsetMm = new Vector3Data(100d, 0d, 0d)
            };
            var firmware = new FirmwareBehaviorProfile
            {
                UsesImageSensorBasedPosition = true,
                UsesImageSensorBasedOrientation = true
            };

            var adjusted = FreeDOutputStateApplicator.Apply(state, profile, firmware);

            Assert.That(adjusted.Corrected.Xmm, Is.EqualTo(1010d).Within(0.01d));
            Assert.That(adjusted.Corrected.Ymm, Is.EqualTo(2120d).Within(0.01d));
            Assert.That(adjusted.Corrected.Zmm, Is.EqualTo(3030d).Within(0.01d));
        }

        [Test]
        public void Apply_AppliesRotationOffset_WhenFirmwareSupportsImageSensorOrientation()
        {
            var state = CreateState();
            var profile = new MountProfile
            {
                RotationOffsetDeg = new Vector3Data(0d, 0d, 90d)
            };
            var firmware = new FirmwareBehaviorProfile
            {
                UsesImageSensorBasedPosition = true,
                UsesImageSensorBasedOrientation = true
            };

            var adjusted = FreeDOutputStateApplicator.Apply(state, profile, firmware);

            Assert.That(adjusted.Corrected.PanDeg, Is.EqualTo(90d).Within(0.01d));
            Assert.That(adjusted.Corrected.TiltDeg, Is.EqualTo(0d).Within(0.01d));
            Assert.That(adjusted.Corrected.RollDeg, Is.EqualTo(0d).Within(0.01d));
        }

        [Test]
        public void Apply_IgnoresSensorPositionAndRotation_WhenFirmwareUsesLegacyOutput()
        {
            var state = CreateState();
            var profile = new MountProfile
            {
                TrackingOriginOffsetMm = new Vector3Data(10d, 20d, 30d),
                SensorOffsetMm = new Vector3Data(100d, 0d, 0d),
                RotationOffsetDeg = new Vector3Data(0d, 0d, 90d)
            };
            var firmware = new FirmwareBehaviorProfile
            {
                UsesImageSensorBasedPosition = false,
                UsesImageSensorBasedOrientation = false
            };

            var adjusted = FreeDOutputStateApplicator.Apply(state, profile, firmware);

            Assert.That(adjusted.Corrected.PanDeg, Is.EqualTo(0d).Within(0.01d));
            Assert.That(adjusted.Corrected.Xmm, Is.EqualTo(1010d).Within(0.01d));
            Assert.That(adjusted.Corrected.Ymm, Is.EqualTo(2020d).Within(0.01d));
            Assert.That(adjusted.Corrected.Zmm, Is.EqualTo(3030d).Within(0.01d));
        }

        private static CameraSyncState CreateState()
        {
            return new CameraSyncState
            {
                Corrected = new PoseState
                {
                    Xmm = 1000d,
                    Ymm = 2000d,
                    Zmm = 3000d,
                    TimestampTicks = 1234L
                }
            };
        }
    }
}
