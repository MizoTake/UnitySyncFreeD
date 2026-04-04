using MizoTake.SyncFreeD.Editor.Support;
using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncFreeDDebugComparisonBuilderTests
    {
        [Test]
        public void BuildRows_IncludesPredictedAndLensStates()
        {
            var state = new CameraSyncState
            {
                Command = new PoseState { PanDeg = 10d, TiltDeg = 20d, RollDeg = 30d, Xmm = 100d, Ymm = 200d, Zmm = 300d },
                Predicted = new PoseState { PanDeg = 11d, TiltDeg = 21d, RollDeg = 31d, Xmm = 110d, Ymm = 210d, Zmm = 310d },
                Observed = new PoseState { PanDeg = 12d, TiltDeg = 22d, RollDeg = 32d, Xmm = 120d, Ymm = 220d, Zmm = 320d },
                Corrected = new PoseState { PanDeg = 13d, TiltDeg = 23d, RollDeg = 33d, Xmm = 130d, Ymm = 230d, Zmm = 330d },
                CommandLens = new LensState { FocalLengthMm = 35d, FocusDistanceMeters = 2d, IrisFNumber = 2.8d },
                PredictedLens = new LensState { FocalLengthMm = 36d, FocusDistanceMeters = 2.1d, IrisFNumber = 3d },
                ObservedLens = new LensState { FocalLengthMm = 37d, FocusDistanceMeters = 2.2d, IrisFNumber = 3.2d },
                CorrectedLens = new LensState { FocalLengthMm = 38d, FocusDistanceMeters = 2.3d, IrisFNumber = 3.5d },
                Timing = new TimingState { FrameModulo16 = 7 }
            };

            var snapshot = SyncFreeDDebugSnapshotBuilder.Build(state, default, null);

            Assert.That(snapshot.PoseRows[0].Label, Is.EqualTo("PRY"));
            Assert.That(snapshot.PoseRows[1].Label, Is.EqualTo("XYZ(mm)"));
            Assert.That(snapshot.PoseRows[0].Predicted, Does.Contain("11.00"));
            Assert.That(snapshot.LensRows[0].Label, Is.EqualTo("Zoom(mm)"));
            Assert.That(snapshot.LensRows[0].Observed, Does.Contain("37.00"));
            Assert.That(snapshot.LensRows[3].Label, Is.EqualTo("Frame"));
            Assert.That(snapshot.LensRows[3].Corrected, Is.EqualTo("7"));
        }
    }
}
