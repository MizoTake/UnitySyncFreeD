using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDMountProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ManualTick_AppliesMountProfileToOutputState()
        {
            var cameraObject = new GameObject("SyncFreeD Mount Profile Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = new Vector3(1f, 2f, 3f);
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var mountProfile = ScriptableObject.CreateInstance<MountProfileAsset>();
            mountProfile.Value = new MountProfile
            {
                TrackingOriginOffsetMm = new Vector3Data(10d, 20d, 30d)
            };
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            SetPrivateField(sync, "mountProfileAsset", mountProfile);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(sync.LastState.Corrected.Xmm, Is.EqualTo(1000d).Within(0.001d));
            Assert.That(sync.LastOutputState.Corrected.Xmm, Is.EqualTo(1010d).Within(0.001d));
            Assert.That(sync.LastOutputState.Corrected.Ymm, Is.EqualTo(3020d).Within(0.001d));
            Assert.That(sync.LastOutputState.Corrected.Zmm, Is.EqualTo(2030d).Within(0.001d));

            Object.Destroy(mountProfile);
            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
