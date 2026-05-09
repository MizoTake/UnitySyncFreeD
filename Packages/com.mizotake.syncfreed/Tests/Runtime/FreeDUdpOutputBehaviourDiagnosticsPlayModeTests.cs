using System.Collections;
using System.Text.RegularExpressions;
using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.ScriptableObjects;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class FreeDUdpOutputBehaviourDiagnosticsPlayModeTests
    {
        [UnityTest]
        public IEnumerator BuildPacket_ReturnsCopyAndUpdatesPacketHex()
        {
            var outputObject = new GameObject("FreeD Output Diagnostics");
            var output = outputObject.AddComponent<FreeDUdpOutputBehaviour>();

            var firstPacket = output.BuildPacket(CreateState(1));
            var firstPacketAgain = output.BuildPacket(CreateState(1));
            Assert.That(firstPacket, Is.Not.SameAs(firstPacketAgain));
            firstPacket[1] = 99;
            Assert.That(output.LastPacketHex, Does.StartWith("D1-01"));
            var secondPacket = output.BuildPacket(CreateState(2));
            Assert.That(firstPacket[1], Is.EqualTo(99));
            Assert.That(secondPacket[1], Is.EqualTo(2));
            Assert.That(output.LastPacketHex, Does.StartWith("D1-02"));

            Object.Destroy(outputObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Send_WithInvalidBindAddress_DoesNotThrowAndRecordsFailure()
        {
            var outputObject = new GameObject("FreeD Output Invalid Bind");
            var output = outputObject.AddComponent<FreeDUdpOutputBehaviour>();
            var profile = ScriptableObject.CreateInstance<FreeDUdpOutputProfileAsset>();
            profile.Value.BindAddress = "invalid-address";
            output.SetOutputProfileAsset(profile, true);

            LogAssert.Expect(LogType.Warning, new Regex("SyncFreeD UDP transport setup failed:.*"));
            Assert.DoesNotThrow(() => output.Send(CreateState(1)));
            Assert.That(output.LastSendSuccessCount, Is.EqualTo(0));
            Assert.That(output.TotalSendFailureCount, Is.EqualTo(1));
            Assert.That(output.LastDestinationDiagnosticCount, Is.EqualTo(1));
            Assert.That(output.LastDestinationDiagnostics[0].Success, Is.False);
            Assert.That(output.LastPacketHex, Does.StartWith("D1-01"));

            Object.Destroy(profile);
            Object.Destroy(outputObject);
            yield return null;
        }

        private static CameraSyncState CreateState(int cameraId)
        {
            return new CameraSyncState
            {
                CameraId = cameraId,
                Corrected = new PoseState { PanDeg = 1d, TiltDeg = 2d, RollDeg = 3d, Xmm = 100d, Ymm = 200d, Zmm = 300d },
                CorrectedLens = new LensState { FocalLengthMm = 35d, FocusDistanceMeters = 2d, IrisFNumber = 2.8d },
                Timing = new TimingState { FrameModulo16 = 1 }
            };
        }

    }
}
