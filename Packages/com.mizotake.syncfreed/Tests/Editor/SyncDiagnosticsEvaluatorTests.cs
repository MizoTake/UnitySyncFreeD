using MizoTake.SyncFreeD.Core.Diagnostics;
using MizoTake.SyncFreeD.Core.Models;
using NUnit.Framework;

namespace MizoTake.SyncFreeD.Tests.Editor
{
    public sealed class SyncDiagnosticsEvaluatorTests
    {
        [Test]
        public void Evaluate_ComputesRotationAndPositionErrors()
        {
            var state = new CameraSyncState
            {
                PredictedLens = new LensState { FocalLengthMm = 30d },
                ObservedLens = new LensState { FocalLengthMm = 35d },
                CorrectedLens = new LensState { FocalLengthMm = 50d },
                Observed = new PoseState { PanDeg = 10d, TiltDeg = 20d, RollDeg = 30d, Xmm = 100d, Ymm = 200d, Zmm = 300d },
                Corrected = new PoseState { PanDeg = 14d, TiltDeg = 18d, RollDeg = 32d, Xmm = 104d, Ymm = 203d, Zmm = 312d },
                Timing = new TimingState { TrackingDelayMs = 22, VideoAlignmentDelayMs = 44 },
                Validity = new ValidityState { IsTrackingValid = true, IsLensValid = true, IsDegraded = false, IsFallbackMode = false }
            };

            var snapshot = SyncDiagnosticsEvaluator.Evaluate(state);

            Assert.That(snapshot.PanErrorDeg, Is.EqualTo(4d).Within(0.0001d));
            Assert.That(snapshot.TiltErrorDeg, Is.EqualTo(-2d).Within(0.0001d));
            Assert.That(snapshot.RollErrorDeg, Is.EqualTo(2d).Within(0.0001d));
            Assert.That(snapshot.PositionErrorMm, Is.EqualTo(13d).Within(0.0001d));
            Assert.That(snapshot.ZoomErrorMm, Is.EqualTo(15d).Within(0.0001d));
            Assert.That(snapshot.TrackingDelayMs, Is.EqualTo(22));
            Assert.That(snapshot.VideoAlignmentDelayMs, Is.EqualTo(44));
            Assert.That(snapshot.IsTrackingValid, Is.True);
            Assert.That(snapshot.IsLensValid, Is.True);
            Assert.That(snapshot.IsDegraded, Is.False);
            Assert.That(snapshot.IsFallbackMode, Is.False);
            Assert.That(snapshot.CorrectionApplied, Is.True);
        }
    }
}
