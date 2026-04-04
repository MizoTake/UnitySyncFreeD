using System.Net;
using System.Net.Sockets;
using System.Reflection;
using MizoTake.SyncFreeD.Editor.Windows;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class FreeDDebugControllerWindowTests
    {
        [Test]
        public void SendCurrentPacketForDebug_WhenTargetInputSourceExists_SendsPacketToConfiguredListener()
        {
            using var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Client.ReceiveTimeout = 2000;
            var listenPort = ((IPEndPoint)listener.Client.LocalEndPoint).Port;
            var receiverObject = new GameObject("Debug Controller Receiver");
            var inputSource = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            SetPrivateField(inputSource, "joinMulticastGroup", false);
            SetPrivateField(inputSource, "bindAddress", "127.0.0.1");
            SetPrivateField(inputSource, "listenPort", listenPort);
            var window = EditorWindow.GetWindow<FreeDDebugControllerWindow>();
            try
            {
                window.SetTargetForDebug(inputSource, null);

                Assert.That(window.SendCurrentPacketForDebug(), Is.True);

                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var packet = listener.Receive(ref remoteEndPoint);
                Assert.That(packet, Is.Not.Null);
                Assert.That(packet.Length, Is.EqualTo(29));
                Assert.That(packet[0], Is.EqualTo(0xD1));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(receiverObject);
            }
        }

        [Test]
        public void SendCurrentPacketForDebug_ReachesInputSourceAndDrivenCameraOverUnicast()
        {
            const int port = 41041;
            var receiverObject = new GameObject("Debug Controller Unicast Receiver");
            var camera = receiverObject.AddComponent<Camera>();
            var inputSource = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            var drivenCamera = receiverObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(inputSource, "joinMulticastGroup", false);
            SetPrivateField(inputSource, "bindAddress", string.Empty);
            SetPrivateField(inputSource, "listenPort", port);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);
            EnsureInputSourceBound(inputSource);
            var window = EditorWindow.GetWindow<FreeDDebugControllerWindow>();
            try
            {
                window.SetTargetForDebug(inputSource, drivenCamera);
                SetPrivateField(window, "panDeg", 30f);
                SetPrivateField(window, "tiltDeg", -12f);
                SetPrivateField(window, "xMeters", 1.25f);
                SetPrivateField(window, "yMeters", 2.5f);
                SetPrivateField(window, "zMeters", -6f);
                SetPrivateField(window, "focalLengthMm", 80f);
                Assert.That(window.SendCurrentPacketForDebug(), Is.True);

                PumpReceiver(inputSource, 40);

                Assert.That(inputSource.TryGetObservedFrame(out var frame), Is.True);
                Assert.That(frame.CameraId, Is.GreaterThanOrEqualTo(0));
                Assert.That(drivenCamera.ApplyLatestFrame(), Is.True);
                Assert.That(drivenCamera.LastAppliedFrame.Pose.TimestampTicks, Is.GreaterThan(0L));
                Assert.That(receiverObject.transform.position.x, Is.EqualTo(1.25f).Within(0.02f));
                Assert.That(receiverObject.transform.position.y, Is.EqualTo(2.5f).Within(0.02f));
                Assert.That(receiverObject.transform.position.z, Is.EqualTo(-6f).Within(0.02f));
                Assert.That(receiverObject.transform.eulerAngles.y, Is.EqualTo(30f).Within(0.2f));
                Assert.That(camera.usePhysicalProperties, Is.True);
                Assert.That(camera.focalLength, Is.EqualTo(80f).Within(0.01f));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(receiverObject);
            }
        }

        [Test]
        public void SendCurrentPacketForDebug_ReachesInputSourceAndDrivenCameraOverMulticast()
        {
            const int port = 41042;
            const string group = "239.10.10.42";
            var receiverObject = new GameObject("Debug Controller Multicast Receiver");
            var camera = receiverObject.AddComponent<Camera>();
            var inputSource = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            var drivenCamera = receiverObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(inputSource, "joinMulticastGroup", true);
            SetPrivateField(inputSource, "multicastGroupIpAddress", group);
            SetPrivateField(inputSource, "listenPort", port);
            receiverObject.SetActive(false);
            receiverObject.SetActive(true);
            EnsureInputSourceBound(inputSource);
            var window = EditorWindow.GetWindow<FreeDDebugControllerWindow>();
            try
            {
                window.SetTargetForDebug(inputSource, drivenCamera);
                Assert.That(window.SendCurrentPacketForDebug(), Is.True);

                PumpReceiver(inputSource, 60);

                Assert.That(inputSource.TryGetObservedFrame(out var frame), Is.True);
                Assert.That(frame.CameraId, Is.GreaterThanOrEqualTo(0));
                Assert.That(drivenCamera.ApplyLatestFrame(), Is.True);
                Assert.That(drivenCamera.LastAppliedFrame.Pose.TimestampTicks, Is.GreaterThan(0L));
                Assert.That(camera.usePhysicalProperties, Is.True);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(receiverObject);
            }
        }

        [Test]
        public void SetTargetForDebug_WithDrivenCamera_CopiesPoseAndLensWithoutThrowing()
        {
            var receiverObject = new GameObject("Debug Controller Driven Camera");
            var camera = receiverObject.AddComponent<Camera>();
            var inputSource = receiverObject.AddComponent<FreeDInputSourceBehaviour>();
            var drivenCamera = receiverObject.AddComponent<FreeDDrivenCameraBehaviour>();
            receiverObject.transform.position = new Vector3(1f, 2f, 3f);
            receiverObject.transform.rotation = Quaternion.Euler(10f, 20f, 5f);
            camera.focalLength = 70f;
            camera.focusDistance = 4f;
            var window = EditorWindow.GetWindow<FreeDDebugControllerWindow>();
            try
            {
                Assert.DoesNotThrow(() => window.SetTargetForDebug(inputSource, drivenCamera));
                Assert.That(window.SendCurrentPacketForDebug(), Is.True);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(receiverObject);
            }
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void PumpReceiver(FreeDInputSourceBehaviour inputSource, int maxAttempts)
        {
            var updateMethod = typeof(FreeDInputSourceBehaviour).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                updateMethod.Invoke(inputSource, null);
                if (inputSource.ReceivedCount > 0)
                {
                    return;
                }

                System.Threading.Thread.Sleep(10);
            }

            Assert.Fail("FreeDInputSourceBehaviour did not receive a packet.");
        }

        private static void EnsureInputSourceBound(FreeDInputSourceBehaviour inputSource)
        {
            if (inputSource.IsBound)
            {
                return;
            }

            var onEnableMethod = typeof(FreeDInputSourceBehaviour).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnableMethod.Invoke(inputSource, null);
            Assert.That(inputSource.IsBound, Is.True, "FreeDInputSourceBehaviour failed to bind.");
        }
    }
}
