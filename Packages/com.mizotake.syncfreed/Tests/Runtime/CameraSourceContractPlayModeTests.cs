using System.Collections;
using MizoTake.SyncFreeD.Core.Abstractions;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class CameraSourceContractPlayModeTests
    {
        [UnityTest]
        public IEnumerator TrackerSource_ImplementsCameraSourceContract()
        {
            var sourceObject = new GameObject("Tracker Source Contract");
            var source = sourceObject.AddComponent<TrackerCameraSourceBehaviour>();

            yield return null;

            var contract = source as ICameraSource;
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.SourceId, Is.Not.Empty);
            Assert.That(contract.Capabilities, Is.Not.EqualTo(0));
            Assert.That(contract.TryGetObservedState(out var frame), Is.True);
            Assert.That(frame.SourceId, Is.EqualTo(contract.SourceId));

            Object.Destroy(sourceObject);
        }

        [UnityTest]
        public IEnumerator DualDriveSource_ResolvesGenericCommandAndObservedTargets()
        {
            var sourceObject = new GameObject("Dual Drive Source");
            sourceObject.SetActive(false);
            var commandTarget = new GameObject("Command Target").transform;
            commandTarget.SetParent(sourceObject.transform, false);
            commandTarget.localPosition = new Vector3(1f, 2f, 3f);
            var observedTarget = new GameObject("Observed Target").transform;
            observedTarget.SetParent(sourceObject.transform, false);
            observedTarget.localPosition = new Vector3(4f, 5f, 6f);
            var source = sourceObject.AddComponent<DualDriveTransformSourceBehaviour>();
            sourceObject.SetActive(true);

            Assert.That(source.TryGetObservedFrame(out var observedFrame), Is.True);
            Assert.That(observedFrame.Pose.Xmm, Is.EqualTo(4000d).Within(0.001d));
            Assert.That(source.CaptureCommandFrame().Pose.Xmm, Is.EqualTo(1000d).Within(0.001d));

            Object.Destroy(sourceObject);
            yield return null;
        }
    }
}
