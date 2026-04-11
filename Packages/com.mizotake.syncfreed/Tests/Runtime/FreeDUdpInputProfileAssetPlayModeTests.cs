using System.Collections;
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
            var inputSource = inputObject.AddComponent<FreeDInputSourceBehaviour>();
            inputSource.SetInputProfileAsset(profile, true);

            var receiverObject = new GameObject("Input Profile Receiver");
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
    }
}
