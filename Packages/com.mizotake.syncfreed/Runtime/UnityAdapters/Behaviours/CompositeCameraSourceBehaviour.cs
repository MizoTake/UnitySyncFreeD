using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class CompositeCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        private const CameraCapabilities LensCapabilities = CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;
        [SerializeField] private string sourceId = "composite-source";
        [SerializeField] private int cameraId = 5;
        [SerializeField] private MonoBehaviour poseSourceBehaviour;
        [SerializeField] private MonoBehaviour lensSourceBehaviour;
        [SerializeField] private Transform fallbackPoseTransform;
        [SerializeField] private bool usePoseSourceCameraId = true;
        [SerializeField] private bool treatMissingPoseAsTrackingInvalid = true;

        public string SourceId => sourceId;
        public int CameraId => cameraId;
        public CameraCapabilities Capabilities
        {
            get
            {
                var capabilities = ResolvePoseSource()?.Capabilities ?? CameraCapabilities.None;
                var lensSource = ResolveLensSource();
                if (lensSource != null)
                {
                    capabilities &= ~LensCapabilities;
                    capabilities |= lensSourceBehaviour is ICameraFrameProvider lensFrameProvider ? lensFrameProvider.Capabilities & LensCapabilities : LensCapabilities;
                }

                return capabilities;
            }
        }

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
            var poseSource = ResolvePoseSource();
            var poseFrame = default(CameraObservedFrame);
            var hasPose = poseSource != null && poseSource.TryGetObservedFrame(out poseFrame);
            var hasLens = TryResolveLensState(hasPose, poseFrame, out var lens, out var lensFrame, out var hasLensFrame);
            if (!hasPose && !hasLens)
            {
                frame = default;
                return false;
            }

            var validity = hasPose ? poseFrame.Validity : new ValidityState
            {
                IsTrackingValid = !treatMissingPoseAsTrackingInvalid,
                IsLensValid = false,
                IsDegraded = treatMissingPoseAsTrackingInvalid,
                IsFallbackMode = treatMissingPoseAsTrackingInvalid
            };
            if (hasLens)
            {
                validity.IsLensValid = true;
                validity.IsFallbackMode = validity.IsFallbackMode || !hasPose;
                validity.IsDegraded = !validity.IsTrackingValid || !validity.IsLensValid;
            }

            frame = new CameraObservedFrame
            {
                SourceId = sourceId,
                CameraId = hasPose && usePoseSourceCameraId ? poseFrame.CameraId : cameraId,
                Capabilities = Capabilities,
                Pose = hasPose ? poseFrame.Pose : CapturePose(fallbackPoseTransform != null ? fallbackPoseTransform : transform),
                Lens = hasLens ? lens : poseFrame.Lens,
                Projection = hasLensFrame ? lensFrame.Projection : poseFrame.Projection,
                Timing = hasLensFrame ? lensFrame.Timing : hasPose ? poseFrame.Timing : new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                Validity = validity,
                RawFreeD = hasLensFrame ? lensFrame.RawFreeD : poseFrame.RawFreeD
            };
            return true;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            ResolveReferences();
            var poseSource = ResolvePoseSource();
            var commandFrame = poseSource != null ? poseSource.CaptureCommandFrame() : new CameraCommandFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Pose = CapturePose(fallbackPoseTransform != null ? fallbackPoseTransform : transform),
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) }
            };
            if (TryGetLensState(out var lens))
            {
                commandFrame.Lens = lens;
            }

            commandFrame.SourceId = sourceId;
            commandFrame.CameraId = poseSource != null && usePoseSourceCameraId ? commandFrame.CameraId : cameraId;
            return commandFrame;
        }

        public bool TryGetLensState(out LensState lens)
        {
            ResolveReferences();
            var lensSource = ResolveLensSource();
            if (lensSource != null)
            {
                return lensSource.TryGetLensState(out lens);
            }

            var poseSource = ResolvePoseSource();
            if (poseSource != null && poseSource.TryGetObservedFrame(out var poseFrame) && poseFrame.Validity.IsLensValid)
            {
                lens = poseFrame.Lens;
                return true;
            }

            lens = default;
            return false;
        }

        private bool TryResolveLensState(bool hasPose, in CameraObservedFrame poseFrame, out LensState lens, out CameraObservedFrame lensFrame, out bool hasLensFrame)
        {
            var lensSource = ResolveLensSource();
            if (lensSource != null)
            {
                if (ReferenceEquals(lensSourceBehaviour, poseSourceBehaviour) && hasPose && poseFrame.Validity.IsLensValid)
                {
                    lens = poseFrame.Lens;
                    lensFrame = poseFrame;
                    hasLensFrame = true;
                    return true;
                }

                if (lensSource.TryGetLensState(out lens))
                {
                    lensFrame = default;
                    hasLensFrame = lensSourceBehaviour is ICameraFrameProvider lensFrameProvider && lensFrameProvider.TryGetObservedFrame(out lensFrame);
                    return true;
                }

                lens = default;
                lensFrame = default;
                hasLensFrame = false;
                return false;
            }

            if (hasPose && poseFrame.Validity.IsLensValid)
            {
                lens = poseFrame.Lens;
                lensFrame = poseFrame;
                hasLensFrame = true;
                return true;
            }

            lens = default;
            lensFrame = default;
            hasLensFrame = false;
            return false;
        }

        private ICameraFrameProvider ResolvePoseSource()
        {
            return poseSourceBehaviour as ICameraFrameProvider;
        }

        private ILensDataSource ResolveLensSource()
        {
            if (lensSourceBehaviour is ILensDataSource lensSource)
            {
                return lensSource;
            }

            return null;
        }

        private void ResolveReferences()
        {
            if (fallbackPoseTransform == null)
            {
                fallbackPoseTransform = transform;
            }

            if (poseSourceBehaviour == null)
            {
                poseSourceBehaviour = GetComponent<TrackerCameraSourceBehaviour>();
            }

            if (poseSourceBehaviour == null)
            {
                poseSourceBehaviour = GetComponent<UnityCameraSourceBehaviour>();
            }

            if (poseSourceBehaviour == null)
            {
                poseSourceBehaviour = GetComponent<ReplayCameraSourceBehaviour>();
            }

            if (lensSourceBehaviour == null)
            {
                lensSourceBehaviour = GetComponent<UnityCameraSourceBehaviour>();
            }

            if (lensSourceBehaviour == null)
            {
                lensSourceBehaviour = GetComponent<ReplayCameraSourceBehaviour>();
            }

            if (lensSourceBehaviour == null)
            {
                lensSourceBehaviour = GetComponent<TrackerCameraSourceBehaviour>();
            }

            if (lensSourceBehaviour == null)
            {
                lensSourceBehaviour = GetComponent<LensEncoderSourceBehaviour>();
            }
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
