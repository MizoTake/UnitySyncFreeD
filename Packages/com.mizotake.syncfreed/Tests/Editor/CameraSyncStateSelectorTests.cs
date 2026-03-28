using MizoTake.SyncFreeD.Core.Models;
using MizoTake.SyncFreeD.Core.Sync;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class CameraSyncStateSelectorTests
    {
        [Test]
        public void SelectOutputPose_ReplacesCorrectedWithObservedWhenRequested()
        {
            var state = new CameraSyncState
            {
                Command = new PoseState { PanDeg = 1d },
                Predicted = new PoseState { PanDeg = 2d },
                Observed = new PoseState { PanDeg = 3d },
                Corrected = new PoseState { PanDeg = 4d },
                CommandLens = new LensState { FocalLengthMm = 10d },
                PredictedLens = new LensState { FocalLengthMm = 20d },
                ObservedLens = new LensState { FocalLengthMm = 30d },
                CorrectedLens = new LensState { FocalLengthMm = 40d },
                Lens = new LensState { FocalLengthMm = 40d }
            };

            var selected = CameraSyncStateSelector.SelectOutputPose(state, OutputPoseKind.Observed);

            Assert.That(selected.Corrected.PanDeg, Is.EqualTo(3d).Within(0.0001d));
            Assert.That(selected.CorrectedLens.FocalLengthMm, Is.EqualTo(30d).Within(0.0001d));
            Assert.That(selected.Lens.FocalLengthMm, Is.EqualTo(30d).Within(0.0001d));
        }

        [Test]
        public void SelectOutputPose_BlendsPredictedAndObservedWhenRequested()
        {
            var state = new CameraSyncState
            {
                Predicted = new PoseState { PanDeg = 2d, Xmm = 100d },
                Observed = new PoseState { PanDeg = 6d, Xmm = 300d }
            };

            var selected = CameraSyncStateSelector.SelectOutputPose(state, OutputPoseKind.Blended);

            Assert.That(selected.Corrected.PanDeg, Is.EqualTo(4d).Within(0.0001d));
            Assert.That(selected.Corrected.Xmm, Is.EqualTo(200d).Within(0.0001d));
        }
    }
}
