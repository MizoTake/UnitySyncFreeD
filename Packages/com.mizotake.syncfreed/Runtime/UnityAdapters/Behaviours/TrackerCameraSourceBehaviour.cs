using System;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class TrackerCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider
    {
        [SerializeField] private string sourceId = "external-tracker";
        [SerializeField] private int cameraId = 2;
        [SerializeField] private Transform trackedTransform;
        [SerializeField] private float focalLengthMm = 35f;
        [SerializeField] private float focusDistanceMeters = 2f;
        [SerializeField] private float irisFNumber = 2.8f;

        public string SourceId => sourceId;

        public int CameraId => cameraId;

        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.ExternalTracking | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

        public bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }

        public bool TryGetObservedFrame(out CameraObservedFrame frame)
        {
            var transformToUse = trackedTransform != null ? trackedTransform : transform;
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
            var transformToUse = trackedTransform != null ? trackedTransform : transform;
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
    }
}
