using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FixedCameraValidityPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_AllowsTrackingInvalidAndLensValid()
        {
            var cameraObject = new GameObject("Fixed Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.focalLength = 65f;
            cameraObject.AddComponent<AudioListener>();
            var source = cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(source, "trackingValid", false);
            SetPrivateField(source, "lensValid", true);

            yield return null;
            yield return null;

            Assert.That(sync.LastState.Validity.IsTrackingValid, Is.False);
            Assert.That(sync.LastState.Validity.IsLensValid, Is.True);
            Assert.That(sync.LastDiagnostics.IsTrackingValid, Is.False);
            Assert.That(sync.LastDiagnostics.IsLensValid, Is.True);
            Assert.That(sync.LastState.Lens.FocalLengthMm, Is.EqualTo(65d).Within(0.001d));
            Assert.That(output.LastPacketHex, Is.Not.Empty);

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
