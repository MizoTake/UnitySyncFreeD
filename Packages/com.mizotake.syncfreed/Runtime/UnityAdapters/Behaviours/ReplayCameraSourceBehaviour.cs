using System;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class ReplayCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider
    {
        [SerializeField] private string sourceId = "replay";
        [SerializeField] private int cameraId = 3;
        [SerializeField] private bool loop = true;
        [SerializeField] private ReplayPoseKeyframe[] keyframes = new[]
        {
            new ReplayPoseKeyframe { TimeSeconds = 0f, Position = new Vector3(0f, 1f, -10f), Rotation = Vector3.zero, FocalLengthMm = 35f },
            new ReplayPoseKeyframe { TimeSeconds = 1f, Position = new Vector3(1f, 1.2f, -9f), Rotation = new Vector3(0f, 20f, 0f), FocalLengthMm = 40f },
            new ReplayPoseKeyframe { TimeSeconds = 2f, Position = new Vector3(-1f, 1.1f, -8f), Rotation = new Vector3(0f, -20f, 0f), FocalLengthMm = 50f }
        };

        public string SourceId => sourceId;

        public int CameraId => cameraId;

        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

        public bool TryGetObservedState(out CameraObservedFrame frame)
        {
            return TryGetObservedFrame(out frame);
        }

        public bool TryGetObservedFrame(out CameraObservedFrame frame)
        {
            var sample = EvaluateCurrentSample();
            frame = new CameraObservedFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Capabilities = Capabilities,
                Pose = sample.Pose,
                Lens = sample.Lens,
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
            };
            return keyframes != null && keyframes.Length > 0;
        }

        public CameraCommandFrame CaptureCommandFrame()
        {
            var sample = EvaluateCurrentSample();
            return new CameraCommandFrame
            {
                SourceId = sourceId,
                CameraId = cameraId,
                Pose = sample.Pose,
                Lens = sample.Lens,
                Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) }
            };
        }

        private (PoseState Pose, LensState Lens) EvaluateCurrentSample()
        {
            if (keyframes == null || keyframes.Length == 0)
            {
                return default;
            }

            if (keyframes.Length == 1)
            {
                return Convert(keyframes[0]);
            }

            var duration = keyframes[keyframes.Length - 1].TimeSeconds;
            var time = Time.time;
            if (loop && duration > 0f)
            {
                time %= duration;
            }
            else
            {
                time = Mathf.Min(time, duration);
            }

            for (var index = 1; index < keyframes.Length; index++)
            {
                var previous = keyframes[index - 1];
                var next = keyframes[index];
                if (time > next.TimeSeconds)
                {
                    continue;
                }

                var range = Mathf.Max(0.0001f, next.TimeSeconds - previous.TimeSeconds);
                var t = Mathf.Clamp01((time - previous.TimeSeconds) / range);
                var position = Vector3.Lerp(previous.Position, next.Position, t);
                var rotation = Vector3.Lerp(previous.Rotation, next.Rotation, t);
                var focalLength = Mathf.Lerp(previous.FocalLengthMm, next.FocalLengthMm, t);
                return Convert(position, rotation, focalLength);
            }

            return Convert(keyframes[keyframes.Length - 1]);
        }

        private static (PoseState Pose, LensState Lens) Convert(ReplayPoseKeyframe keyframe)
        {
            return Convert(keyframe.Position, keyframe.Rotation, keyframe.FocalLengthMm);
        }

        private static (PoseState Pose, LensState Lens) Convert(Vector3 position, Vector3 rotation, float focalLengthMm)
        {
            var pose = new PoseState
            {
                PanDeg = NormalizeSignedAngle(rotation.y),
                TiltDeg = NormalizeSignedAngle(-rotation.x),
                RollDeg = NormalizeSignedAngle(rotation.z),
                Xmm = position.x * 1000d,
                Ymm = position.z * 1000d,
                Zmm = position.y * 1000d,
                TimestampTicks = DateTime.UtcNow.Ticks
            };
            var lens = new LensState
            {
                FocalLengthMm = focalLengthMm,
                FocusDistanceMeters = 2d,
                IrisFNumber = 2.8d
            };
            return (pose, lens);
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }
    }
}
