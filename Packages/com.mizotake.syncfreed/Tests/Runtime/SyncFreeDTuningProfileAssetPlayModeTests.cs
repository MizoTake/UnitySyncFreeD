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
            asset.Value = new SyncTuningProfile { TrackingDelayMs = 123, IdleToSettleDelayMs = 234, SettleIntervalMs = 56, SettleTimeoutMs = 789, RequiredConsecutiveMatches = 4 };
            SetPrivateField(sync, "tuningProfileAsset", asset);

            yield return null;

            Assert.That(sync.EffectiveTuning.TrackingDelayMs, Is.EqualTo(123));
            Assert.That(sync.EffectiveTuning.IdleToSettleDelayMs, Is.EqualTo(234));
            Assert.That(sync.EffectiveTuning.SettleIntervalMs, Is.EqualTo(56));
            Assert.That(sync.EffectiveTuning.SettleTimeoutMs, Is.EqualTo(789));
            Assert.That(sync.EffectiveTuning.RequiredConsecutiveMatches, Is.EqualTo(4));

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
