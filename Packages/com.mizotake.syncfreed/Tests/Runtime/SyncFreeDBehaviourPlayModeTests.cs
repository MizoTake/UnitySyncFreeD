using System.Collections;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.Core.Models;
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

        [UnityTest]
        public IEnumerator ManualTick_RaisesStateUpdatedWithCurrentValues()
        {
            var cameraObject = new GameObject("SyncFreeD Event Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var updatedCount = 0;
            var eventState = default(CameraSyncState);
            var eventOutputState = default(CameraSyncState);
            sync.StateUpdated += (state, outputState, diagnostics) =>
            {
                updatedCount++;
                eventState = state;
                eventOutputState = outputState;
            };
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(updatedCount, Is.EqualTo(1));
            Assert.That(eventState.SourceId, Is.EqualTo(sync.LastState.SourceId));
            Assert.That(eventOutputState.SourceId, Is.EqualTo(sync.LastOutputState.SourceId));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_WithDisabledOutput_UpdatesStateWithoutSendingPacket()
        {
            var cameraObject = new GameObject("SyncFreeD Disabled Output Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var output = cameraObject.AddComponent<FreeDUdpOutputBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            output.enabled = false;
            var updatedCount = 0;
            sync.StateUpdated += (state, outputState, diagnostics) => updatedCount++;

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(updatedCount, Is.EqualTo(1));
            Assert.That(output.LastPacketHex, Is.Empty);

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_WithInactiveOutputObject_UpdatesStateWithoutSendingPacket()
        {
            var cameraObject = new GameObject("SyncFreeD Inactive Output Camera");
            var outputObject = new GameObject("Inactive Output");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var output = outputObject.AddComponent<FreeDUdpOutputBehaviour>();
            sync.SetOutputBehaviour(output);
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            outputObject.SetActive(false);
            var updatedCount = 0;
            sync.StateUpdated += (state, outputState, diagnostics) => updatedCount++;

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(updatedCount, Is.EqualTo(1));
            Assert.That(output.LastPacketHex, Is.Empty);

            Object.Destroy(cameraObject);
            Object.Destroy(outputObject);
        }

        [UnityTest]
        public IEnumerator ManualTick_WithoutUdpOutput_StillUpdatesStateAndEvent()
        {
            var cameraObject = new GameObject("SyncFreeD State Only Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityCameraSourceBehaviour>();
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            SetPrivateField(sync, "outputTickMode", OutputTickMode.Manual);
            var updatedCount = 0;
            sync.StateUpdated += (state, outputState, diagnostics) => updatedCount++;

            yield return null;

            Assert.That(sync.ManualTick(), Is.True);
            Assert.That(sync.LastState.SourceId, Is.Not.Empty);
            Assert.That(sync.LastOutputState.SourceId, Is.EqualTo(sync.LastState.SourceId));
            Assert.That(updatedCount, Is.EqualTo(1));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator SetSourceBehaviour_RejectsNonProviderAndAcceptsCameraFrameProvider()
        {
            var cameraObject = new GameObject("SyncFreeD Source Setter Camera");
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var nonProvider = cameraObject.AddComponent<DebugLogOutputBehaviour>();
            var provider = cameraObject.AddComponent<FakeCameraFrameProviderBehaviour>();

            yield return null;

            Assert.That(sync.SetSourceBehaviour(nonProvider), Is.False);
            Assert.That(sync.SetSourceBehaviour(provider), Is.True);
            Assert.That(sync.SourceProvider, Is.SameAs((ICameraFrameProvider)provider));

            Object.Destroy(cameraObject);
        }

        [UnityTest]
        public IEnumerator SetOutputBehaviour_AssignsSeparatedOutputBehaviour()
        {
            var cameraObject = new GameObject("SyncFreeD Output Setter Camera");
            var outputObject = new GameObject("Separated Output");
            var sync = cameraObject.AddComponent<SyncFreeDBehaviour>();
            var output = outputObject.AddComponent<FreeDUdpOutputBehaviour>();

            yield return null;

            sync.SetOutputBehaviour(output);
            Assert.That(sync.OutputBehaviour, Is.SameAs(output));

            Object.Destroy(cameraObject);
            Object.Destroy(outputObject);
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class FakeCameraFrameProviderBehaviour : MonoBehaviour, ICameraFrameProvider
        {
            string ICameraSource.SourceId => "fake-provider";
            CameraCapabilities ICameraSource.Capabilities => CameraCapabilities.PanTilt;

            bool ICameraSource.TryGetObservedState(out CameraObservedFrame frame)
            {
                return ((ICameraFrameProvider)this).TryGetObservedFrame(out frame);
            }

            bool ICameraFrameProvider.TryGetObservedFrame(out CameraObservedFrame frame)
            {
                frame = new CameraObservedFrame { SourceId = "fake-provider", CameraId = 1, Pose = new PoseState { TimestampTicks = 1L }, Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true } };
                return true;
            }

            CameraCommandFrame ICameraFrameProvider.CaptureCommandFrame()
            {
                return new CameraCommandFrame { SourceId = "fake-provider", CameraId = 1, Pose = new PoseState { TimestampTicks = 1L } };
            }
        }
    }
}
