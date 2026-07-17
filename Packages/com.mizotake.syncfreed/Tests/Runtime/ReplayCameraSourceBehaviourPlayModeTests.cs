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

        [UnityTest]
        public IEnumerator LoadReplayData_WhenManualFrameStepEnabled_UsesSelectedFrame()
        {
            var replayObject = new GameObject("Replay Step Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();
            SetPrivateField(replay, "manualFrameStep", true);

            Assert.That(replay.LoadReplayData("TimeSeconds,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,FocalLengthMm\n0,0,1,-10,0,0,0,35\n1,1,2,-9,0,45,0,50", ReplayDataFormat.Csv), Is.True);

            yield return null;

            Assert.That(replay.SetFrameIndex(1), Is.True);
            Assert.That(replay.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.Pose.PanDeg, Is.EqualTo(45d).Within(0.001d));
            Assert.That(frame.Lens.FocalLengthMm, Is.EqualTo(50d).Within(0.001d));

            Object.Destroy(replayObject);
        }

        [UnityTest]
        public IEnumerator ReloadReplayData_FromJsonTextAsset_ClearsError()
        {
            var replayObject = new GameObject("Replay Json Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();
            SetPrivateField(replay, "replayDataAsset", new TextAsset("{\"keyframes\":[{\"TimeSeconds\":0.0,\"Position\":{\"x\":0.0,\"y\":1.0,\"z\":-10.0},\"Rotation\":{\"x\":0.0,\"y\":10.0,\"z\":0.0},\"FocalLengthMm\":35.0},{\"TimeSeconds\":1.0,\"Position\":{\"x\":0.5,\"y\":1.5,\"z\":-9.0},\"Rotation\":{\"x\":0.0,\"y\":20.0,\"z\":0.0},\"FocalLengthMm\":40.0}]}"));
            SetPrivateField(replay, "replayDataFormat", ReplayDataFormat.Json);

            yield return null;

            Assert.That(replay.ReloadReplayData(), Is.True);
            Assert.That(replay.LastLoadError, Is.Empty);
            Assert.That(replay.FrameCount, Is.EqualTo(2));

            Object.Destroy(replayObject);
        }

        [UnityTest]
        public IEnumerator PlaybackClock_StartsFromLoadTimeInsteadOfGlobalTime()
        {
            yield return new WaitForSeconds(0.1f);
            var replayObject = new GameObject("Replay Local Clock Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();
            SetPrivateField(replay, "loop", false);
            Assert.That(replay.LoadReplayData(CreateLinearReplayCsv(1f), ReplayDataFormat.Csv), Is.True);

            Assert.That(replay.TryGetObservedFrame(out var frame), Is.True);
            Assert.That(frame.Pose.Xmm, Is.LessThan(100d));

            Object.Destroy(replayObject);
        }

        [UnityTest]
        public IEnumerator SetPlaybackPaused_FreezesAndResumesLocalPlaybackClock()
        {
            var replayObject = new GameObject("Replay Pause Resume Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();
            SetPrivateField(replay, "loop", false);
            Assert.That(replay.LoadReplayData(CreateLinearReplayCsv(1f), ReplayDataFormat.Csv), Is.True);

            yield return new WaitForSeconds(0.12f);
            Assert.That(replay.TryGetObservedFrame(out var beforePauseFrame), Is.True);
            replay.SetPlaybackPaused(true);
            Assert.That(replay.TryGetObservedFrame(out var pausedFrame), Is.True);
            Assert.That(pausedFrame.Pose.Xmm, Is.EqualTo(beforePauseFrame.Pose.Xmm).Within(20d));

            yield return new WaitForSeconds(0.12f);
            Assert.That(replay.TryGetObservedFrame(out var stillPausedFrame), Is.True);
            Assert.That(stillPausedFrame.Pose.Xmm, Is.EqualTo(pausedFrame.Pose.Xmm).Within(0.001d));

            replay.SetPlaybackPaused(false);
            yield return new WaitForSeconds(0.12f);
            Assert.That(replay.TryGetObservedFrame(out var resumedFrame), Is.True);
            Assert.That(resumedFrame.Pose.Xmm, Is.GreaterThan(stillPausedFrame.Pose.Xmm + 50d));

            Object.Destroy(replayObject);
        }

        [UnityTest]
        public IEnumerator ReEnable_RestartsPlaybackFromBeginning()
        {
            var replayObject = new GameObject("Replay Re-enable Source");
            var replay = replayObject.AddComponent<ReplayCameraSourceBehaviour>();
            SetPrivateField(replay, "loop", false);
            Assert.That(replay.LoadReplayData(CreateLinearReplayCsv(1f), ReplayDataFormat.Csv), Is.True);

            yield return new WaitForSeconds(0.12f);
            Assert.That(replay.TryGetObservedFrame(out var progressedFrame), Is.True);
            Assert.That(progressedFrame.Pose.Xmm, Is.GreaterThan(50d));

            replay.enabled = false;
            replay.enabled = true;
            Assert.That(replay.TryGetObservedFrame(out var restartedFrame), Is.True);
            Assert.That(restartedFrame.Pose.Xmm, Is.LessThan(50d));

            Object.Destroy(replayObject);
        }

        private static string CreateLinearReplayCsv(float durationSeconds)
        {
            return $"TimeSeconds,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,FocalLengthMm\n0,0,1,-10,0,0,0,35\n{durationSeconds},1,1,-10,0,0,0,50";
        }

        private static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
