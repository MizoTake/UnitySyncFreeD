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
    }
}
