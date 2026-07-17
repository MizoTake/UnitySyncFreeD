using System.Collections;
using System.Net;
using MizoTake.SyncFreeD.Networking;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputProfileAssetPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyProfile_UsesPresetValues()
        {
            var cameraObject = new GameObject("Output Profile Camera");
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.PacketSendMode = PacketSendMode.MultiDestinationUnicast;
            profile.Value.DestinationIpAddress = "127.0.0.1";
            profile.Value.DestinationPort = 41000;
            profile.Value.AdditionalDestinations = new[]
            {
                new FreeDUdpDestination { IpAddress = "127.0.0.1", Port = 41001, Enabled = true }
            };
            profile.Value.CameraIdFilter = 12;
            profile.Value.MulticastTtl = 32;

            output.SetOutputProfileAsset(profile, true);
            yield return null;

            Assert.That(output.SendMode, Is.EqualTo(PacketSendMode.MultiDestinationUnicast));
            Assert.That(output.DestinationPort, Is.EqualTo(41000));
            Assert.That(output.CameraIdFilter, Is.EqualTo(12));
            Assert.That(output.MulticastTtl, Is.EqualTo(32));
            Assert.That(output.GetConfiguredDestinationCount(), Is.EqualTo(2));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ApplyProfile_RecreatesAnExistingTransport()
        {
            var cameraObject = new GameObject("Output Profile Transport");
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.DestinationIpAddress = "127.0.0.1";
            profile.Value.DestinationPort = 41002;
            output.SetOutputProfileAsset(profile, true);
            output.Send(default);
            var previousTransport = GetPrivateField<FreeDUdpTransport>(output, "transport");

            profile.Value.SocketBufferSize = 32768;
            output.SetOutputProfileAsset(profile, true);

            Assert.That(previousTransport, Is.Not.Null);
            Assert.That(GetPrivateField<FreeDUdpTransport>(output, "transport"), Is.Null);
            Assert.Throws<System.ObjectDisposedException>(() => previousTransport.Send(new byte[] { 0xD1 }, new IPEndPoint(IPAddress.Loopback, 41002)));

            output.Send(default);
            Assert.That(GetPrivateField<FreeDUdpTransport>(output, "transport"), Is.Not.SameAs(previousTransport));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyProfileOnEnable_AppliesOnFirstAndSubsequentEnable()
        {
            var cameraObject = new GameObject("Output Profile Enable Lifecycle");
            cameraObject.SetActive(false);
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.DestinationPort = 41004;
            output.SetOutputProfileAsset(profile, false);

            cameraObject.SetActive(true);
            yield return null;
            Assert.That(output.DestinationPort, Is.EqualTo(41004));

            cameraObject.SetActive(false);
            profile.Value.DestinationPort = 41005;
            cameraObject.SetActive(true);
            yield return null;
            Assert.That(output.DestinationPort, Is.EqualTo(41005));

            Object.Destroy(profile);
            Object.Destroy(cameraObject);
            yield return null;
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (T)field.GetValue(target);
        }
    }
}
