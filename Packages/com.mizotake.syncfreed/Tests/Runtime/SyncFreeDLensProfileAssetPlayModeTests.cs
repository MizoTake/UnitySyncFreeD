using System.Collections;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDLensProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_AppliesLensProfileClampToCorrectedLens()
        {
            var root = new GameObject("SyncFreeD Lens Profile Camera");
            root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            var source = root.AddComponent<FakeLensProfileSourceBehaviour>();
            root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            var asset = ScriptableObject.CreateInstance<LensProfileAsset>();
            asset.Value = new LensProfile { MinFocalLengthMm = 20d, MaxFocalLengthMm = 80d };
            SetPrivateField(sync, "sourceBehaviour", source);
            SetPrivateField(sync, "lensProfileAsset", asset);

            yield return null;

            Assert.That(sync.LastState.CorrectedLens.FocalLengthMm, Is.EqualTo(80d).Within(0.0001d));
            Assert.That(sync.LastState.Lens.FocalLengthMm, Is.EqualTo(80d).Within(0.0001d));

            Object.Destroy(asset);
            Object.Destroy(root);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class FakeLensProfileSourceBehaviour : MonoBehaviour, ICameraFrameProvider
        {
            public string SourceId => "fake-lens-profile";
            public int CameraId => 1;
            public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Zoom | CameraCapabilities.Focus | CameraCapabilities.Iris;

            public bool TryGetObservedState(out CameraObservedFrame frame)
            {
                return TryGetObservedFrame(out frame);
            }

            public bool TryGetObservedFrame(out CameraObservedFrame frame)
            {
                frame = CreateFrame();
                return true;
            }

            public CameraCommandFrame CaptureCommandFrame()
            {
                var frame = CreateFrame();
                return new CameraCommandFrame
                {
                    SourceId = frame.SourceId,
                    CameraId = frame.CameraId,
                    Pose = frame.Pose,
                    Lens = frame.Lens,
                    Timing = frame.Timing
                };
            }

            private CameraObservedFrame CreateFrame()
            {
                return new CameraObservedFrame
                {
                    SourceId = SourceId,
                    CameraId = CameraId,
                    Capabilities = Capabilities,
                    Pose = new PoseState { TimestampTicks = System.DateTime.UtcNow.Ticks },
                    Lens = new LensState { FocalLengthMm = 120d, FocusDistanceMeters = 3d, IrisFNumber = 2.8d },
                    Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                };
            }
        }
    }
}
