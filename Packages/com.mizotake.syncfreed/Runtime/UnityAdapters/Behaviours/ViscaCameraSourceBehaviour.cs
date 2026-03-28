using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class ViscaCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider
    {
        [SerializeField] private string sourceId = "visca-bridge";
        [SerializeField] private int cameraId = 4;
        [SerializeField] private Transform commandTransform;
        [SerializeField] private Transform observedTransform;
        [SerializeField] private MonoBehaviour telemetryProviderBehaviour;
        [SerializeField] private float focalLengthMm = 40f;
        [SerializeField] private float focusDistanceMeters = 3f;
        [SerializeField] private float irisFNumber = 2.8f;

        public string SourceId => sourceId;

        public int CameraId => cameraId;

        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;
        public IViscaTelemetryProvider TelemetryProvider => telemetryProviderBehaviour as IViscaTelemetryProvider;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        public bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }

        public bool TryGetObservedFrame(out CameraObservedFrame frame)
        {
            if (TelemetryProvider != null && TelemetryProvider.TryGetTelemetry(out var telemetryFrame))
            {
                frame = new CameraObservedFrame
                {
                    SourceId = sourceId,
                    CameraId = cameraId,
                    Capabilities = Capabilities,
                    Pose = telemetryFrame.ObservedPose,
                    Lens = telemetryFrame.Lens,
                    Timing = telemetryFrame.Timing,
                    Validity = telemetryFrame.Validity
                };
                return true;
            }

            var transformToUse = observedTransform != null ? observedTransform : transform;
            frame = new CameraObservedFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Capabilities = Capabilities,
                Pose = CapturePose(transformToUse),
                Lens = CaptureLens(),
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
            };
            return true;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            var transformToUse = commandTransform != null ? commandTransform : transform;
            return new CameraCommandFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Pose = CapturePose(transformToUse),
                Lens = CaptureLens(),
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) }
            };
        }

        private LensState CaptureLens()
        {
            return new LensState
            {
                FocalLengthMm = focalLengthMm,
                FocusDistanceMeters = focusDistanceMeters,
                IrisFNumber = irisFNumber
            };
        }

        private static PoseState CapturePose(Transform transformToUse)
        {
            var euler = transformToUse.rotation.eulerAngles;
            return new PoseState
            {
                PanDeg = NormalizeSignedAngle(euler.y),
                TiltDeg = NormalizeSignedAngle(-euler.x),
                RollDeg = NormalizeSignedAngle(euler.z),
                Xmm = transformToUse.position.x * 1000d,
                Ymm = transformToUse.position.z * 1000d,
                Zmm = transformToUse.position.y * 1000d,
                TimestampTicks = DateTime.UtcNow.Ticks
            };
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        private void ResolveReferences()
        {
            if (telemetryProviderBehaviour == null)
            {
                telemetryProviderBehaviour = GetComponent<ViscaTelemetryProviderBehaviour>();
            }

            if (commandTransform == null)
            {
                var child = transform.Find("VISCA Command Target");
                if (child != null)
                {
                    commandTransform = child;
                }
            }

            if (observedTransform == null)
            {
                var child = transform.Find("VISCA Observed Target");
                if (child != null)
                {
                    observedTransform = child;
                }
            }
        }
    }
}
