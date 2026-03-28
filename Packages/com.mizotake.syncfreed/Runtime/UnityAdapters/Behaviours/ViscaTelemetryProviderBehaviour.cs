using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class ViscaTelemetryProviderBehaviour : MonoBehaviour, IViscaTelemetryProvider
    {
        [SerializeField] private bool hasTelemetry;
        [SerializeField] private bool expireTelemetry = true;
        [SerializeField] private int telemetryTimeoutMs = 500;
        [SerializeField] private PoseState observedPose;
        [SerializeField] private LensState observedLens = new LensState { FocalLengthMm = 40d, FocusDistanceMeters = 3d, IrisFNumber = 2.8d };
        [SerializeField] private TimingState timing;
        [SerializeField] private ValidityState validity = new ValidityState { IsTrackingValid = true, IsLensValid = true };

        private long lastUpdateTicks;

        public bool HasTelemetry => hasTelemetry;

        public bool TryGetTelemetry(out ViscaTelemetryFrame frame)
        {
            if (!hasTelemetry || IsExpired())
            {
                frame = default;
                return false;
            }

            frame = new ViscaTelemetryFrame
            {
                ObservedPose = observedPose,
                Lens = observedLens,
                Timing = timing,
                Validity = validity
            };
            return true;
        }

        public void ApplyTelemetry(in ViscaTelemetryFrame frame)
        {
            observedPose = frame.ObservedPose;
            observedLens = frame.Lens;
            timing = frame.Timing;
            validity = frame.Validity;
            hasTelemetry = true;
            lastUpdateTicks = DateTime.UtcNow.Ticks;
        }

        public void ApplyObservedPose(in PoseState pose)
        {
            observedPose = pose;
            hasTelemetry = true;
            lastUpdateTicks = DateTime.UtcNow.Ticks;
        }

        public void ApplyLens(in LensState lens)
        {
            observedLens = lens;
            hasTelemetry = true;
            lastUpdateTicks = DateTime.UtcNow.Ticks;
        }

        public void ApplyTiming(in TimingState timingState)
        {
            timing = timingState;
            hasTelemetry = true;
            lastUpdateTicks = DateTime.UtcNow.Ticks;
        }

        public void ApplyValidity(in ValidityState validityState)
        {
            validity = validityState;
            hasTelemetry = true;
            lastUpdateTicks = DateTime.UtcNow.Ticks;
        }

        public void ClearTelemetry()
        {
            hasTelemetry = false;
        }

        private bool IsExpired()
        {
            if (!expireTelemetry || telemetryTimeoutMs <= 0 || lastUpdateTicks == 0L)
            {
                return false;
            }

            var elapsedTicks = DateTime.UtcNow.Ticks - lastUpdateTicks;
            return elapsedTicks > (telemetryTimeoutMs * TimeSpan.TicksPerMillisecond);
        }
    }
}
