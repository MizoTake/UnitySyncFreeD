using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class TrackerCameraSourceBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator TryGetObservedFrame_ReadsTrackedTransform()
        {
            var trackerObject = new GameObject("Tracker Source");
            trackerObject.transform.position = new Vector3(1f, 2f, 3f);
            trackerObject.transform.rotation = Quaternion.Euler(10f, 20f, 30f);
            var tracker = trackerObject.AddComponent<TrackerCameraSourceBehaviour>();

            yield return null;

            Assert.That(tracker.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.Pose.Xmm, Is.EqualTo(1000d).Within(0.0001d));
            Assert.That(frame.Pose.Zmm, Is.EqualTo(2000d).Within(0.0001d));
            Assert.That(frame.Validity.IsTrackingValid, Is.True);

            Object.Destroy(trackerObject);
        }
    }
}
