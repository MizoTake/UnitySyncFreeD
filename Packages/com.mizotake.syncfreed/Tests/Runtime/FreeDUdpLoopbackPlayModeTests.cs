using System.Collections;
using System.Net.Sockets;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpLoopbackPlayModeTests
    {
        [UnityTest]
        public IEnumerator Update_ReceivesAllPacketsFromExternalUdpSenderBurst()
        {
            const int port = 41010;
            const int packetCount = 256;
            var receiverObject = new GameObject("External Sender Receiver");
            var receiver = receiverObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
            SetPrivateField(receiver, "listenPort", port);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                for (var index = 0; index < packetCount; index++)
                {
                    var payload = new byte[] { 0xD1, 0x01, (byte)index };
                    udpClient.Send(payload, payload.Length, "127.0.0.1", port);
                }
            }

            yield return WaitUntilReceivedCount(receiver, packetCount, 60);

            Assert.That(receiver.IsBound, Is.True);
            Assert.That(receiver.ReceivedCount, Is.EqualTo(packetCount));
            Assert.That(receiver.LastPacketHex, Is.EqualTo("D1-01-FF"));

            Object.Destroy(receiverObject);
        }

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
            Assert.That(receiver.IsBound, Is.True);
            Assert.That(receiver.ReceivedCount, Is.GreaterThan(0));
            Assert.That(receiver.LastPacketHex, Is.EqualTo(output.LastPacketHex));
            Assert.That(receiver.LastRemoteEndpoint, Does.Contain("127.0.0.1"));
            Assert.That(receiver.LastReceivedAtUtcTicks, Is.GreaterThan(0L));

            Object.Destroy(receiverObject);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_SustainsRepeatedLoopbackSendsWithoutFailures()
        {
            const int tickCount = 1000;
            var receiverObject = new GameObject("Soak Receiver");
            var receiver = receiverObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
            var cameraObject = new GameObject("Soak Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", Core.Models.OutputTickMode.Manual);

            yield return null;

            for (var index = 0; index < tickCount; index++)
            {
                Assert.That(sync.ManualTick(), Is.True, $"ManualTick failed at iteration {index}");
            }

            yield return WaitUntilReceivedCount(receiver, tickCount, 120);

            Assert.That(output.TotalSendFailureCount, Is.EqualTo(0));
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(1));
            Assert.That(receiver.ReceivedCount, Is.EqualTo(tickCount));
            Assert.That(receiver.LastPacketHex, Is.EqualTo(output.LastPacketHex));

            Object.Destroy(receiverObject);
            Object.Destroy(cameraObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static IEnumerator WaitUntilReceivedCount(FreeDLoopbackReceiverBehaviour receiver, int expectedCount, int maxFrames)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (receiver.ReceivedCount >= expectedCount)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
