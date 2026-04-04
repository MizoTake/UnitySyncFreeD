using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDControllerBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyTranslationAndRotation_UpdatesTransform()
        {
            var cameraObject = new GameObject("FreeD Controller Camera");
            cameraObject.AddComponent<Camera>();
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();

            yield return null;

            controller.ApplyTranslation(new Vector3(1f, 2f, 3f));
            controller.ApplyRotation(new Vector3(0f, 45f, 0f));

            Assert.That(cameraObject.transform.localPosition.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(cameraObject.transform.localPosition.y, Is.EqualTo(2f).Within(0.001f));
            Assert.That(cameraObject.transform.localPosition.z, Is.EqualTo(3f).Within(0.001f));
            Assert.That(cameraObject.transform.localEulerAngles.y, Is.EqualTo(45f).Within(0.001f));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ApplyLensDeltaAndReset_UpdatesCameraLens()
        {
            var cameraObject = new GameObject("FreeD Controller Lens Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();

            yield return null;

            controller.ApplyLensDelta(15f, 2f);
            Assert.That(camera.focalLength, Is.EqualTo(65f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(12f).Within(0.001f));

            controller.ResetPoseAndLens();
            Assert.That(camera.focalLength, Is.EqualTo(50f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(10f).Within(0.001f));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_AfterControllerChange_UpdatesPacketHex()
        {
            var cameraObject = new GameObject("FreeD Controller Sync Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDControllerBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            var initialPacket = output.LastPacketHex;

            cameraObject.GetComponent<FreeDControllerBehaviour>().ApplyRotation(new Vector3(0f, 15f, 0f));
            cameraObject.GetComponent<FreeDControllerBehaviour>().ApplyLensDelta(10f, 1f);
            Assert.That(sync.ManualTick(), Is.True);

            Assert.That(output.LastPacketHex, Is.Not.EqualTo(initialPacket));
            Assert.That(sync.LastState.Corrected.PanDeg, Is.Not.EqualTo(0d));
            Assert.That(sync.LastState.CorrectedLens.FocalLengthMm, Is.GreaterThan(50d));

            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
