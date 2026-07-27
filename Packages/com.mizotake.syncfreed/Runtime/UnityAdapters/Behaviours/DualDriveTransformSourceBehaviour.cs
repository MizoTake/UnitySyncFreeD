using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class DualDriveTransformSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        [SerializeField] private string sourceId = "dual-drive";
        [SerializeField] private int cameraId = 4;
        [SerializeField] private Transform commandTransform;
        [SerializeField] private Transform observedTransform;
        [SerializeField] private float focalLengthMm = 40f;
        [SerializeField] private float focusDistanceMeters = 3f;
        [SerializeField] private float irisFNumber = 2.8f;

        public string SourceId => sourceId;
        public int CameraId => cameraId;
        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

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
            ResolveReferences();
            frame = new CameraObservedFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Capabilities = Capabilities,
                Pose = CapturePose(observedTransform),
                Lens = CaptureLens(),
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                Validity = new ValidityState
                {
                    IsTrackingValid = true,
                    IsLensValid = true,
                    IsDegraded = false,
                    IsFallbackMode = false
                }
            };
            return true;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            ResolveReferences();
            return new CameraCommandFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Pose = CapturePose(commandTransform),
                Lens = CaptureLens(),
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) }
            };
        }

        public bool TryGetLensState(out LensState lens)
        {
            lens = CaptureLens();
            return true;
        }

        private void ResolveReferences()
        {
            if (commandTransform == null)
            {
                commandTransform = transform.Find("Command Target");
            }

            if (observedTransform == null)
            {
                observedTransform = transform.Find("Observed Target");
            }

            if (commandTransform == null)
            {
                commandTransform = transform;
            }

            if (observedTransform == null)
            {
                observedTransform = commandTransform != null ? commandTransform : transform;
            }
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

        private static PoseState CapturePose(Transform targetTransform)
        {
            var euler = targetTransform.rotation.eulerAngles;
            return new PoseState
            {
                PanDeg = NormalizeSignedAngle(euler.y),
                TiltDeg = NormalizeSignedAngle(-euler.x),
                RollDeg = NormalizeSignedAngle(euler.z),
                Xmm = targetTransform.position.x * 1000d,
                Ymm = targetTransform.position.z * 1000d,
                Zmm = targetTransform.position.y * 1000d,
                TimestampTicks = DateTime.UtcNow.Ticks
            };
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
    }
}
