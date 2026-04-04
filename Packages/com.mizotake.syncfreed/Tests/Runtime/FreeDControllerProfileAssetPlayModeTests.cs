using System.Collections;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDControllerProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyProfile_UsesPresetValues()
        {
            var cameraObject = new GameObject("Controller Profile Camera");
            cameraObject.AddComponent<Camera>();
            var controller = cameraObject.AddComponent<FreeDControllerBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDControllerProfileAsset>();
            profile.Value.MoveSpeedMetersPerSecond = 4f;
            profile.Value.RotateSpeedDegreesPerSecond = 90f;
            profile.Value.BoostMultiplier = 5f;

            controller.SetProfileAsset(profile, true);
            yield return null;

            controller.ApplyTranslation(Vector3.forward * 4f);
            controller.ApplyRotation(new Vector3(0f, 90f, 0f));

            Assert.That(cameraObject.transform.localPosition.z, Is.EqualTo(4f).Within(0.001f));
            Assert.That(cameraObject.transform.localEulerAngles.y, Is.EqualTo(90f).Within(0.001f));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }
    }
}
