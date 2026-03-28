using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class SyncFreeDBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_ProducesLastStateAndPacketHex()
        {
            var cameraObject = new GameObject("SyncFreeD Test Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            var source = cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;

            Assert.That(source, Is.Not.Null);
            Assert.That(output.LastPacketHex, Is.Not.Empty);
            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(sync.LastState.Validity.IsTrackingValid, Is.True);
            Assert.That(sync.LastState.Timing.FrameModulo16, Is.GreaterThanOrEqualTo((ushort)0));

            Object.Destroy(cameraObject);
        }
    }
}
