using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    public class UnityCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        [SerializeField] private string sourceId = "UnityCamera";
        [SerializeField] private int cameraId = 255;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float fallbackFocalLengthMm = 50f;
        [SerializeField] private float fallbackFocusDistanceMeters = 1f;
        [SerializeField] private float irisFNumber = 2.8f;
        [SerializeField] private bool trackingValid = true;
        [SerializeField] private bool lensValid = true;

        private Camera cachedCamera;
        private Transform cachedTransform;

        public string SourceId => sourceId;

        public int CameraId => cameraId;

        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

        public bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }

        public bool TryGetObservedFrame(out CameraObservedFrame frame)
        {
            var transformToUse = ResolveTransform();
            var cameraToUse = ResolveCamera();
            if (transformToUse == null)
            {
                frame = default;
                return false;
            }

            frame = new CameraObservedFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Capabilities = CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris,
                Pose = CapturePose(transformToUse),
                Lens = CaptureLens(cameraToUse),
                Timing = CaptureTiming(),
                Validity = new ValidityState { IsTrackingValid = trackingValid, IsLensValid = lensValid, IsDegraded = !trackingValid || !lensValid, IsFallbackMode = !trackingValid && lensValid }
            };
            return true;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            var transformToUse = ResolveTransform();
            var cameraToUse = ResolveCamera();
            return new CameraCommandFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Pose = CapturePose(transformToUse),
                Lens = CaptureLens(cameraToUse),
                Timing = CaptureTiming()
            };
        }

        public CameraSyncState CaptureState()
        {
            var command = CaptureCommandFrame();
            var observed = TryGetObservedFrame(out var frame) ? frame : default;

            return new CameraSyncState
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Command = command.Pose,
                Predicted = command.Pose,
                Observed = observed.Pose,
                Corrected = observed.Pose.TimestampTicks != 0L ? observed.Pose : command.Pose,
                CommandLens = command.Lens,
                PredictedLens = command.Lens,
                ObservedLens = observed.Lens,
                CorrectedLens = observed.Lens.FocalLengthMm != 0d ? observed.Lens : command.Lens,
                Lens = observed.Lens.FocalLengthMm != 0d ? observed.Lens : command.Lens,
                Timing = observed.Timing,
                Validity = observed.Validity
            };
        }

        public bool TryGetLensState(out LensState lens)
        {
            var cameraToUse = ResolveCamera();
            lens = CaptureLens(cameraToUse);
            return lensValid;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            cachedCamera = null;
            cachedTransform = null;
        }
#endif

        private Transform ResolveTransform()
        {
            if (targetTransform != null)
            {
                return targetTransform;
            }

            if (cachedTransform == null)
            {
                cachedTransform = transform;
            }

            return cachedTransform;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null)
            {
                return targetCamera;
            }

            if (cachedCamera == null)
            {
                cachedCamera = GetComponent<Camera>();
            }

            return cachedCamera;
        }

        private LensState CaptureLens(Camera cameraToUse)
        {
            return new LensState
            {
                IrisFNumber = irisFNumber,
                FocalLengthMm = cameraToUse != null ? cameraToUse.focalLength : fallbackFocalLengthMm,
                FocusDistanceMeters = fallbackFocusDistanceMeters
            };
        }

        private static TimingState CaptureTiming()
        {
            return new TimingState
            {
                FrameModulo16 = (ushort)(Time.frameCount & 0x0F)
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
                TimestampTicks = System.DateTime.UtcNow.Ticks
            };
        }

        private static float NormalizeSignedAngle(float angle)
        {
            var normalized = Mathf.Repeat(angle + 180f, 360f) - 180f;
            return normalized;
        }
    }
}
