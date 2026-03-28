using System.Collections;
using MizoTake.SyncFreeD.UnityAdapters.Behaviours;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MizoTake.SyncFreeD.Tests.Runtime
{
    public sealed class ReplayCameraSourceBehaviourPlayModeTests
    {
        [UnityTest]
        public IEnumerator TryGetObservedFrame_ReturnsReplayPose()
        {
            var replayObject = new GameObject("Replay Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();

            yield return null;

            Assert.That(replay.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.SourceId, Is.EqualTo("replay"));
            Assert.That(frame.Validity.IsTrackingValid, Is.True);

            Object.Destroy(replayObject);
        }
    }
}
