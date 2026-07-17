using System.Collections;
using System.Net;
using System.Net.Sockets;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpInputProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyProfile_UsesPresetValues_ForInputSourceAndLoopbackReceiver()
        {
            var profile = ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            profile.Value.ListenPort = 41030;
            profile.Value.BindAddress = "127.0.0.1";
            profile.Value.ValidateChecksum = false;
            profile.Value.CameraIdFilter = 7;
            profile.Value.JoinMulticastGroup = true;
            profile.Value.MulticastGroupIpAddress = "239.10.10.10";
            profile.Value.MulticastInterfaceAddress = "127.0.0.1";

            var inputObject = new GameObject("Input Profile Source");
            inputObject.SetActive(false);
            var inputSource = inputObject.AddComponent<FreeDInputSourceBehaviour>();
            inputSource.SetInputProfileAsset(profile, true);

            var receiverObject = new GameObject("Input Profile Receiver");
            receiverObject.SetActive(false);
            var receiver = receiverObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
            receiver.SetInputProfileAsset(profile, true);

            yield return null;

            Assert.That(inputSource.ListenPort, Is.EqualTo(41030));
            Assert.That(inputSource.BindAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(inputSource.ValidateChecksum, Is.False);
            Assert.That(inputSource.CameraIdFilter, Is.EqualTo(7));
            Assert.That(inputSource.JoinMulticastGroup, Is.True);
            Assert.That(inputSource.MulticastGroupIpAddress, Is.EqualTo("239.10.10.10"));
            Assert.That(inputSource.MulticastInterfaceAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(receiver.ListenPort, Is.EqualTo(41030));
            Assert.That(receiver.BindAddress, Is.EqualTo("127.0.0.1"));

            Object.Destroy(profile);
            Object.Destroy(inputObject);
            Object.Destroy(receiverObject);
        }

        [UnityTest]
        public IEnumerator ApplyProfile_RebindsActiveInputSourceAndLoopbackReceiver()
        {
            GetTwoAvailableUdpPorts(out var inputPort, out var inputReplacementPort);
            var inputProfile = ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            inputProfile.Value.BindAddress = "127.0.0.1";
            inputProfile.Value.ListenPort = inputPort;
            var inputObject = new GameObject("Rebinding Input Profile Source");
            inputObject.SetActive(false);
            var inputSource = inputObject.AddComponent<FreeDInputSourceBehaviour>();
            inputSource.SetInputProfileAsset(inputProfile, true);
            inputObject.SetActive(true);
            yield return null;
            var previousHub = GetPrivateField<FreeDUdpReceiveHub>(inputSource, "receiveHub");

            inputProfile.Value.ListenPort = inputReplacementPort;
            inputSource.SetInputProfileAsset(inputProfile, true);
            var replacementHub = GetPrivateField<FreeDUdpReceiveHub>(inputSource, "receiveHub");

            Assert.That(previousHub, Is.Not.Null);
            Assert.That(previousHub.IsBound, Is.False);
            Assert.That(replacementHub, Is.Not.Null);
            Assert.That(replacementHub, Is.Not.SameAs(previousHub));
            Assert.That(replacementHub.IsBound, Is.True);

            GetTwoAvailableUdpPorts(out var loopbackPort, out var loopbackReplacementPort);
            var loopbackProfile = ScriptableObject.CreateInstance<FreeDUdpInputProfileAsset>();
            loopbackProfile.Value.BindAddress = "127.0.0.1";
            loopbackProfile.Value.ListenPort = loopbackPort;
            var receiverObject = new GameObject("Rebinding Input Profile Receiver");
            receiverObject.SetActive(false);
            var receiver = receiverObject.AddComponent<FreeDLoopbackReceiverBehaviour>();
            receiver.SetInputProfileAsset(loopbackProfile, true);
            receiverObject.SetActive(true);
            yield return null;
            var previousClient = GetPrivateField<UdpClient>(receiver, "udpClient");

            loopbackProfile.Value.ListenPort = loopbackReplacementPort;
            receiver.SetInputProfileAsset(loopbackProfile, true);
            var replacementClient = GetPrivateField<UdpClient>(receiver, "udpClient");

            Assert.That(previousClient, Is.Not.Null);
            Assert.That(replacementClient, Is.Not.Null);
            Assert.That(replacementClient, Is.Not.SameAs(previousClient));
            Assert.Throws<System.ObjectDisposedException>(() => previousClient.Send(new byte[] { 0xD1 }, 1, new IPEndPoint(IPAddress.Loopback, loopbackReplacementPort)));

            Object.Destroy(inputProfile);
            Object.Destroy(loopbackProfile);
            Object.Destroy(inputObject);
            Object.Destroy(receiverObject);
            yield return null;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)field.GetValue(target);
        }

        private static void GetTwoAvailableUdpPorts(out int firstPort, out int secondPort)
        {
            using (var firstListener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            using (var secondListener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                firstPort = ((IPEndPoint)firstListener.Client.LocalEndPoint).Port;
                secondPort = ((IPEndPoint)secondListener.Client.LocalEndPoint).Port;
            }
        }
    }
}
