using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpLoopbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_SendsPacketToLoopbackReceiver()
        {
            var receiverObject = new GameObject("Loopback Receiver");
            var receiver = receiverObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
            var cameraObject = new GameObject("Loopback Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();

            yield return null;
            yield return null;

            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(output.LastPacketHex, Is.Not.Empty);
            Assert.That(receiver.ReceivedCount, Is.GreaterThan(0));
            Assert.That(receiver.LastPacketHex, Is.EqualTo(output.LastPacketHex));

            Object.Destroy(receiverObject);
            Object.Destroy(cameraObject);
        }
    }
}
