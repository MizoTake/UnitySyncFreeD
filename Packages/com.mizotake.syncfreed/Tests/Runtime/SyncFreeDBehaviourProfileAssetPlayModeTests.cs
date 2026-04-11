using System.Collections;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDBehaviourProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyProfile_UsesPresetValues()
        {
            var cameraObject = new GameObject("SyncFreeD Profile Camera");
            cameraObject.AddComponent<Camera>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var profile = ScriptableObject.CreateInstance<SyncFreeDBehaviourProfileAsset>();
            profile.Value.SyncMode = SyncMode.RealMaster;
            profile.Value.OutputPoseKind = OutputPoseKind.Blended;
            profile.Value.OutputTickMode = OutputTickMode.FixedInterval;
            profile.Value.FixedIntervalMs = 20;
            profile.Value.LogPacketHex = true;
            profile.Value.Tuning.InquiryIntervalMs = 250;

            sync.SetProfileAsset(profile, true);
            yield return null;

            Assert.That(sync.SyncMode, Is.EqualTo(SyncMode.RealMaster));
            Assert.That(sync.OutputPoseKind, Is.EqualTo(OutputPoseKind.Blended));
            Assert.That(sync.OutputTickMode, Is.EqualTo(OutputTickMode.FixedInterval));
            Assert.That(sync.FixedIntervalMs, Is.EqualTo(20));
            Assert.That(sync.LogPacketHex, Is.True);
            Assert.That(sync.EffectiveTuning.InquiryIntervalMs, Is.EqualTo(250));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }
    }
}
