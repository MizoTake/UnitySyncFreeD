using System.Collections;
using System.Net.Sockets;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Outputs;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDInputSourceBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator Update_ReceivesAndParsesUnicastFreeDPacket()
        {
            const int port = 41020;
            var receiverObject = new GameObject("FreeD Input Receiver");
            var receiver = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            SetPrivateField(receiver, "listenPort", port);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(7, 15f, -5f, 2f, 1000d, 2000d, 3000d, 35d, 4d, 2.8d, 6), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilReceived(receiver, 30);

            Assert.That(receiver.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.CameraId, Is.EqualTo(7));
            Assert.That(frame.Pose.PanDeg, Is.EqualTo(15d).Within(0.001d));
            Assert.That(frame.Pose.TiltDeg, Is.EqualTo(-5d).Within(0.001d));
            Assert.That(frame.Pose.Xmm, Is.EqualTo(1000d).Within(0.001d));
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(35d).Within(0.001d));
            Assert.That(frame.Timing.FrameModulo16, Is.EqualTo(6));

            Object.Destroy(receiverObject);
        }

        [UnityTest]
        public IEnumerator Update_ReceivesAndParsesMulticastFreeDPacket()
        {
            const int port = 41021;
            const string group = "239.10.10.10";
            var receiverObject = new GameObject("FreeD Multicast Receiver");
            var receiver = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            SetPrivateField(receiver, "listenPort", port);
            SetPrivateField(receiver, "joinMulticastGroup", true);
            SetPrivateField(receiver, "multicastGroupIpAddress", group);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.MulticastLoopback = true;
                udpClient.Send(CreatePacket(9, -20f, 10f, 1f, -200d, 500d, 1500d, 50d, 6d, 4d, 3), FreeDPacketBuilder.PacketLength, group, port);
            }

            yield return WaitUntilReceived(receiver, 60);

            Assert.That(receiver.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.CameraId, Is.EqualTo(9));
            Assert.That(frame.Pose.PanDeg, Is.EqualTo(-20d).Within(0.001d));
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(50d).Within(0.001d));

            Object.Destroy(receiverObject);
        }

        [UnityTest]
        public IEnumerator Update_ReceivesMultipleCameraIdsOnSamePort_WhenFiltersDiffer()
        {
            const int port = 41022;
            var receiverOneObject = new GameObject("FreeD Input Receiver One");
            var receiverOne = receiverOneObject.AddComponent<FreeDInputSourceBehaviour>();
            SetPrivateField(receiverOne, "listenPort", port);
            SetPrivateField(receiverOne, "cameraIdFilter", 7);
            receiverOneObject.SetActive(false);
            receiverOneObject.SetActive(true);

            var receiverTwoObject = new GameObject("FreeD Input Receiver Two");
            var receiverTwo = receiverTwoObject.AddComponent<FreeDInputSourceBehaviour>();
            SetPrivateField(receiverTwo, "listenPort", port);
            SetPrivateField(receiverTwo, "cameraIdFilter", 8);
            receiverTwoObject.SetActive(false);
            receiverTwoObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(7, 10f, 0f, 0f, 100d, 200d, 300d, 35d, 4d, 2.8d, 1), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
                udpClient.Send(CreatePacket(8, -10f, 0f, 0f, 400d, 500d, 600d, 55d, 6d, 4d, 2), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilReceived(receiverOne, 60);
            yield return WaitUntilReceived(receiverTwo, 60);

            Assert.That(receiverOne.TryGetObservedFrame(out var frameOne), Is.True);
            Assert.That(receiverTwo.TryGetObservedFrame(out var frameTwo), Is.True);
            Assert.That(frameOne.CameraId, Is.EqualTo(7));
            Assert.That(frameTwo.CameraId, Is.EqualTo(8));
            Assert.That(frameOne.Lens.FocalLengthMm, Is.EqualTo(35d).Within(0.001d));
            Assert.That(frameTwo.Lens.FocalLengthMm, Is.EqualTo(55d).Within(0.001d));

            Object.Destroy(receiverOneObject);
            Object.Destroy(receiverTwoObject);
        }

        [UnityTest]
        public IEnumerator CaptureCommandFrame_DelegatesToConfiguredCommandSource()
        {
            const int port = 41025;
            var receiverObject = new GameObject("FreeD Input With Command Source");
            var receiver = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            var commandSource = receiverObject.AddComponent<UnityCameraSourceBehaviour>();
            var unityCamera = receiverObject.AddComponent<Camera>();
            unityCamera.focalLength = 82f;
            receiverObject.transform.position = new Vector3(1f, 2f, 3f);
            receiverObject.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            SetPrivateField(receiver, "listenPort", port);
            SetPrivateField(receiver, "commandSourceBehaviour", commandSource);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(7, 15f, -5f, 2f, 1000d, 2000d, 3000d, 35d, 4d, 2.8d, 6), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilReceived(receiver, 30);

            var commandFrame = receiver.CaptureCommandFrame();
            Assert.That(commandFrame.CameraId, Is.EqualTo(commandSource.CameraId));
            Assert.That(commandFrame.Pose.PanDeg, Is.EqualTo(25d).Within(0.5d));
            Assert.That(commandFrame.Pose.Xmm, Is.EqualTo(1000d).Within(0.001d));
            Assert.That(commandFrame.Pose.Zmm, Is.EqualTo(2000d).Within(0.001d));
            Assert.That(commandFrame.Lens.FocalLengthMm, Is.EqualTo(82d).Within(0.001d));

            Object.Destroy(receiverObject);
        }

        private static byte[] CreatePacket(int cameraId, double panDeg, double tiltDeg, double rollDeg, double xmm, double ymm, double zmm, double focalLengthMm, double focusDistanceMeters, double irisFNumber, ushort frameModulo16)
        {
            var packet = new byte[FreeDPacketBuilder.PacketLength];
            new FreeDPacketBuilder().Build(new CameraSyncState
            {
                CameraId = cameraId,
                Corrected = new PoseState { PanDeg = panDeg, TiltDeg = tiltDeg, RollDeg = rollDeg, Xmm = xmm, Ymm = ymm, Zmm = zmm },
                CorrectedLens = new LensState { FocalLengthMm = focalLengthMm, FocusDistanceMeters = focusDistanceMeters, IrisFNumber = irisFNumber },
                Timing = new TimingState { FrameModulo16 = frameModulo16 }
            }, packet);
            return packet;
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static IEnumerator WaitUntilReceived(FreeDInputSourceBehaviour receiver, int maxFrames)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (receiver.ReceivedCount > 0)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
