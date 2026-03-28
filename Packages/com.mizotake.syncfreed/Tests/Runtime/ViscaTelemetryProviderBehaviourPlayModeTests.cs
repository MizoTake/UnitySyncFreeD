using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class ViscaTelemetryProviderBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator TryGetTelemetry_ReturnsAppliedFrame()
        {
            var root = new GameObject("Visca Telemetry Provider");
            var provider = root.AddComponent<ViscaTelemetryProviderBehaviour>();
            var frame = new ViscaTelemetryFrame
            {
                ObservedPose = new PoseState { PanDeg = 42d, TimestampTicks = System.DateTime.UtcNow.Ticks },
                Lens = new LensState { FocalLengthMm = 75d, FocusDistanceMeters = 4d, IrisFNumber = 3.2d },
                Timing = new TimingState { FrameModulo16 = 7, TrackingDelayMs = 11 },
                Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
            };

            provider.ApplyTelemetry(frame);

            yield return null;

            Assert.That(provider.TryGetTelemetry(out var result), Is.True);
            Assert.That(result.ObservedPose.PanDeg, Is.EqualTo(42d).Within(0.0001d));
            Assert.That(result.Lens.FocalLengthMm, Is.EqualTo(75d).Within(0.0001d));

            Object.Destroy(root);
        }
    }
}
