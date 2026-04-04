using System;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using UnityEngine;

namespace MizoTake.SyncFreeD.UnityAdapters.Behaviours
{
    [DisallowMultipleComponent]
    public sealed class ReplayCameraSourceBehaviour : MonoBehaviour, ICameraFrameProvider, ILensDataSource
    {
        [SerializeField] private string sourceId = "replay";
        [SerializeField] private int cameraId = 3;
        [SerializeField] private bool loop = true;
        [SerializeField] private TextAsset replayDataAsset;
        [SerializeField] [TextArea(4, 12)] private string replayDataText = string.Empty;
        [SerializeField] private ReplayDataFormat replayDataFormat = ReplayDataFormat.Auto;
        [SerializeField] private bool loadReplayDataOnEnable = true;
        [SerializeField] private bool manualFrameStep;
        [SerializeField] private bool showFrameStepGui = true;
        [SerializeField] private Rect frameStepRect = new Rect(16f, 208f, 360f, 88f);
        [SerializeField] private bool trackingValid = true;
        [SerializeField] private bool lensValid = true;
        [SerializeField] private ReplayPoseKeyframe[] keyframes = new[]
        {
            new ReplayPoseKeyframe { TimeSeconds = 0f, Position = new Vector3(0f, 1f, -10f), Rotation = Vector3.zero, FocalLengthMm = 35f },
            new ReplayPoseKeyframe { TimeSeconds = 1f, Position = new Vector3(1f, 1.2f, -9f), Rotation = new Vector3(0f, 20f, 0f), FocalLengthMm = 40f },
            new ReplayPoseKeyframe { TimeSeconds = 2f, Position = new Vector3(-1f, 1.1f, -8f), Rotation = new Vector3(0f, -20f, 0f), FocalLengthMm = 50f }
        };
        [SerializeField] private int currentFrameIndex;

        public string SourceId => sourceId;

        public int CameraId => cameraId;

        public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Roll | CameraCapabilities.Position | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

        public int FrameCount => keyframes != null ? keyframes.Length : 0;

        public int CurrentFrameIndex => currentFrameIndex;

        public bool ManualFrameStep => manualFrameStep;

        public string LastLoadError { get; private set; } = string.Empty;
        public int LastLoadedFrameCount => keyframes != null ? keyframes.Length : 0;

        private void Awake()
        {
            if (loadReplayDataOnEnable)
            {
                ReloadReplayData();
            }
        }

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
                Validity = new ValidityState { IsTrackingValid = trackingValid, IsLensValid = lensValid, IsDegraded = !trackingValid || !lensValid, IsFallbackMode = !trackingValid && lensValid }
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

        public bool ReloadReplayData()
        {
            var text = replayDataAsset != null ? replayDataAsset.text : replayDataText;
            if (string.IsNullOrWhiteSpace(text))
            {
                LastLoadError = string.Empty;
                return keyframes != null && keyframes.Length > 0;
            }

            return LoadReplayData(text, replayDataFormat);
        }

        public bool LoadReplayData()
        {
            return ReloadReplayData();
        }

        public bool LoadReplayData(string text, ReplayDataFormat format)
        {
            if (!ReplayPoseKeyframeParser.TryParse(text, format, out var parsedKeyframes, out var error))
            {
                LastLoadError = error;
                return false;
            }

            keyframes = parsedKeyframes;
            currentFrameIndex = 0;
            LastLoadError = string.Empty;
            return true;
        }

        public bool TryGetLensState(out LensState lens)
        {
            lens = EvaluateCurrentSample().Lens;
            return lensValid;
        }

        public void LoadFromJsonText(string text)
        {
            LoadReplayData(text, ReplayDataFormat.Json);
        }

        public void LoadFromCsvText(string text)
        {
            LoadReplayData(text, ReplayDataFormat.Csv);
        }

        public void SetPlaybackPaused(bool paused)
        {
            manualFrameStep = paused;
        }

        public bool StepForward()
        {
            return SetFrameIndex(currentFrameIndex + 1);
        }

        public bool StepBackward()
        {
            return SetFrameIndex(currentFrameIndex - 1);
        }

        public void StepForwardFrame()
        {
            StepForward();
        }

        public void StepBackwardFrame()
        {
            StepBackward();
        }

        public bool SetFrameIndex(int index)
        {
            if (keyframes == null || keyframes.Length == 0)
            {
                return false;
            }

            currentFrameIndex = Mathf.Clamp(index, 0, keyframes.Length - 1);
            return true;
        }

        public void SetCurrentFrameIndex(int index)
        {
            SetFrameIndex(index);
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

            if (manualFrameStep)
            {
                currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, keyframes.Length - 1);
                return Convert(keyframes[currentFrameIndex]);
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

        private void OnGUI()
        {
            if (!showFrameStepGui || !manualFrameStep || keyframes == null || keyframes.Length == 0)
            {
                return;
            }

            GUILayout.BeginArea(frameStepRect, GUI.skin.box);
            GUILayout.Label("Replay Frame Step");
            GUILayout.Label(string.Format("Frame {0}/{1} Asset {2}", currentFrameIndex + 1, keyframes.Length, replayDataAsset != null ? replayDataAsset.name : "InlineKeyframes"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Prev"))
            {
                StepBackward();
            }

            if (GUILayout.Button("Next"))
            {
                StepForward();
            }

            if (GUILayout.Button("Reload"))
            {
                ReloadReplayData();
            }

            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(LastLoadError))
            {
                GUILayout.Label(LastLoadError);
            }

            GUILayout.EndArea();
        }
    }
}
