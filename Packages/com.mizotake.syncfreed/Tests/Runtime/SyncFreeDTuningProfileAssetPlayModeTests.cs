using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDTuningProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_UsesTuningProfileAssetWhenAssigned()
        {
            var cameraObject = new GameObject("SyncFreeD Asset Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var asset = ScriptableObject.CreateInstance<SyncTuningProfileAsset>();
            asset.Value = new SyncTuningProfile { TrackingDelayMs = 123 };
            SetPrivateField(sync, "tuningProfileAsset", asset);

            yield return null;

            Assert.That(sync.EffectiveTuning.TrackingDelayMs, Is.EqualTo(123));

            Object.Destroy(cameraObject);
            Object.Destroy(asset);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
