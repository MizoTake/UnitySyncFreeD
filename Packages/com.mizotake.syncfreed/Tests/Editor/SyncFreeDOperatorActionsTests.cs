using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncFreeDOperatorActionsTests
    {
        [Test]
        public void TryTranslateRotateLensAndReset_WithController_UpdatesRig()
        {
            var cameraObject = new GameObject("Operator Actions Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            cameraObject.AddComponent<FreeDControllerBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            Assert.That(SyncFreeDOperatorActions.TryTranslate(sync, new Vector3(0.5f, 0.25f, 1f)), Is.True);
            Assert.That(SyncFreeDOperatorActions.TryRotate(sync, new Vector3(-10f, 15f, 5f)), Is.True);
            Assert.That(SyncFreeDOperatorActions.TryAdjustLens(sync, 12f, 1.5f), Is.True);
            Assert.That(cameraObject.transform.localPosition.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(cameraObject.transform.localEulerAngles.y, Is.EqualTo(15f).Within(0.001f));
            Assert.That(camera.focalLength, Is.EqualTo(62f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(11.5f).Within(0.001f));

            Assert.That(SyncFreeDOperatorActions.TryReset(sync), Is.True);
            Assert.That(cameraObject.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(cameraObject.transform.localEulerAngles.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(camera.focalLength, Is.EqualTo(50f).Within(0.001f));
            Assert.That(camera.focusDistance, Is.EqualTo(10f).Within(0.001f));

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void TryTranslate_WithoutController_ReturnsFalse()
        {
            var cameraObject = new GameObject("Operator Actions No Controller Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            Assert.That(SyncFreeDOperatorActions.TryTranslate(sync, Vector3.forward), Is.False);

            Object.DestroyImmediate(cameraObject);
        }
    }
}
