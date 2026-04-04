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
    public sealed class FreeDDrivenCameraBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator LateUpdate_AppliesPoseAndLensToCamera()
        {
            const int port = 41022;
            var cameraObject = new GameObject("Driven Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var source = cameraObject.AddComponent<FreeDInputSourceBehaviour>();
            var driver = cameraObject.AddComponent<FreeDDrivenCameraBehaviour>();
            SetPrivateField(source, "listenPort", port);
            cameraObject.SetActive(false);
            cameraObject.SetActive(true);

            using (var udpClient = new UdpClient())
            {
                udpClient.Send(CreatePacket(4, 25f, -10f, 5f, 1500d, 2500d, 3500d, 60d, 5d, 3.2d, 2), FreeDPacketBuilder.PacketLength, "127.0.0.1", port);
            }

            yield return WaitUntilApplied(driver, 60);

            Assert.That(driver.LastAppliedFrame.CameraId, Is.EqualTo(4));
            Assert.That(cameraObject.transform.position.x, Is.EqualTo(1.5f).Within(0.02f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(3.5f).Within(0.02f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(2.5f).Within(0.02f));
            Assert.That(cameraObject.transform.eulerAngles.y, Is.EqualTo(25f).Within(0.1f));
            Assert.That(camera.usePhysicalProperties, Is.True);
            Assert.That(camera.focalLength, Is.EqualTo(60f).Within(0.01f));
            Assert.That(camera.focusDistance, Is.EqualTo(5f).Within(0.01f));
            Assert.That(driver.LastAppliedFieldOfView, Is.GreaterThan(0f));

            Object.Destroy(cameraObject);
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

        private static IEnumerator WaitUntilApplied(FreeDDrivenCameraBehaviour behaviour, int maxFrames)
        {
            for (var frame = 0; frame < maxFrames; frame++)
            {
                if (behaviour.LastAppliedFrame.Pose.TimestampTicks != 0L)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
