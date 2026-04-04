using System.Collections;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncCorrectionCountPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_IncrementsCorrectionCountWhenCorrectionApplied()
        {
            var root = new GameObject("Correction Count Camera");
            root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            var source = root.AddComponent<FakeDualDriveSourceBehaviour>();
            source.ObservedPanDeg = 20d;
            root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "sourceBehaviour", source);
            SetPrivateField(sync, "syncMode", SyncMode.DualDrive);

            yield return null;

            Assert.That(sync.CorrectionAppliedCount, Is.GreaterThan(0));

            Object.Destroy(root);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class FakeDualDriveSourceBehaviour : MonoBehaviour, ICameraFrameProvider
        {
            public double ObservedPanDeg;

            public string SourceId => "fake-dualdrive";
            public int CameraId => 1;
            public CameraCapabilities Capabilities => CameraCapabilities.PanTilt | CameraCapabilities.Zoom;

            public bool TryGetObservedState(out CameraObservedFrame frame)
            {
                return TryGetObservedFrame(out frame);
            }

            public bool TryGetObservedFrame(out CameraObservedFrame frame)
            {
                frame = new CameraObservedFrame
                {
                    SourceId = SourceId,
                    CameraId = CameraId,
                    Capabilities = Capabilities,
                    Pose = new PoseState { PanDeg = ObservedPanDeg, TimestampTicks = System.DateTime.UtcNow.Ticks },
                    Lens = new LensState { FocalLengthMm = 35d },
                    Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) },
                    Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true }
                };
                return true;
            }

            public CameraCommandFrame CaptureCommandFrame()
            {
                return new CameraCommandFrame
                {
                    SourceId = SourceId,
                    CameraId = CameraId,
                    Pose = new PoseState { PanDeg = 0d, TimestampTicks = System.DateTime.UtcNow.Ticks },
                    Lens = new LensState { FocalLengthMm = 35d },
                    Timing = new TimingState { FrameModulo16 = (ushort)(Time.frameCount & 0x0F) }
                };
            }
        }
    }
}
