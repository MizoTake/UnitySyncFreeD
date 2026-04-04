using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class CompositeCameraSourceBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator TryGetObservedFrame_UsesPoseFromTrackerAndLensFromUnityCamera()
        {
            var root = new GameObject("Composite Camera");
            var tracker = root.AddComponent<TrackerCameraSourceBehaviour>();
            var unitySource = root.AddComponent<UnityCameraSourceBehaviour>();
            var composite = root.AddComponent<CompositeCameraSourceBehaviour>();
            var unityCamera = root.AddComponent<Camera>();
            unityCamera.focalLength = 77f;
            root.transform.position = new Vector3(1f, 2f, 3f);
            root.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            SetPrivateField(composite, "poseSourceBehaviour", tracker);
            SetPrivateField(composite, "lensSourceBehaviour", unitySource);

            yield return null;

            Assert.That(composite.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.Pose.Xmm, Is.EqualTo(1000d).Within(0.001d));
            Assert.That(frame.Pose.Zmm, Is.EqualTo(2000d).Within(0.001d));
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(77d).Within(0.001d));
            Assert.That(frame.Validity.IsTrackingValid, Is.True);
            Assert.That(frame.Validity.IsLensValid, Is.True);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator TryGetObservedFrame_WithLensOnly_KeepsLensValidWhileTrackingInvalid()
        {
            var root = new GameObject("Fixed Camera Composite");
            var lens = root.AddComponent<LensEncoderSourceBehaviour>();
            var composite = root.AddComponent<CompositeCameraSourceBehaviour>();
            SetPrivateField(composite, "lensSourceBehaviour", lens);

            yield return null;

            Assert.That(composite.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.Validity.IsTrackingValid, Is.False);
            Assert.That(frame.Validity.IsLensValid, Is.True);
            Assert.That(frame.Validity.IsFallbackMode, Is.True);
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(35d).Within(0.001d));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ManualTick_WithCompositeSource_ProducesState()
        {
            var root = new GameObject("Composite Sync Source");
            root.AddComponent<Camera>();
            root.AddComponent<AudioListener>();
            var tracker = root.AddComponent<TrackerCameraSourceBehaviour>();
            var lens = root.AddComponent<LensEncoderSourceBehaviour>();
            var composite = root.AddComponent<CompositeCameraSourceBehaviour>();
            var output = root.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = root.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(composite, "poseSourceBehaviour", tracker);
            SetPrivateField(composite, "lensSourceBehaviour", lens);
            SetPrivateField(sync, "sourceBehaviour", composite);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(sync.LastState.SourceId, Is.EqualTo("composite-source"));
            Assert.That(sync.LastState.ObservedLens.FocalLengthMm, Is.EqualTo(35d).Within(0.001d));
            Assert.That(output.LastPacketHex, Is.Not.Empty);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator TryGetObservedFrame_CanBindCameraIdIndependentlyFromTracker()
        {
            var root = new GameObject("External Tracking Composite");
            var tracker = root.AddComponent<TrackerCameraSourceBehaviour>();
            var lens = root.AddComponent<LensEncoderSourceBehaviour>();
            var composite = root.AddComponent<CompositeCameraSourceBehaviour>();
            SetPrivateField(composite, "poseSourceBehaviour", tracker);
            SetPrivateField(composite, "lensSourceBehaviour", lens);
            SetPrivateField(composite, "cameraId", 21);
            SetPrivateField(composite, "usePoseSourceCameraId", false);

            yield return null;

            Assert.That(composite.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.CameraId, Is.EqualTo(21));
            Assert.That(frame.Validity.IsTrackingValid, Is.True);
            Assert.That(frame.Validity.IsLensValid, Is.True);

            Object.Destroy(root);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
